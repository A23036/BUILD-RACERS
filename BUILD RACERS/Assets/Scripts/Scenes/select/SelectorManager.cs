using Photon.Pun;

using Photon.Realtime;
using UnityEngine;

using System.Collections.Generic;

public class SelectorManager : MonoBehaviourPunCallbacks
{
    private Dictionary<int, bool> selectorsStat;
    private bool isEveryoneReady;
    private float startTimer;

    [SerializeField] private float timeUntilStart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Awake()
    {
        isEveryoneReady = false;
        startTimer = timeUntilStart;
        selectorsStat = new Dictionary<int, bool>();

        if (PhotonNetwork.IsMasterClient)
        {
            //マスタークライアントのIDを登録
            PhotonView pv = GetComponent<PhotonView>();
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable{{"MasterSelectorViewID", pv.ViewID}});
        }
    }

    // Update is called once per frame
    void Update()
    {
        //全員が準備完了ならタイマーが作動
        if (isEveryoneReady)
        {
            startTimer -= Time.deltaTime;
            Debug.Log("UNTIL START : " + startTimer);
        }
        else
        {
            startTimer = timeUntilStart;
        }

        if(startTimer <= 0f)
        {
            //シーン遷移
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "isEveryoneReady", isEveryoneReady } });
        }
    }

    //他プレイヤーがルームに参加したときに呼ばれるコールバック
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
    }

    public void MasterSelectorChanged(bool isReady, int senderID)
    {
        selectorsStat[senderID] = isReady;
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

        selectorsStat[senderID] = isReady;

        Debug.Log("準備状態の配列数：" + selectorsStat.Count);

        //全員分の状態が登録されていなければ準備まだ判定
        if (PhotonNetwork.PlayerList.Length != selectorsStat.Count)
        {
            isEveryoneReady = false;
            return;
        }

        //全員が準備完了か判定
        foreach(var vk in selectorsStat)
        {
            if(!vk.Value)
            {
                isEveryoneReady = false;
                break;
            }
            isEveryoneReady = true;
        }
    }
}
