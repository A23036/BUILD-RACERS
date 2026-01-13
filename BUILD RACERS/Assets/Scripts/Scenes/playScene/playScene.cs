using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class playScene : baseScene
{
    [Tooltip("BOTの生成数")]
    [SerializeField] int GenerateBotsNum = 0;

    [SerializeField] private GameObject DriverUI;
    [SerializeField] private GameObject EngineerUI;
    [SerializeField] private GameObject MonitorUI;
    [SerializeField] private GameObject ResultUI;

    private bool isNotifyDriverConnected = false;
    private InputAction resultAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== PLAY SCENE START ===");
        
        preSceneName = "select";

        GenerateKarts();

        //ロード完了後にメッセージ処理を再開
        PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void Awake()
    {
        base.Awake();

        Debug.Log("=== PLAY SCENE AWAKE ===");
    }

    // Update is called once per frame
    void Update()
    {
        if (resultAction.WasPressedThisFrame())
        {
            ToResult();
        }

        base.Update();
    }
    private void OnEnable()
    {
        resultAction = new InputAction(type: InputActionType.Button);
        resultAction.AddBinding("<Keyboard>/r");
        resultAction.Enable();
    }

    private void OnDisable()
    {
        resultAction?.Disable();
    }


    public void ToResult()
    {
        SceneManager.LoadScene("result");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        //カートの生成
        GenerateKarts();

        Debug.Log("接続成功");
    }

    public void GenerateKarts()
    {
        //オフラインなら処理なし
        if (!PhotonNetwork.IsConnected) return;

        if(PlayerPrefs.GetInt("driverNum") != -1)
        {
            // プレイヤー生成（自分）
            var position = new Vector3(Random.Range(-3f, 3f), 0f , PhotonNetwork.LocalPlayer.ActorNumber * 5);
            var player = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
            var playerCc = player.GetComponent<CarController>();
            playerCc.SetCamera();
            playerCc.isMine = true;

            float geneX = 0, geneZ = 0;
            for (int i = 0; i < GenerateBotsNum; i++)
            {
                // CPUの生成　テスト
                position = new Vector3(geneX, 0f, -i * geneZ);
                var cpu = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
                var cpuCc = cpu.GetComponent<CarController>();

                // cpu に AI を設定する。WaypointContainer を渡す（シーンに複数ある場合は適切に選択）
                var wpContainer = FindObjectOfType<WaypointContainer>(); // 単一ならこれでOK

                //AIをクラス指定で選択して生成　引数はウェイポイント
                cpuCc.SetAI<AIDriver>(wpContainer);
                //cpuCc.SetAI<AIDriver_v2>();

                geneX++;
                if (geneX >= 3)
                {
                    geneX = 0;
                    geneZ += .1f;
                }
            }

            //UIの表示・非表示
            DriverUI.SetActive(true);
            EngineerUI.SetActive(false);
            MonitorUI.SetActive(false);
        }
        else if(PlayerPrefs.GetInt("engineerNum") != -1)
        {
            //UIの表示・非表示
            DriverUI.SetActive(false);
            EngineerUI.SetActive(true);
            MonitorUI.SetActive(false);

            //エンジニア生成
            var player = PhotonNetwork.Instantiate("Engineer", new Vector3(0,0,0), Quaternion.identity);
            var playerCc = player.GetComponent<Engineer>();
        }
        else if(PlayerPrefs.GetInt("isMonitor") == 1)
        {
            //UIの表示・非表示
            DriverUI.SetActive(false);
            EngineerUI.SetActive(false);
            MonitorUI.SetActive(true);

            //カメラの初期設定
            Transform carTf = FindAnyObjectByType<CarController>()?.transform;
            var cameraController = Camera.main.GetComponent<CameraController>();
            if (cameraController != null)
                cameraController.SetTarget(carTf);
        }
        else
        {
            Debug.Log("セレクトされていません");
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if(!isNotifyDriverConnected && PlayerPrefs.GetInt("driverNum") != -1 && photonView != null)
        {
            //マスタークライアントへカートの生成を通知する
            photonView.RPC("RPC_NotifyDriverConnected", RpcTarget.All);

            isNotifyDriverConnected = true;
        }
    }
    public GameObject GetResultUI()
    {
        return ResultUI;
    }
}
