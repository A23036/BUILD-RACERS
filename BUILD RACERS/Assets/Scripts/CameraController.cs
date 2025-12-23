using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class CameraController : MonoBehaviourPunCallbacks , IPunInstantiateMagicCallback
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -6); // 追従オフセット
    [SerializeField] private float smoothSpeed = 5f;

    private Transform target;  // カートのTransform

    // --- 観戦者関係 ---
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float monitorDistance = 6f;

    private float yaw;   // 水平方向
    private float pitch; // 垂直方向

    private int watchIndex = 0;
    Player[] cachedPlayers;

    private TextMeshProUGUI targetName;
    // --- 観戦者関係 ---

    public void SetTarget(Transform newTarget) => target = newTarget;

    void Start()
    {
        if (PlayerPrefs.GetInt("isMonitor") == 1)
        {
            cachedPlayers = PhotonNetwork.PlayerList;
            SetNextTarget(0);
        }
    }

    void Awake()
    {
        var textObj = GameObject.Find("MonitorTargetName");
        if (textObj != null)
        {
            targetName = textObj.GetComponent<TextMeshProUGUI>();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            //観戦者なら最初にランダムなカート1台をターゲットに設定
            if (PlayerPrefs.GetInt("isMonitor") == 1)
            {
                //カメラの初期設定
                Transform carTf = FindAnyObjectByType<CarController>()?.transform;
                var cameraController = Camera.main.GetComponent<CameraController>();
                if (cameraController != null)
                    cameraController.SetTarget(carTf);
            }
            return;
        }

        Vector3 desiredPosition;

        Vector3 forwardFlat = target.forward;
        forwardFlat.y = 0;
        forwardFlat.Normalize();

        Quaternion flatRotation = Quaternion.LookRotation(forwardFlat);

        if (Input.GetKey(KeyCode.B))
        {
            Vector3 backOffset = offset;
            backOffset.z *= -1;
            desiredPosition = target.position + flatRotation * backOffset;
        }
        else
        {
            desiredPosition = target.position + flatRotation * offset;
        }

        // --- スムーズに追従 --- 背面カメラの時はスムーズを適用しない
        if (Input.GetKeyDown("b") || Input.GetKeyUp("b")) transform.position = desiredPosition;
        else transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // --- カメラをプレイヤーの方向に向ける ---
        transform.LookAt(target.position + Vector3.up * 1.5f); // 1.5fで少し上を見させる

        //観戦者の処理
        if(PlayerPrefs.GetInt("isMonitor") == 1)
        {
            //カメラ操作
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 60f);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 dir = rot * Vector3.back;

            transform.position = target.position + dir * monitorDistance;
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }

    //追従対象の切り替え UIから呼び出す
    public void SetNextTarget(int step)
    {
        if (cachedPlayers == null || cachedPlayers.Length == 0) return;

        watchIndex += step;
        //配列の範囲外参照を防ぐ
        if(watchIndex < 0) watchIndex = cachedPlayers.Length - 1;
        else if(cachedPlayers.Length <= watchIndex) watchIndex = 0;

        Player p = cachedPlayers[watchIndex];

        foreach (var car in FindObjectsByType<CarController>(FindObjectsSortMode.None))
        {
            PhotonView pv = car.GetComponent<PhotonView>();
            if (pv != null && pv.Owner == p)
            {
                target = car.transform;
                targetName.text = pv.Owner.NickName;

                // カメラ角度をリセット
                yaw = transform.eulerAngles.y;
                pitch = 10f;

                return;
            }
        }

        //ループ内で処理が終了しない＝ターゲットが見つからなければ再度実行
        SetNextTarget(step);
    }

    //PhotonNetwork.Instantiateのコールバック　インターフェースからの実装
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        //キャッシュの更新
        UpdateCaches();
    }

    //ルームから誰か抜けたら呼ばれるコールバック
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //キャッシュの更新
        UpdateCaches();
    }

    public void UpdateCaches()
    {
        //キャッシュの更新
        Player[] currentList = PhotonNetwork.PlayerList;
        cachedPlayers = currentList;
        watchIndex = Mathf.Clamp(watchIndex, 0, cachedPlayers.Length - 1);
    }
}
