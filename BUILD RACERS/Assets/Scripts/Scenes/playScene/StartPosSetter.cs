using UnityEngine;
using Photon.Pun;
using Fusion;
using ExitGames.Client.Photon;

public class StartPosSetter : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 offsetPos = new Vector3(0, 1f, 0);
    public Transform[] startPosList;
    private bool[] isSet;

    private int driversSum = 0;
    private int nowConnectDrivers = 0;
    private bool isSetDrivers = false;

    private string debugText = "waiting for members...";

    //スタートまでの時間を設定
    [SerializeField] private int untilStartTime;

    private void Awake()
    {
        //観戦者の時に作動しないように　RPCはバッファされるため関数先で処理を止める　＝＞　途中参加でREADY GO!が表示されないように
        //マスターなら途中参加でない　＋　カートの初期位置セットを実行したいので false にしない
        if(!photonView.IsMine && PlayerPrefs.GetInt("isMonitor") == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        //ドライバーのスタート地点を取得
        startPosList = new Transform[transform.childCount];
        isSet = new bool[transform.childCount];

        int i = 0;
        foreach (Transform child in transform)
        {
            startPosList[i] = child;

            //フラグ初期化
            isSet[i] = false;

            i++;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        Debug.Log("=== StartPosSetter START ===");

        if (!PhotonNetwork.IsConnected)
        {
            debugText = "LOADING...";
            
            //ドライバーを初期位置にセット
            Invoke(nameof(SetStartPosDrivers), 3f);

            //N秒後にドライバー開始
            Invoke(nameof(DriverStart), untilStartTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!isSetDrivers && driversSum <= nowConnectDrivers)
        {
            //全ドライバーが接続されたら初期位置へセット
            Invoke(nameof(SetStartPosDrivers), 1f);
            isSetDrivers = true;

            //N秒後にドライバー開始
            Invoke(nameof(DriverStart), untilStartTime);
        }
        else
        {
            Debug.Log(" === WAIT MENBERS === ");
        }

        //2秒後に消す
        if (debugText == "GO!")
        {
            Invoke(nameof(ResetDebugText), 2f);
        }
    }

    public Transform GetStartPos()
    {
        int idx = 0;
        for(int i = 0;i < isSet.Length;i++)
        {
            if (isSet[i]) continue;
            isSet[i] = true;
            idx = i;
            break;
        }

        Debug.Log("START POS" + startPosList[idx].position);
        return startPosList[idx];
    }

    [PunRPC]
    public void RPC_NotifyDriverConnected()
    {
        //マスターより先に呼ばれる可能性があるため、OnRoomPropertiesUpdateで処理する
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "newnowConnectDrivers", nowConnectDrivers + 1 } });
    }

    [PunRPC]
    public void RPC_SetStartPosDrivers()
    {
        SetStartPosDrivers();
    }

    public void SetStartPosDrivers()
    {
        int idx = 0;
        if (!PhotonNetwork.IsConnected)
        {
            //全カートを初期位置へ
            var karts = FindObjectsOfType<CarController>();
            foreach (var kart in karts)
            {
                //初期位置へセット
                kart.SetStartPos(startPosList[idx++ % startPosList.Length].position + offsetPos);
            }
            Debug.Log("=== Set StartPos Drivers (Offline) ===");
        }

        else if (PhotonNetwork.IsMasterClient)
        {
            //マスタークライアントのみ実行
            var karts = FindObjectsOfType<CarController>();
            foreach (var kart in karts)
            {
                // PhotonViewを取得してRPCを呼ぶ
                PhotonView photonView = kart.GetComponent<PhotonView>();
                if (photonView != null)
                {
                    //初期位置へセット
                    photonView.RPC("RPC_SetStartPos", RpcTarget.AllBuffered, startPosList[idx++ % startPosList.Length].position + offsetPos);

                    //順位を更新
                    photonView.RPC("RPC_UpdateRank", RpcTarget.AllBuffered);
                }
            }
        }

        debugText = "READY";
        if (PhotonNetwork.IsConnected) photonView.RPC("RPC_UpdateDebugText", RpcTarget.AllBuffered, debugText);
    }

    public void DriverStart()
    {
        if(!PhotonNetwork.IsConnected)
        {
            //全カートの状態を運転へ
            var karts = FindObjectsOfType<CarController>();
            foreach (var kart in karts)
            {
                //状態を運転へ
                kart.StateToDrive();
            }
        }
        else if(PhotonNetwork.IsMasterClient)
        {
            //マスタークライアントのみ実行
            var karts = FindObjectsOfType<CarController>();
            foreach (var kart in karts)
            {
                // PhotonViewを取得してRPCを呼ぶ
                PhotonView photonView = kart.GetComponent<PhotonView>();
                if (photonView != null)
                {
                    //状態を運転へ
                    //photonView.RPC("RPC_StateToDrive", RpcTarget.AllBufferedBuffered);
                    photonView.RPC("RPC_StateToDrive", RpcTarget.AllBuffered);
                }
            }
        }

        debugText = "GO!";
        if(PhotonNetwork.IsConnected) photonView.RPC("RPC_UpdateDebugText", RpcTarget.AllBuffered, debugText);
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 80;
        style.normal.textColor = Color.red;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;  // Bold追加

        float width = 800;
        float height = 400;
        Rect rect = new Rect(
            (Screen.width - width) / 2,
            (Screen.height - height) / 2 - Screen.height/4,
            width,
            height
        );

        GUI.Label(rect, debugText, style);
    }

    void ResetDebugText()
    {
        debugText = "";
    }

    [PunRPC]
    public void RPC_UpdateDebugText(string text)
    {
        debugText = text;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        /*
        //総ドライバー数を取得
        driversSum = (int)PhotonNetwork.CurrentRoom.CustomProperties["DriversCount"];

        //接続済みドライバー数を取得
        if (propertiesThatChanged.TryGetValue("newnowConnectDrivers", out var v) && v is int ncd)
        {
            nowConnectDrivers = ncd;
        }
        */

        //別方法で接続済みドライバー数を取得
        var drivers = FindObjectsOfType<CarController>();
        nowConnectDrivers = drivers.Length;
    }
}
