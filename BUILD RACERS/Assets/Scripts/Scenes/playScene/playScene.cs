using ExitGames.Client.Photon;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

public class playScene : baseScene
{
    [Tooltip("BOTの生成数")]
    [SerializeField] int GenerateBotsNum = 0;

    [SerializeField] private GameObject DriverUI;
    [SerializeField] private GameObject EngineerUI;
    [SerializeField] private GameObject MonitorUI;
    [SerializeField] private GameObject ResultUI;

    private InputAction resultAction;

    private CarController carController;
    private Engineer engineer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!PhotonNetwork.InRoom) isConnect = false;

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
        //オフラインなら普通のInstantiate
        if (!PhotonNetwork.IsConnected)
        {
            carController = null;
            engineer = null;

            //プレイヤーの生成
            if (PlayerPrefs.GetInt("driverNum") != -1)
            {
                //ドライバーの生成
                var player = Instantiate(Resources.Load("player"), new Vector3(0, 0, 0), Quaternion.identity);
                player.GetComponent<CarController>().SetCamera();
                carController = player.GetComponent<CarController>();

                //UIの有効化
                DriverUI.SetActive(true);
                EngineerUI.SetActive(false);
            }
            else if (PlayerPrefs.GetInt("engineerNum") != -1)
            {
                //UIの有効化
                DriverUI.SetActive(false);
                EngineerUI.SetActive(true);

                //相方ドライバーの生成（CPU）
                var cpu = Instantiate(Resources.Load("Player"));
                carController = cpu.GetComponent<CarController>();
                //carController.SetCamera();
                carController.SetName(PlayerPrefs.GetString("PlayerName"));
                var cpuCc = cpu.GetComponent<CarController>();
                var wpContainer = FindObjectOfType<WaypointContainer>();
                cpuCc.SetAI<AIDriver>(wpContainer);

                //エンジニアの生成
                var player = Instantiate(Resources.Load("Engineer"));
                engineer = player.GetComponent<Engineer>();
                engineer.SetPairDriver(cpuCc);
                engineer.SetCamera();
            }
            else Debug.Log("not select");

            carController.isMine = true;

            //BOTドライバーの生成
            GenerateBotDrivers();

            return;
        }

        if (PlayerPrefs.GetInt("driverNum") != -1)
        {
            // プレイヤー生成（自分）
            var position = new Vector3(Random.Range(-3f, 3f), 0f, PhotonNetwork.LocalPlayer.ActorNumber * 5);
            var player = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
            var playerCc = player.GetComponent<CarController>();
            playerCc.SetCamera();
            playerCc.isMine = true;

            //UIの表示・非表示
            DriverUI.SetActive(true);
            EngineerUI.SetActive(false);
            MonitorUI.SetActive(false);
        }
        else if (PlayerPrefs.GetInt("engineerNum") != -1)
        {
            //UIの表示・非表示
            DriverUI.SetActive(false);
            EngineerUI.SetActive(true);
            MonitorUI.SetActive(false);

            //エンジニア生成
            var player = PhotonNetwork.Instantiate("Engineer", new Vector3(0, 0, 0), Quaternion.identity);
            var playerCc = player.GetComponent<Engineer>();
        }
        else if (PlayerPrefs.GetInt("isMonitor") == 1)
        {
            ToValidMonitor();
        }
        else
        {
            Debug.Log("セレクトされていません");
        }

        //マスターならBOT生成
        if(PhotonNetwork.IsMasterClient) GenerateBotDrivers();
    }


    public void ToValidMonitor()
    {
        PlayerPrefs.SetInt("driverNum", -1);
        PlayerPrefs.SetInt("engineerNum", -1);
        PlayerPrefs.SetInt("isMonitor", 1);

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

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        
    }
    public GameObject GetResultUI()
    {
        return ResultUI;
    }

    public void GenerateBotDrivers()
    {
        bool isOnline = PhotonNetwork.InRoom ? true : false;

        //オンラインなら生成数を求める
        if(isOnline)
        {
            var Players = PhotonNetwork.PlayerList;
            GenerateBotsNum = 8;
            foreach (var p in Players)
            {
                if(p.CustomProperties.TryGetValue("driverNum" , out var n) && n is int && (int)n != -1)
                {
                    GenerateBotsNum--;
                }
            }
        }

        //テスト　生成数固定
        //GenerateBotsNum = 1;

        var wpContainer = FindObjectOfType<WaypointContainer>();
        for (int i = 0; i < GenerateBotsNum; i++)
        {
            //オンラインか否かで生成方法を切り替える
            var bot = isOnline ? 
                PhotonNetwork.Instantiate("Player", new Vector3(0, 1, (i + 1) * -6f), Quaternion.identity):
                Instantiate(Resources.Load("Player"), new Vector3(0, 1, (i + 1) * -6f), Quaternion.identity);
            
            var botCc = bot.GetComponent<CarController>();
            botCc.SetAI<AIDriver>(wpContainer);

            //0埋め2桁で名前を設定
            botCc.SetName("CPU_" + (i + 1).ToString("00"));
        }

        Debug.Log($"BOT生成数: {GenerateBotsNum}");
    }
}
