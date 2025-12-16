using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Engineer : MonoBehaviourPunCallbacks
{
    private PartsManager partsManager;
    MiniMapCamera miniMapCamera;

    private Player pairPlayer = null;
    private int pairViewID = -1;

    void Awake()
    {
        partsManager = GetComponentInChildren<PartsManager>();
        
        PhotonView pv = GetComponent<PhotonView>();

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PlayerViewID", pv.ViewID } });

        Debug.Log("My ViewID: " + pv.ViewID);
    }

    private void TryPairPlayers()
    {
        // ペアを発見済みの場合、処理を行わない
        if (pairViewID != -1) return;

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

                    // ミニマップカメラに対象を指定
                    PhotonView targetPV = PhotonView.Find(pairViewID);
                    miniMapCamera = FindAnyObjectByType<MiniMapCamera>();
                    miniMapCamera.SetTarget(targetPV.transform);
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
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("CALL BACK");
        TryPairPlayers();
    }

    // 通信用関数
    [PunRPC]
    public void RPC_SpawnItem(PartsID id)
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
