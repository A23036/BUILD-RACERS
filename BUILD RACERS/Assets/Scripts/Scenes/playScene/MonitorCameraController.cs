using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MonitorCameraController : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -6);
    [SerializeField] private float followSmooth = 8f;
    [SerializeField] private float rotateSmooth = 8f;

    [Header("Spectator")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float monitorDistance = 6f;

    private Transform target;

    private float yaw;
    private float pitch;

    private int watchIndex = 0;
    Player[] cachedPlayers;

    private TextMeshProUGUI targetName;

    private bool hasCustomOrbit = false;

    // New Input System
    private InputAction clickAction;
    private InputAction pointerDeltaAction;
    private InputAction backViewAction;

    void Awake()
    {
        var textObj = GameObject.Find("MonitorTargetName");
        if (textObj != null) targetName = textObj.GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (PlayerPrefs.GetInt("isMonitor") == 1)
        {
            cachedPlayers = PhotonNetwork.PlayerList;
            SetNextTarget(0);
        }

        //カメラの初期設定
        Transform carTf = FindAnyObjectByType<CarController>()?.transform;
        var cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
            cameraController.SetTarget(carTf);
        SetNextTarget(0);

        Debug.Log("Monitor Start");
        if(target == null)
        {
            Debug.Log("Monitor Target is null");
        }
    }

    void OnEnable()
    {
        // 左クリック / タップ
        clickAction = new InputAction(type: InputActionType.Button);
        clickAction.AddBinding("<Mouse>/leftButton");
        clickAction.AddBinding("<Touchscreen>/primaryTouch/press");
        clickAction.Enable();

        // マウス移動 / タッチ移動
        pointerDeltaAction = new InputAction(type: InputActionType.Value);
        pointerDeltaAction.AddBinding("<Mouse>/delta");
        pointerDeltaAction.AddBinding("<Touchscreen>/primaryTouch/delta");
        pointerDeltaAction.Enable();

        // 背面切り替え（Bキー）
        backViewAction = new InputAction(type: InputActionType.Button);
        backViewAction.AddBinding("<Keyboard>/b");
        backViewAction.Enable();
        base.OnEnable();
    }

    void OnDisable()
    {
        clickAction?.Disable();
        pointerDeltaAction?.Disable();
        backViewAction?.Disable();
        base.OnDisable();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired;

        // --- 押した瞬間 ---
        if (clickAction.WasPressedThisFrame())
        {
            if (!hasCustomOrbit)
            {
                hasCustomOrbit = true;

                Vector3 dir = (transform.position - target.position).normalized;
                yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
            }
        }

        // --- 押し続けている間 ---
        if (clickAction.IsPressed())
        {
            Vector2 delta = pointerDeltaAction.ReadValue<Vector2>();
            yaw += delta.x * mouseSensitivity * 0.1f;
            pitch -= delta.y * mouseSensitivity * 0.1f;
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        // --- 位置計算 ---
        if (hasCustomOrbit)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rot * Vector3.back;
            desired = target.position + dir * monitorDistance;
        }
        else
        {
            Vector3 baseOffset = backViewAction.IsPressed()
                ? new Vector3(offset.x, offset.y, -offset.z)
                : offset;

            desired = target.position + baseOffset;
        }

        transform.position = desired;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }



    public void SetNextTarget(int step)
    {
        UpdateCaches();
        if (cachedPlayers == null || cachedPlayers.Length == 0) return;

        watchIndex += step;
        if (watchIndex < 0) watchIndex = cachedPlayers.Length - 1;
        else if (watchIndex >= cachedPlayers.Length) watchIndex = 0;

        Player p = cachedPlayers[watchIndex];

        var cars = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        foreach (var car in cars)
        {
            PhotonView pv = car.GetComponent<PhotonView>();
            if (pv != null && pv.Owner == p)
            {
                target = car.transform;
                targetName.text = pv.Owner.NickName;

                return;
            }
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (target == null)
        {
            SetNextTarget(0);
        }

        //カメラの初期設定
        Transform carTf = FindAnyObjectByType<CarController>()?.transform;
        var cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
            cameraController.SetTarget(carTf);
        SetNextTarget(0);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateCaches();
    }

    public void UpdateCaches()
    {
        if (cachedPlayers != null &&
            cachedPlayers.Length == PhotonNetwork.PlayerList.Length) return;

        cachedPlayers = PhotonNetwork.PlayerList;
        watchIndex = Mathf.Clamp(watchIndex, 0, cachedPlayers.Length - 1);
    }
}
