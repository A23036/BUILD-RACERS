using Photon.Pun;

using Photon.Realtime;
using UnityEngine;

public class SelectorManager : MonoBehaviourPunCallbacks
{
    private Photon.Realtime.Player[] selectors;
    private bool isEveryoneReady;

    [SerializeField] private int masterSelectorViewID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Awake()
    {
        isEveryoneReady = false;

        if (PhotonNetwork.IsMasterClient)
        {
            GetSelectors();

            //マスタークライアントのIDを登録
            PhotonView pv = GetComponent<PhotonView>();
            pv.ViewID = masterSelectorViewID;
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable{{"MasterSelectorViewID", pv.ViewID}});
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //他プレイヤーがルームに参加したときに呼ばれるコールバック
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GetSelectors();
        }
    }

    private void GetSelectors()
    {
        selectors = PhotonNetwork.PlayerList;
    }

    [PunRPC]
    public void RPC_OnSelectorChanged(bool isReady, int senderID)
    {
        //ルームマスター以外は処理なし
        if(!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("ROOM MASTER");
            return;
        }

        Debug.Log("SELECTOR ID : " + senderID + " , " + "STAT : " + (isReady ? "READY" : "NOT READY"));
    }
}
