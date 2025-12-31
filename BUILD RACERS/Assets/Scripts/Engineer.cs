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
        if (!photonView.IsMine) return;

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
        carController = FindObjectOfType<CarController>();
    }

    private void Update()
    {
        TryPairPlayers();
    }

    private void TryPairPlayers()
    {
        //シングルプレイの処理
        if(!PhotonNetwork.IsConnected)
        {
            var cameraController = GameObject.Find("MiniMapCamera").GetComponent<MiniMapCamera>();
            var carController = FindObjectOfType<CarController>();
            if (cameraController != null && carController != null)
                cameraController.SetTarget(carController.transform);

            if(cameraController == null)
            {
                Debug.Log("cameraController is null");
            }

            if(carController == null)
            {
                Debug.Log("kart is null");
            }

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
        var cameraController = GameObject.Find("MiniMapCamera").GetComponent<MiniMapCamera>();
        if (cameraController != null)
            cameraController.SetTarget(PhotonView.Find(pairViewID).transform);
        else
            Debug.Log("cameraController is null");
    }

    public void SendItem(PartsID id)
    {
        //ドライバーにアイテム送信
        Debug.Log("ドライバーに送信するパーツID:" + id);

        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーのアイテムキューにアイテムを追加
        target.RPC("RPC_EnqueueItem", pairPlayer, id);
    }

    public void RemoveItem(PartsID id)
    {
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
        PhotonView target = PhotonView.Find(pairViewID);

        if (target == null) Debug.Log("target is null");
        if (pairPlayer == null) Debug.Log("pair player is null");
        if (photonView == null) Debug.Log("photon view is null");

        // ペアのドライバーのアイテムキューからアイテムを削除
        target.RPC("RPC_RemovePartsNum", pairPlayer);
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
}
