using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

public class Engineer : MonoBehaviourPunCallbacks
{
    private PartsManager partsManager;
    private PanelManager panelManager;

    private CarController carController;
    private Player pairPlayer = null;
    private int pairViewID = -1;

    private int lapCount = -1;
    private int maxLaps = 0;

    //リザルトUI ゴールしたら有効化
    private GameObject resultUI;

    //開始までの準備段階かフラグ
    private bool isLoading = true;

    private bool isNotifyEngineerConnected = false;

    //検索時間　計測用とタイムアウト制限時間
    [Tooltip("検索時間の制限(秒)")]
    [SerializeField] private float searchLimitTime = 20f;
    private float searchTimer = 0f;

    public void SetPairDriver(CarController car)
    {
        carController = car;
        car.SetEngineer(this);
    }

    void Awake()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        partsManager = GetComponentInChildren<PartsManager>();

        panelManager = GameObject.Find("PanelManager").GetComponent<PanelManager>();
        panelManager.SetEngineer(this);

        PhotonView pv = GetComponent<PhotonView>();

        Debug.Log("My ViewID: " + pv.ViewID);

        carController = null;

        switch (SceneManager.GetActiveScene().name)
        {
            case "gamePlay":
                var smp = FindObjectOfType<playScene>();
                if (smp != null)
                {
                    resultUI = smp.GetResultUI();
                    resultUI.SetActive(false);
                }
                break;
            case "singlePlay":
                var smsp = FindObjectOfType<singlePlayScene>();
                if (smsp != null)
                {
                    resultUI = smsp.GetResultUI();
                    resultUI.SetActive(false);
                }
                break;
            case "Map2":
                var smm2 = FindObjectOfType<map2>();
                if (smm2 != null)
                {
                    resultUI = smm2.GetResultUI();
                    resultUI.SetActive(false);
                }
                break;
        }
    }

    private void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            partsManager = GetComponentInChildren<PartsManager>();

            panelManager = GameObject.Find("PanelManager").GetComponent<PanelManager>();
            panelManager.SetEngineer(this);
            return;
        }

        if (!photonView.IsMine)
        {
            return;
        }

        partsManager = GetComponentInChildren<PartsManager>();

        panelManager = GameObject.Find("PanelManager").GetComponent<PanelManager>();
        panelManager.SetEngineer(this);

        TryPairPlayers();
    }

    private void Update()
    {
        //読み込み中ならペア検索
        if (isLoading && photonView.IsMine)
        {
            Debug.Log("エンジニア：ペア検索中！");
            TryPairPlayers();

            //タイムアウト処理
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchLimitTime)
            {
                Debug.Log("タイムアウト：ペア検索に時間がかかりすぎています");
                PhotonNetwork.Disconnect();
                SceneManager.LoadScene("menu");
            }
        }
        else if(!isLoading && photonView.IsMine)
        {
            Debug.Log("エンジニア：ペア発見済み！");
        }
    }

    private void TryPairPlayers()
    {
        if (!PhotonNetwork.IsConnected)
        {
            return;
        }

        // ペアを発見済みの場合、処理を行わない
        if (!photonView.IsMine) return;
        if (pairViewID != -1)
        {
            Debug.Log($"ペア発見済み：{pairViewID}");
            return;
        }

        /*
        Player[] players = PhotonNetwork.PlayerList;

        // ネットワークに接続中のplayerを一人ずつ調査
        foreach (var p in players)
        {
            Debug.Log($"{players.Length}人の中からペアを検索");

            //自分は処理なし
            if (PhotonNetwork.LocalPlayer == p) continue;

            // エンジニアはcontinue(ドライバーのみ探す)
            int d = p.CustomProperties["driverNum"] is int dn ? dn:-1;
            if (d == -1) continue;

            //どこかで１多く設定されてるので泣く泣くのー１；； 2026.1.22 U.Hiroto
            //selectSystem::Updateで+1を発見&修正 2026.1.22 U.Hiroto
            Debug.Log($"{d} == {PlayerPrefs.GetInt("engineerNum")}");
            // 自身と同番号のドライバーを探す
            if (d == PlayerPrefs.GetInt("engineerNum"))
            {
                // PlayerViewID が設定済みならpairViewIDに保存
                if (p.CustomProperties.ContainsKey("PlayerViewID"))
                {
                    pairViewID = p.CustomProperties["PlayerViewID"] is int pairViewId ? pairViewId : -1;
                    pairPlayer = p;
                    Debug.Log("FOUND PAIR! pairID:" + pairViewID);

                    //PhotonViewの有効性を確認
                    PhotonView pairPhotonView = PhotonView.Find(pairViewID);
                    if (pairPhotonView == null)
                    {
                        Debug.Log($"無効なID：{pairViewID}");
                        pairViewID = -1;
                        return;
                    }
                    else
                    {
                        Debug.Log($"有効なID：{pairViewID} , {players.Length}人の中からペアを発見");
                    }

                    //カメラの追従
                    SetCamera();

                    //ペアの検索が完了で通知をする　１回のみ実行
                    if (!isNotifyEngineerConnected && PlayerPrefs.GetInt("engineerNum") != -1 && photonView != null)
                    {
                        //マスタークライアントへエンジニアの生成を通知する
                        PhotonView startPosPv = GameObject.Find("StartPos").GetComponent<PhotonView>();

                        startPosPv.RPC("RPC_NotifyEngineerConnected", RpcTarget.AllBuffered);

                        isNotifyEngineerConnected = true;
                    }
                }
                else
                {
                    Debug.Log("FOUND PAIR BUT PlayerViewID is not set.");
                }
                break;
            }
        }
        */

        CarController[] cars = FindObjectsOfType<CarController>();

        Debug.Log($"{cars.Length}人の中からペアを検索");

        foreach (var car in cars)
        {
            var carPv = car.GetComponent<PhotonView>();
            Player searchPlayer = carPv.Owner;
            if(searchPlayer.CustomProperties.TryGetValue("driverNum",out var propDn) && propDn is int)
            {
                //チーム番号の照合
                if((int)propDn == PlayerPrefs.GetInt("engineerNum"))
                {
                    pairViewID = carPv.ViewID;
                    pairPlayer = searchPlayer;

                    Debug.Log("FOUND PAIR! pairID:" + pairViewID);

                    //PhotonViewの有効性を確認
                    PhotonView pairPhotonView = PhotonView.Find(pairViewID);
                    if (pairPhotonView == null)
                    {
                        Debug.Log($"無効なID：{pairViewID}");
                        pairViewID = -1;
                        return;
                    }
                    else
                    {
                        Debug.Log($"有効なID：{pairViewID} , {cars.Length}人の中からペアを発見");
                    }

                    //カメラの追従
                    SetCamera();

                    //ペアの検索が完了で通知をする　１回のみ実行
                    if (!isNotifyEngineerConnected && PlayerPrefs.GetInt("engineerNum") != -1 && photonView != null)
                    {
                        //マスタークライアントへエンジニアの生成を通知する
                        PhotonView startPosPv = GameObject.Find("StartPos").GetComponent<PhotonView>();

                        startPosPv.RPC("RPC_NotifyEngineerConnected", RpcTarget.AllBuffered , photonView.ViewID);

                        isNotifyEngineerConnected = true;

                        // ペアのドライバーのミニマップ上強調UIを有効化
                        car.SetMapFrame();
                    }
                }
            }
        }

        if (pairPlayer == null)
        {
            Debug.Log("Pair is null");
        }
    }

    //カメラの設定
    public void SetCamera()
    {
        Debug.Log($"=== SetCamera Debug ===");

        //シングルプレイ時の処理
        if (!PhotonNetwork.IsConnected)
        {
            var singleCameraController = GameObject.Find("MiniMapCamera").GetComponent<MiniMapCamera>();

            // ペアのドライバーのミニマップ上強調UIを有効化
            carController.SetMapFrame();

            if (singleCameraController != null)
                singleCameraController.SetTarget(carController.transform);
            return;
        }

        Debug.Log($"現在のシーン: {SceneManager.GetActiveScene().name}");
        Debug.Log($"pairViewID: {pairViewID}");
        Debug.Log($"pairPlayer: {pairPlayer?.NickName}");

        // シーン内の全PhotonViewを列挙
        PhotonView[] allViews = FindObjectsOfType<PhotonView>();
        Debug.Log($"シーン内のPhotonView数: {allViews.Length}");
        foreach (var pv in allViews)
        {
            Debug.Log($"  ViewID={pv.ViewID}, Owner={pv.Owner?.NickName}, Name={pv.gameObject.name}, Scene={pv.gameObject.scene.name}");
        }

        // 目的のPhotonViewを検索
        PhotonView pairPhotonView = PhotonView.Find(pairViewID);
        Debug.Log($"PhotonView.Find({pairViewID}) の結果: {(pairPhotonView != null ? "見つかった" : "null")}");

        if (pairPhotonView == null)
        {
            Debug.LogError($"ViewID={pairViewID}が見つかりません");
            //return;
        }

        Debug.Log($"Set Camera to {pairViewID}");

        var cameraController = GameObject.Find("MiniMapCamera").GetComponent<MiniMapCamera>();
        if (cameraController != null)
        {
            pairPhotonView = PhotonView.Find(pairViewID);
            if (pairPhotonView == null)
            {
                Debug.LogError($"ViewID={pairViewID}のPhotonViewが見つかりません");
                return;
            }
            cameraController.SetTarget(PhotonView.Find(pairViewID).transform);
        }
        else
            Debug.Log("cameraController is null");
    }

    public void SendItem(PartsID id)
    {
        //シングルプレイの処理
        if (!PhotonNetwork.IsConnected)
        {
            // キューに追加
            carController.RPC_EnqueueItem(id);
            RPC_RemoveUsedItem(id);
            // 即座に生成
            carController.RemoveUsedItem();
            return;
        }

        //ドライバーにアイテム送信
        Debug.Log("ドライバーに送信するパーツID:" + id);

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーのアイテムキューにアイテムを追加
        target.RPC("RPC_EnqueueItem", pairPlayer, id);
    }

    // パネルから外したアイテムをキューから削除
    public void RemoveItem(PartsID id)
    {
        //シングルプレイの処理
        if (!PhotonNetwork.IsConnected)
        {
            var carController = FindObjectOfType<CarController>();
            carController.RPC_RemoveItem(id);
            return;
        }

        Debug.Log("削除するパーツID:" + id);

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーのアイテムキューからアイテムを削除
        target.RPC("RPC_RemoveItem", pairPlayer, id);
    }

    public void SubstractPartsNum()
    {
        //シングルプレイの処理
        if(!PhotonNetwork.IsConnected)
        {
            var carController = FindObjectOfType<CarController>();
            carController.RPC_RemovePartsNum();
            return;
        }

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーのアイテムキューからアイテムを削除
        target.RPC("RPC_RemovePartsNum", pairPlayer);
    }

    // ドライバーの未設置パーツ数を増やす
    public void AddPartsNum()
    {
        //シングルプレイの処理
        if (!PhotonNetwork.IsConnected)
        {
            var carController = FindObjectOfType<CarController>();
            carController.RPC_AddPartsNum();
            return;
        }

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーの未設置パーツ数を増やす
        target.RPC("RPC_AddPartsNum", pairPlayer);
    }

    // ドライバーにパッシブの強化状態を送信
    public void SetPassiveState(PartsID id,bool isAdd)
    {
        //シングルプレイの処理
        if (!PhotonNetwork.IsConnected)
        {
            var carController = FindObjectOfType<CarController>();
            carController.SetPassiveState(id,isAdd);
            return;
        }

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーの未設置パーツ数を増やす
        target.RPC("RPC_SetPassiveState", pairPlayer, id, isAdd);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("CALL BACK");
        TryPairPlayers();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        //自身のViewIDを登録
        var pv = GetComponent<PhotonView>();
        if(pv.IsMine)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PlayerViewID", pv.ViewID } });
            Debug.Log($"ID登録：{pv.ViewID}");
        }

        //ラップ数の設定　オンラインはカスタムコールバックで取得する
        if (!PhotonNetwork.IsConnected)
        {
            maxLaps = PlayerPrefs.GetInt("lapCnt");
        }
    }

    // 通信用関数
    [PunRPC]
    public void RPC_SpawnParts(PartsID id)
    {
        Debug.Log("Spawn Item Request");
        if (partsManager == null)
        {
            Debug.LogError("PartsManager が見つかりません");
            return;
        }

        GameObject canvas = GameObject.Find("EngineerCanvas");
        partsManager.SpawnParts(id, canvas.transform);
    }

    [PunRPC]
    // 使用したアイテムパーツを削除
    public void RPC_RemoveUsedItem(PartsID id)
    {
        PanelManager panelManager = FindAnyObjectByType<PanelManager>();

        if (panelManager == null)
        {
            Debug.LogError("PanelManager not found");
            return;
        }

        panelManager.RemovePlacedPartsByID(id);
    }

    [PunRPC]
    public void RPC_ReceiveGoalNotif(int id)
    {
        //ペア以外の通知は処理なし
        if (id != pairViewID || !photonView.IsMine) return;

        //リザルトUIを有効化
        if (resultUI.activeSelf == false)
        {
            resultUI.SetActive(true);

            //リザルトUIを表示開始
            var result = resultUI.GetComponent<resultUI>();
            result.SetPairDriverID(id);

            result.SetTextColor(Color.white);
            //result.SetOutLine(0.3f,Color.black);

            result.StartCoroutines();
        }
    }

    [PunRPC]
    public void RPC_NotifLoadFinish()
    {
        if (photonView.IsMine) isLoading = false;
    }
}
