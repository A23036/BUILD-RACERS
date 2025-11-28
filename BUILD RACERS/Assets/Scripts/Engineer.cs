using Photon.Pun;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Engineer : MonoBehaviourPunCallbacks
{
    int engineerNum = -1;
    private Player pairPlayer;
    private PartsManager partsManager;
    private int pairID;
    
    public int GetEngineerNum() => engineerNum;

    void Awake()
    {
        partsManager = GetComponentInChildren<PartsManager>();

        engineerNum = PlayerPrefs.GetInt("engineerNum");
        
        PhotonView pv = GetComponent<PhotonView>();

        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "PlayerViewID", pv.ViewID } });

        Debug.Log("My ViewID: " + pv.ViewID);
    }

    // í êMópä÷êî
    [PunRPC]
    public void RPC_SpawnItem(PartsID id)
    {
        Debug.Log("Spawn Item Request");
        if (partsManager == null)
        {
            Debug.LogError("PartsManager Ç™å©Ç¬Ç©ÇËÇ‹ÇπÇÒ");
            return;
        }

        GameObject canvas = GameObject.Find("EngineerCanvas");
        partsManager.SpawnParts(id, canvas.transform);
    }
}
