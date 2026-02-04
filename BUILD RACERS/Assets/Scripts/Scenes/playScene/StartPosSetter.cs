using ExitGames.Client.Photon;
using Fusion;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartPosSetter : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 offsetPos = new Vector3(0, 1f, 0);
    public Transform[] startPosList;
    private bool[] isSet;

    //接続される人数と現在の接続数　ドライバー
    private int driversSum = 99;
    private int nowConnectDrivers = 0;

    //接続される人数と現在の接続数　エンジニア
    private int engineersSum = 99;
    private int nowConnectEngineers = 0;

    //ゴールしたドライバーの数
    private int raceClearDriversSum = 0;

    //ドライバーがスタート位置についているか
    private bool isSetDrivers = false;

    //スタート時に再生する画像を再生したか
    private bool isPlayReady = false;
    private bool isPlayGo = false;

    //スタートまでの時間を設定
    [SerializeField] private int untilStartTime;

    //スタート時に再生する画像を制御するスクリプト
    [SerializeField] private GameObject readyUIObj;
    private readyUI readyUI;

    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

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
            Invoke(nameof(SetStartPosDrivers), 3f);

            //N秒後にドライバー開始
            Invoke(nameof(DriverStart), untilStartTime + 3f);
        }
        //*/

        //白い画面から始める
        fade.SetStartRange();

        //観戦者は読み込みなしでフェードアウト
        if (PlayerPrefs.GetInt("isMonitor") == 1 && fade != null)
        {
            fade.FadeOut(fadeOutDuration);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!PhotonNetwork.IsConnected) return;

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
            driversSum = PlayerPrefs.GetInt("DriversCount");
            Debug.Log($"{driversSum} をPlayerPrefsから取得！");
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
            engineersSum = PlayerPrefs.GetInt("EngineersCount");
            Debug.Log($"{engineersSum} をPlayerPrefsから取得！");
        }

        //エンジニアとドライバーの接続を待つ
        if (PhotonNetwork.IsMasterClient && !isSetDrivers && driversSum <= nowConnectDrivers && engineersSum <= nowConnectEngineers)
        {
            //全ドライバーが接続されたら初期位置へセット
            Invoke(nameof(SetStartPosDrivers), 1f);
            isSetDrivers = true;

            //N秒後にドライバー開始
            Invoke(nameof(DriverStart), untilStartTime);
        }
        else
        {
            Debug.Log($" === WAIT MENBERS === DRIVER:{nowConnectDrivers}/{driversSum} , ENGINEER:{nowConnectEngineers}/{engineersSum}");
        }

        //ルームの状態をレース終了へ
        if(PhotonNetwork.IsMasterClient)
        {
            if(raceClearDriversSum >= driversSum)
            {
                props = new Hashtable();
                props["masterGameScene"] = "Finished";
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }

        Debug.Log($"GOAL DRIVERS:{raceClearDriversSum} / {driversSum}");
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
    public void RPC_NotifyDriverConnected(int d_id)
    {
        //途中切断等に対応するために　カウントは全員、スタート判定のみマスターが行う
        nowConnectDrivers++;

        //自分のドライバーならペア検索フラグをオフにする
        var karts = FindObjectsOfType<CarController>();
        foreach(var cc in karts)
        {
            if(!cc.isMine) continue;

            //フェードアウト
            if (fade != null)
            {
                fade.FadeOut(fadeOutDuration);
            }

            var pv = cc.GetComponent<PhotonView>();
            if(pv.ViewID == d_id)
            {
                cc.RPC_NotifLoadFinish();
            }
        }
    }

    [PunRPC]
    public void RPC_NotifyEngineerConnected(int e_id)
    {
        //途中切断等に対応するために　カウントは全員、スタート判定のみマスターが行う
        nowConnectEngineers++;

        //自分のエンジニアならペア検索フラグをオフにする
        var engineers = FindObjectsOfType<Engineer>();
        foreach (var eng in engineers)
        {
            var pv = eng.GetComponent<PhotonView>();

            if (!pv.IsMine) continue;

            //フェードアウト
            if (fade != null)
            {
                fade.FadeOut(fadeOutDuration);
            }

            if (pv.ViewID == e_id)
            {
                eng.RPC_NotifLoadFinish();
            }
        }
    }

    [PunRPC]
    public void RPC_NotifyDriverGoal()
    {
        raceClearDriversSum++;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Hashtable hash = new Hashtable();

        // 退室したプレイヤーのカスタムプロパティを確認
        if (otherPlayer.CustomProperties.TryGetValue("driverNum" , out var dnObj) && dnObj is int dn && dn != -1)
        {
            //対応する総数を減らす
            driversSum--;
            PlayerPrefs.SetInt("DriversCount",driversSum);
            Debug.Log(" === Disconnect Driver === ");

            //ゴール済みだったらゴールカウントも減らす
            if(otherPlayer.CustomProperties.TryGetValue("isRaceClear", out var flag) && (bool)flag)
            {
                raceClearDriversSum--;
            }

            //マスターのみカスタムプロパティに反映させる
            if (PhotonNetwork.IsMasterClient)
            {
                hash["DriversCount"] = driversSum;
                PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
            }

        }
        else if (otherPlayer.CustomProperties.TryGetValue("engineerNum", out var enObj) && enObj is int en && en != -1)
        {
            //対応する総数を減らす
            engineersSum--;
            PlayerPrefs.SetInt("EngineersCount",engineersSum);
            Debug.Log(" === Disconnect Engineer === ");

            //マスターのみカスタムプロパティに反映させる
            if(PhotonNetwork.IsMasterClient)
            {
                hash["EngineersCount"] = engineersSum;
                PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
            }
        }
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
                kart.transform.rotation = gameObject.transform.rotation;

                Debug.Log($"=== Set StartPos Drivers (Offline) {kart.GetName()} , {kart.transform.position} ===");
            }
            readyUI.StartReadyImage();

            //フェードアウト
            if (fade != null)
            {
                fade.FadeOut(fadeOutDuration);
            }
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
