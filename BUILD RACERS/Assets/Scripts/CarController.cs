using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [System.Serializable]
    public class WheelVisual
    {
        public Transform leftWheel;
        public Transform rightWheel;
        public bool steering;  // 前輪なら true
    }

    public List<WheelVisual> wheelVisuals;

    [SerializeField]
    private float motorForce = 10f;
    [SerializeField]
    private float steerAngle = 60f;
    [SerializeField]
    private float turnSensitivity = 2f;
    [SerializeField]
    private float maxSpeed = 20f;

    private Rigidbody rb;
    private InputAction throttleAction;
    private InputAction brakeAction;
    private InputAction steerAction;

    private float currentSteer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Yをマイナスにして下方向へ重心を移動(転倒防止)
        rb.centerOfMass = new Vector3(0f, -1.0f, 0f);

        // 物理挙動を補完して滑らかに動かす
        rb.interpolation = RigidbodyInterpolation.Interpolate;

    }

    //入力処理、キーボード、コントローラーに対応
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
        throttleAction.Disable();
        brakeAction.Disable();
        steerAction.Disable();
    }

    private void FixedUpdate()
    {
        float motorInput = throttleAction.ReadValue<float>() - brakeAction.ReadValue<float>();
        float steerInput = steerAction.ReadValue<float>();

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

        // 進行方向へ力を加える
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            Quaternion steerRotation = Quaternion.Euler(0f, currentSteer, 0f);
            Vector3 forwardDir = steerRotation * transform.forward;
            float motorPower = motorInput < 0 ? motorForce * 0.6f : motorForce;
            rb.AddForce(forwardDir * motorInput * motorPower, ForceMode.Acceleration);
        }

        // 横方向速度を減衰させる（横滑り防止）
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= 0.85f; // ← 横方向の速度を抑制（値を0〜1で調整）
        rb.linearVelocity = transform.TransformDirection(localVel);

        // 車体の回転
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float rotationSign = motorInput < 0 ? -1f : 1f;
            float turnAmount = steerInput * turnSensitivity * rotationSign;
            Quaternion deltaRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }


}
