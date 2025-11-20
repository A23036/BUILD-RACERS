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
        TryPairPlayers();
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
}
