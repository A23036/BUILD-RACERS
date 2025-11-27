using Photon.Pun;

using Photon.Realtime;
using UnityEngine;

using System.Collections.Generic;

public class SelectorManager : MonoBehaviourPunCallbacks
{
    private Dictionary<int, bool> selectorsStat;
    private bool isEveryoneReady;
    private float startTimer;

    [SerializeField] private int masterSelectorViewID;
    [SerializeField] private float timeUntilStart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void Awake()
    {
        isEveryoneReady = false;
        startTimer = 0f;

        if (PhotonNetwork.IsMasterClient)
        {
            //マスタークライアントのIDを登録
            PhotonView pv = GetComponent<PhotonView>();
            pv.ViewID = masterSelectorViewID;
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
        }
        else
        {
            startTimer = timeUntilStart;
        }

        if(startTimer <= 0f)
        {
            //シーン遷移
            var sm = GameObject.Find("SceneMananger").GetComponent<selectScene>();
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "isEveryoneReady", isEveryoneReady } });
        }
    }

    //他プレイヤーがルームに参加したときに呼ばれるコールバック
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
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
