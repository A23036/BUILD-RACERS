using JetBrains.Annotations;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using Hashtable = ExitGames.Client.Photon.Hashtable;

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

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //切断したプレイヤーの準備状態のデータを削除
        RPC_ReleaseSelectorStat(otherPlayer.ActorNumber);
    }

    //ルームマスターが変更された際のコールバック
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
    {
        if (photonView.IsMine && PhotonNetwork.IsMasterClient)
        {
            List<int> removeList = new();

            //削除されてるセレクターの状態を削除
            foreach (var vk in selectorsStat)
            {
                bool isHit = false;
                int id = vk.Key;
                foreach(var p in PhotonNetwork.PlayerList)
                {
                    if (id == p.ActorNumber)
                    {
                        isHit = true;
                        break;
                    }
                }

                if (isHit) continue;

                //見つからなければ削除リストに追加
                removeList.Add(id);
            }

            //削除
            foreach (var id in removeList)
            {
                RPC_ReleaseSelectorStat(id);
            }
        }
    }

    public void MasterSelectorChanged(bool isReady, int senderID)
    {
        selectorsStat[senderID] = isReady;
    }

    [PunRPC]
    public void RPC_OnSelectorChanged(bool isReady, int senderID)
    {
        //接続数と登録数
        Debug.Log($"Connect:{PhotonNetwork.PlayerList.Length} , regist:{selectorsStat.Count}");

        //ルームマスター以外は処理なし
        if(!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("NOT ROOM MASTER");
            return;
        }

        Debug.Log("SELECTOR ID : " + senderID + " , " + "STAT : " + (isReady ? "READY" : "NOT READY"));

        selectorsStat[senderID] = isReady;

        Debug.Log("準備状態の配列数：" + selectorsStat.Count);

        /*
        //全員分の状態が登録されていなければ準備まだ判定
        if (PhotonNetwork.PlayerList.Length != selectorsStat.Count)
        {
            isEveryoneReady = false;
            return;
        }
        */

        selectSystem[] selectors = FindObjectsOfType<selectSystem>();
        foreach (var ss in selectors)
        {
            PhotonView pv = ss.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null)
            {
                int actor = pv.Owner.ActorNumber;
                
                if (!selectorsStat.TryGetValue(actor, out bool b) || !b)
                {
                    Debug.Log(actor + " is not ready");
                    isEveryoneReady = false;

                    //ルームの状態をWaitingに変更
                    var propsw = new Hashtable();
                    propsw["masterGameScene"] = "Waiting";
                    PhotonNetwork.CurrentRoom.SetCustomProperties(propsw);

                    Debug.Log($"Set {propsw["masterGameScene"]}");

                    break;
                }
                isEveryoneReady = true;
            }
        }

        if (isEveryoneReady)
        {
            //ルームの状態をStartingに変更
            var props = new Hashtable();
            props["masterGameScene"] = "Starting";
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Debug.Log($"Set {props["masterGameScene"]}");

            //ドライバー　エンジニア　観戦者の人数を記録
            int drivers = 0;
            int engineers = 0;
            int monitors = 0;

            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.TryGetValue("driverNum", out var d) && (int)d != -1) drivers++;
                if (p.CustomProperties.TryGetValue("engineerNum", out var e) && (int)e != -1) engineers++;
                if (p.CustomProperties.TryGetValue("isMonitor", out var m) && (int)m == 1) monitors++;
            }

            Debug.Log("人数カウント完了");
            Debug.Log($"Drivers:{drivers} , Engineers:{engineers} , Monitors:{monitors}");

            //ルームプロパティに保存
            Hashtable hash = new Hashtable
            {
                {"DriversCount",drivers },
                {"EngineersCount",engineers },
                {"MonitorsCount",monitors }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }

    //切断したセレクターのステータスを削除
    [PunRPC]
    public void RPC_ReleaseSelectorStat(int senderID)
    {
        selectorsStat.Remove(senderID);
    }
}
