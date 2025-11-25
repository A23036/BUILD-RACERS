using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Engineer : MonoBehaviourPunCallbacks
{
    int engineerNum = -1;
    private Player pairPlayer;
    private PartsManager partsManager;
    
    public int GetEngineerNum() => engineerNum;
    
    void Awake()
    {
        engineerNum = PlayerPrefs.GetInt("engineerNum");

        partsManager = GetComponentInChildren<PartsManager>();

        // ペアを探す
        //TryPairPlayers();
    }

    // ネットワークルームに参加したときに自動で呼ばれる
    public override void OnJoinedRoom()
    {
        var rpcObj = PhotonNetwork.Instantiate("PlayerRPCReceiver", Vector3.zero, Quaternion.identity);
        rpcObj.GetComponent<PhotonView>().Owner.TagObject = rpcObj.GetComponent<PhotonView>();
    }

    private void TryPairPlayers()
    {
        Player[] players = PhotonNetwork.PlayerList;

        foreach (var p in players)
        {
            if (!p.CustomProperties.ContainsKey("driverNum")) continue;

            // 同番号のドライバーを探す
            int d = (int)p.CustomProperties["driverNum"];
            if(d == engineerNum)
            {
                pairPlayer = p;
                Debug.Log("FOUND PAIR!");
                break;
            }
        }
    }

    // 通信用関数
    [PunRPC]
    public void RPC_SpawnItem(int id)
    {
        PartsID partsID = (PartsID)id;

        if (partsManager == null)
        {
            Debug.LogError("PartsManager が見つかりません");
            return;
        }

        partsManager.SpawnParts(partsID);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("CALL BACK");
        if (changed["driverNum"] is int number && number == PlayerPrefs.GetInt("engineerNum"))
        {
            TryPairPlayers();
        }
    }
    
}
