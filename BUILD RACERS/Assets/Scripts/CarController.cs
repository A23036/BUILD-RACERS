using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using TMPro;

public class CarController : MonoBehaviourPunCallbacks
{
    //ジョイスティック
    private Joystick variableJoystick;

    //CPU
    private IDriver driver = null;

    [System.Serializable]
    public class WheelVisual
    {
        public Transform leftWheel;
        public Transform rightWheel;
        public bool steering;  // 前輪なら true
    }

    public List<WheelVisual> wheelVisuals;

    [Header("基本パラメータ")]
    [SerializeField] private float motorForce = 10f;
    [SerializeField] private float steerAngle = 60f;
    [SerializeField] private float turnSensitivity = 2f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("地面関連")]
    [SerializeField] private float raycastLength = 1.2f;  // 地面判定距離
    [SerializeField] private LayerMask groundMask;        // 地面レイヤー
    //ダート
    [SerializeField] private float dirtSpeedMultiplier = 0.6f;  // ダート上の速度倍率
    [SerializeField] private float dirtAccelMultiplier = 1.0f;  // ダート上の加速倍率
    //ブースト
    [SerializeField] private float boostSpeedMultiplier = 1.8f;   // ブースト時の速度倍率
    [SerializeField] private float boostAccelMultiplier = 2.5f;   // ブースト時の加速倍率
    [SerializeField] private float boostDuration = 2.0f;          // 効果時間（秒）
    private float boostTimer = 0f;  // 残りブースト時間

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI speedText;  // 速度表示テキスト
    [SerializeField] private TextMeshProUGUI coinText;  // コイン枚数表示テキスト

    private Rigidbody rb;
    private InputAction throttleAction;
    private InputAction brakeAction;
    private InputAction steerAction;

    private float currentSteer = 0f;
    private int coinCnt = 0;
    private string currentGroundTag = "Default";

    private void Awake()
    {
        Debug.Log("AWAKE");

        //ジョイスティック取得
        var joystick = GameObject.Find("Floating Joystick");
        if(joystick != null) variableJoystick = joystick.GetComponent<Joystick>();

        // スピード表示テキストの設定
        if (speedText == null)
        {
            var text = GameObject.FindWithTag("SpeedText");
            if (text != null)
                speedText = text.GetComponent<TextMeshProUGUI>();
            else
                speedText = FindObjectOfType<TextMeshProUGUI>();
        }

        // コイン表示テキストの設定
        if (coinText == null)
        {
            var text = GameObject.FindWithTag("coinText");
            if (text != null)
                coinText = text.GetComponent<TextMeshProUGUI>();
            else
                coinText = FindObjectOfType<TextMeshProUGUI>();
        }

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0f, -1.0f, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // 入力設定
    private void OnEnable()
    {
        throttleAction = new InputAction(type: InputActionType.Button);
        throttleAction.AddBinding("<Keyboard>/w");
        throttleAction.AddBinding("<Keyboard>/upArrow");
        throttleAction.AddBinding("<Gamepad>/buttonEast");
        throttleAction.Enable();

        brakeAction = new InputAction(type: InputActionType.Button);
        brakeAction.AddBinding("<Keyboard>/s");
        brakeAction.AddBinding("<Keyboard>/downArrow");
        brakeAction.AddBinding("<Gamepad>/buttonSouth");
        brakeAction.Enable();

        steerAction = new InputAction(type: InputActionType.Value);
        steerAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        steerAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        steerAction.AddBinding("<Gamepad>/leftStick/x");
        steerAction.Enable();
    }

    private void OnDisable()
    {
        if(throttleAction != null) throttleAction.Disable();
        if(brakeAction != null) brakeAction.Disable();
        if(steerAction != null) steerAction.Disable();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        UpdateGroundType();

        float motorInput = 0f;
        float brakeInput = 0f;
        float steerInput = 0f;

        // AIがいればそちらから取得
        if (driver != null)
        {
            driver.GetInputs(out float throttle, out float brake, out float steer);
            motorInput = throttle;
            brakeInput = brake;
            steerInput = steer;
        }
        else
        {
            //　プレイヤー入力
            motorInput = throttleAction.ReadValue<float>() - brakeAction.ReadValue<float>();
            steerInput = steerAction.ReadValue<float>();
            if (Input.GetMouseButton(0)) motorInput = 1;
            if (variableJoystick != null && variableJoystick.Direction != Vector2.zero)
                steerInput = Mathf.Clamp(variableJoystick.Direction.x / 0.9f, -1, 1);
        }

        // combine motor & brake: motorInput 0..1, brakeInput 0..1 -> netMotor (-1..1) or separate
        float netMotor = motorInput - brakeInput; // keep existing behavior if you like

        currentSteer = steerInput * steerAngle;

        // 見た目タイヤ回転
        foreach (var w in wheelVisuals)
        {
            float visualSteer = currentSteer * 0.5f;
            if (w.steering)
            {
                if (w.leftWheel != null)
                    w.leftWheel.localRotation = Quaternion.Euler(-90f, visualSteer, 90f);
                if (w.rightWheel != null)
                    w.rightWheel.localRotation = Quaternion.Euler(-90f, visualSteer, 90f);
            }
        }

        // --- Boostタグを検出 ---
        if (currentGroundTag == "Boost")
        {
            boostTimer = boostDuration; // 効果をリセット
        }

        // --- 地面別・ブースト補正(同じ) ---
        float accelMultiplier = 1f;
        float speedMultiplier = 1f;
        if (currentGroundTag == "Dirt")
        {
            accelMultiplier = dirtAccelMultiplier;
            speedMultiplier = dirtSpeedMultiplier;
        }

        if (boostTimer > 0f)
        {
            accelMultiplier *= boostAccelMultiplier;
            speedMultiplier *= boostSpeedMultiplier;
            boostTimer -= Time.fixedDeltaTime;
        }
        float maxAllowedSpeed = maxSpeed * speedMultiplier;

        if (rb.linearVelocity.magnitude < maxAllowedSpeed)
        {
            Quaternion steerRotation = Quaternion.Euler(0f, currentSteer, 0f);
            Vector3 forwardDir = steerRotation * transform.forward;
            float motorPower = (netMotor < 0 ? motorForce * 0.6f : motorForce) * accelMultiplier;
            rb.AddForce(forwardDir * netMotor * motorPower, ForceMode.Acceleration);
        }

        // 速度表示など残す（rb.linearVelocity -> rb.velocity）
        float speed = rb.linearVelocity.magnitude * 3.6f;
        if (speedText != null && driver == null) speedText.text = $"{speed:F1} km/h";

        // 横滑り防止
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= 0.85f;
        rb.linearVelocity = transform.TransformDirection(localVel);

        // 車体回転
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float rotationSign = netMotor < 0 ? -1f : 1f;
            float turnAmount = steerInput * turnSensitivity * rotationSign;
            Quaternion deltaRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    // 地面の種類をRaycastで検出
    private void UpdateGroundType()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastLength, groundMask))
        {
            currentGroundTag = hit.collider.tag;
        }
        else
        {
            currentGroundTag = "Default";
        }
    }

    //接触がコインならカウント
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            Coin coinScript = other.GetComponent<Coin>();
            if (coinScript.isCnt == false)
            {
                coinCnt++;
                coinScript.isCnt = true;

                //自分以外ならテキストの更新はしない
                if (!photonView.IsMine || driver != null) return;
                coinText.text = $"{coinCnt:D4}";
            }
        }
    }

    //ドライバーをAIに変更
    public void SetAI<T>(WaypointContainer waypointContainer = null)
        where T : Component , IDriver
    {
        Debug.Log("SET AI");

        if (driver == null)
        {
            var aiComp = gameObject.AddComponent<T>();
            driver = aiComp;

            if (waypointContainer != null)
            {
                aiComp.SetWaypointContainer(waypointContainer);
            }
            else
            {
                var wc = FindObjectOfType<WaypointContainer>();
                if (wc != null)
                    aiComp.SetWaypointContainer(wc);
                else
                    Debug.LogWarning("[CarController] SetAI: WaypointContainer が見つかりません。実行時に経路をセットしてください。");
            }
            /*
            // waypointContainerが渡されていれば設定。渡されなければシーン内のものを拾う（安全策）
            if (aiComp is AIDriver)
            {
                if (waypointContainer != null)
                {
                    aiComp.SetWaypointContainer(waypointContainer);
                }
                else
                {
                    var wc = FindObjectOfType<WaypointContainer>();
                    if (wc != null)
                        aiComp.SetWaypointContainer(wc);
                    else
                        Debug.LogWarning("[CarController] SetAI: WaypointContainer が見つかりません。実行時に経路をセットしてください。");
                }
            }
            */
            //名前をCPUに変更
            Transform labelTransform = transform.Find("NameLabel");
            if (labelTransform != null)
            {
                TextMeshPro nameLabel = labelTransform.GetComponent<TextMeshPro>();
                if (nameLabel != null)
                {
                    nameLabel.text = "CPU";
                }
            }
        }
    }

    //カメラの設定
    public void SetCamera()
    {
        var cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
            cameraController.SetTarget(transform);
    }
}