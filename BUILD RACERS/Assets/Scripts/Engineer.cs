using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Engineer : MonoBehaviourPunCallbacks
{
    private PartsManager partsManager;
    private PanelManager panelManager;

    private CarController carController;
    private Player pairPlayer = null;
    private int pairViewID = -1;

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

        if (PlayerPrefs.GetInt("engineerNum") != -1) PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PlayerViewID", pv.ViewID } });

        Debug.Log("My ViewID: " + pv.ViewID);

        carController = null;
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        carController = FindObjectOfType<CarController>();

        partsManager = GetComponentInChildren<PartsManager>();

        panelManager = GameObject.Find("PanelManager").GetComponent<PanelManager>();
        panelManager.SetEngineer(this);

        TryPairPlayers();
    }

    private void Update()
    {
        
    }

    private void TryPairPlayers()
    {
        if (!PhotonNetwork.IsConnected)
        {
            return;
        }

        // ペアを発見済みの場合、処理を行わない
        if (pairViewID != -1 || !photonView.IsMine) return;

        Player[] players = PhotonNetwork.PlayerList;

        // ネットワークに接続中のplayerを一人ずつ調査
        foreach (var p in players)
        {
            // エンジニアはcontinue(ドライバーのみ探す)
            int d = p.CustomProperties["driverNum"] is int dn ? dn:-1;
            if (d == -1) continue;

            // 自身と同番号のドライバーを探す
            if (d == PlayerPrefs.GetInt("engineerNum"))
            {
                // PlayerViewID が設定済みならpairViewIDに保存
                if (p.CustomProperties.ContainsKey("PlayerViewID"))
                {
                    pairViewID = p.CustomProperties["PlayerViewID"] is int pairViewId ? pairViewId : -1;
                    pairPlayer = p;
                    Debug.Log("FOUND PAIR! pairID:" + pairViewID);

                    //カメラの追従
                    SetCamera();
                }
                else
                {
                    Debug.Log("FOUND PAIR BUT PlayerViewID is not set.");
                }
                break;
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
        //シングルプレイ時の処理
        if (!PhotonNetwork.IsConnected)
        {
            var singleCameraController = GameObject.Find("MiniMapCamera").GetComponent<MiniMapCamera>();

            //シングルプレイ時の相方取得
            carController = FindObjectOfType<CarController>();
            if (singleCameraController != null)
                singleCameraController.SetTarget(carController.transform);
            return;
        }

        var cameraController = GameObject.Find("MiniMapCamera").GetComponent<MiniMapCamera>();
        if (cameraController != null)
            cameraController.SetTarget(PhotonView.Find(pairViewID).transform);
        else
            Debug.Log("cameraController is null");
    }

    public void SendItem(PartsID id)
    {
        //シングルプレイの処理
        if (!PhotonNetwork.IsConnected)
        {
            var carController = FindObjectOfType<CarController>();
            carController.RPC_EnqueueItem(id);
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

}
