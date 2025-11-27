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
        engineerNum = PlayerPrefs.GetInt("engineerNum");

        partsManager = GetComponentInChildren<PartsManager>();

        PhotonView pv = GetComponent<PhotonView>();
        pv.ViewID = engineerNum;
        pairID = engineerNum - 8;
        Debug.Log("Pair Driver ViewID: " + pairID);
    }

    // ’ÊM—pŠÖ”
    [PunRPC]
    public void RPC_SpawnItem(PartsID id)
    {
        Debug.Log("Spawn Item Request");
        if (partsManager == null)
        {
            Debug.LogError("PartsManager ‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñ");
            return;
        }

        partsManager.SpawnParts(id);
    }
}
