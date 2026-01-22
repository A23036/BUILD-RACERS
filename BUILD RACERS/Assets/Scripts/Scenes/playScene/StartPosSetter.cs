using ExitGames.Client.Photon;
using Fusion;
using Photon.Pun;
using UnityEngine;

public class StartPosSetter : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 offsetPos = new Vector3(0, 1f, 0);
    public Transform[] startPosList;
    private bool[] isSet;

    private int driversSum = 99;
    private int nowConnectDrivers = 0;

    private int engineersSum = 99;
    private int nowConnectEngineers = 0;

    private bool isSetDrivers = false;

    private bool isPlayReady = false;
    private bool isPlayGo = false;

    //スタートまでの時間を設定
    [SerializeField] private int untilStartTime;

    //readyUI
    [SerializeField] private GameObject readyUIObj;
    private readyUI readyUI;

    private void Awake()
    {
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
        readyUI = readyUIObj.GetComponent<readyUI>();

        Debug.Log("=== StartPosSetter START ===");

        ///*
        if (!PhotonNetwork.IsConnected)
        {
            //ドライバーを初期位置にセット
            Invoke(nameof(SetStartPosDrivers), 1f);

            //N秒後にドライバー開始
            Invoke(nameof(DriverStart), untilStartTime);
        }
        //*/
    }

    // Update is called once per frame
    void Update()
    {
        //総ドライバー数を取得
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.TryGetValue("DriversCount", out var dc) && dc is int)
        {
            driversSum = (int)dc;
            Debug.Log($"総ドライバー数受信：{dc}");
        }
        else
        {
            Debug.Log("総ドライバー数受信失敗");
        }

        //総エンジニア数を取得
        if (props.TryGetValue("EngineersCount", out var ec) && ec is int)
        {
            engineersSum = (int)ec;
            Debug.Log($"総エンジニア数受信：{ec}");
        }
        else
        {
            Debug.Log("総エンジニア数受信失敗");
        }

        //エンジニアとドライバーの接続を待つ
        if (!isSetDrivers && driversSum <= nowConnectDrivers && engineersSum <= nowConnectEngineers)
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
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"DRIVER:{nowConnectDrivers}/{driversSum} , ENGINEER:{nowConnectEngineers}/{engineersSum}");
            }
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
        //マスターがカウントする
        if (PhotonNetwork.IsMasterClient) nowConnectDrivers++;
    }

    [PunRPC]
    public void RPC_NotifyEngineerConnected()
    {
        //マスターがカウントする
        if (PhotonNetwork.IsMasterClient) nowConnectEngineers++;
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

                Debug.Log($"=== Set StartPos Drivers (Offline) {kart.transform.position} ===");
            }
            readyUI.StartReadyImage();
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
                    //ペア検索を終了させる
                    photonView.RPC("RPC_NotifLoadFinish", RpcTarget.AllBuffered);

                    //初期位置へセット
                    photonView.RPC("RPC_SetStartPos", RpcTarget.AllBuffered, startPosList[idx++ % startPosList.Length].position + offsetPos);

                    //順位を更新
                    photonView.RPC("RPC_UpdateRank", RpcTarget.AllBuffered);
                }
            }
            photonView.RPC("RPC_PlayReadyImage", RpcTarget.All);
        }
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
            readyUI.StartGoImage();
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
            photonView.RPC("RPC_PlayGoImage", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_PlayReadyImage()
    {
        if (isPlayReady) return;
        readyUI.StartReadyImage();
        isPlayReady = true;
    }

    [PunRPC]
    public void RPC_PlayGoImage()
    {
        if(isPlayGo) return;
        readyUI.StartGoImage();
        isPlayGo = true;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
    }
}
