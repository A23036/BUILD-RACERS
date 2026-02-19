using JetBrains.Annotations;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SelectorManager : MonoBehaviourPunCallbacks, IPunObservable
{
    private Dictionary<int, bool> selectorsStat;
    private bool isEveryoneReady;
    private float startTimer;

    [SerializeField] private float timeUntilStart;

    private TMP_InputField commentInput;
    private selectSystem ss;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        selectScene sm = GameObject.Find("SceneManager").GetComponent<selectScene>();
        
        if(SceneManager.GetActiveScene().name == "select")
        {
            commentInput = sm.GetCommentInput();
        }
        ss = GetComponent<selectSystem>();

        //ソロフラグの初期化
        if(PhotonNetwork.InRoom)
        {
            PhotonNetwork.LocalPlayer.CustomProperties["isSolo"] = true;
        }
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
        if(!photonView.IsMine || !PhotonNetwork.IsMasterClient) return;

        //プレースホルダー更新
        if(commentInput != null)
        {
            if (ss.IsReady()) commentInput.placeholder.GetComponent<TextMeshProUGUI>().text = "他のプレイヤーを待っています";
            else commentInput.placeholder.GetComponent<TextMeshProUGUI>().text = "コメントしよう！";
        }

        //全員が準備完了ならタイマーが作動
        if (isEveryoneReady)
        {
            startTimer -= Time.deltaTime;
            Debug.Log("TIMER START");

            //プレースホルダー更新
            if(commentInput != null) commentInput.placeholder.GetComponent<TextMeshProUGUI>().text = "ゲーム開始中...";
        }
        else
        {
            startTimer = timeUntilStart;
            Debug.Log("TIMER STOP");
        }

        if (startTimer <= 0f)
        {
            //シーン遷移
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "isEveryoneReady", isEveryoneReady } });
        }
    }

    void FixedUpdate()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isEveryoneReady = false;

        //準備状態の確認
        selectSystem[] selectors = FindObjectsOfType<selectSystem>();
        foreach(var ss in selectors)
        {
            if (ss.IsReady() == false)
            {
                isEveryoneReady = false;

                //ルームの状態をWaitingに変更
                var propsw = new Hashtable();
                propsw["masterGameScene"] = "Waiting";
                PhotonNetwork.CurrentRoom.SetCustomProperties(propsw);

                Debug.Log($"Set {propsw["masterGameScene"]}");

                return;
            }
        }

        //全員準備完了
        isEveryoneReady = true;

        //ペア検索
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
                foreach (var p in PhotonNetwork.PlayerList)
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
        //プレイヤーの分布
        Dictionary<int, Player> driversDist = new Dictionary<int, Player>();
        Dictionary<int, Player> engineersDist = new Dictionary<int, Player>();

        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("driverNum", out var d) && (int)d != -1)
            {
                driversDist[(int)d] = p;
            }
            if (p.CustomProperties.TryGetValue("engineerNum", out var e) && (int)e != -1)
            {
                engineersDist[(int)e] = p;
            }
        }

        //ペアの有無を確認　ペア検索の実行の際に使用
        foreach (var p in driversDist)
        {
            if (engineersDist.TryGetValue(p.Key, out var engineer))
            {
                p.Value.CustomProperties["isSolo"] = false;
            }
        }
        foreach (var p in engineersDist)
        {
            if (driversDist.TryGetValue(p.Key, out var engineer))
            {
                p.Value.CustomProperties["isSolo"] = false;
            }
        }

        var players = PhotonNetwork.PlayerList;
        foreach (var p in players)
        {
            bool? ret = null;
            if (p.CustomProperties.TryGetValue("isSolo", out var b)) ret = (bool)b;

            if (ret != null && ret == true) Debug.Log($"{p.NickName} is SOLO");
            else if (ret != null && ret == false) Debug.Log($"{p.NickName} is NOT SOLO");
            else if (ret == null) Debug.Log($"{p.NickName} has no SOLO info");
        }

        //接続数と登録数
        Debug.Log($"Connect:{PhotonNetwork.PlayerList.Length} , regist:{selectorsStat.Count}");
        foreach (var vk in selectorsStat)
        {
            Debug.Log($"{vk.Key} is {vk.Value}");
        }

        Debug.Log("SELECTOR ID : " + senderID + " , " + "STAT : " + (isReady ? "READY" : "NOT READY"));

        selectorsStat[senderID] = isReady;
        foreach (var vk in selectorsStat)
        {
            Debug.Log($"{vk.Key} is {vk.Value}");
        }

        Debug.Log("準備状態の配列数：" + selectorsStat.Count);

        selectSystem[] selectors = FindObjectsOfType<selectSystem>();
        foreach (var ss in selectors)
        {
            PhotonView pv = ss.GetComponent<PhotonView>();
            int actor = 0;
            if (pv != null && pv.Owner != null)
            {
                actor = pv.Owner.ActorNumber;
            }

            ///*
            if (ss.IsReady() == false)
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
            //*/

            isEveryoneReady = true;
        }

        if (isEveryoneReady && PhotonNetwork.IsMasterClient)
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
                if (p.CustomProperties.TryGetValue("driverNum", out var d) && (int)d != -1)
                {
                    drivers++;
                    driversDist[(int)d] = p;
                }
                if (p.CustomProperties.TryGetValue("engineerNum", out var e) && (int)e != -1)
                {
                    engineers++;
                    engineersDist[(int)e] = p;
                }
                if (p.CustomProperties.TryGetValue("isMonitor", out var m) && (int)m == 1)
                {
                    monitors++;
                }
            }

            //ペアの有無を確認　ペア検索の実行の際に使用
            foreach (var p in driversDist)
            {
                if (engineersDist.TryGetValue(p.Key, out var engineer))
                {
                    p.Value.CustomProperties["isSolo"] = false;
                }
            }
            foreach (var p in engineersDist)
            {
                if (driversDist.TryGetValue(p.Key, out var engineer))
                {
                    p.Value.CustomProperties["isSolo"] = false;
                }
            }

            foreach (var p in players)
            {
                bool? ret = null;
                if (p.CustomProperties.TryGetValue("isSolo", out var b)) ret = (bool)b;

                if (ret != null && ret == true) Debug.Log($"{p.NickName} is SOLO");
                else if (ret != null && ret == false) Debug.Log($"{p.NickName} is NOT SOLO");
                else if(ret == null) Debug.Log($"{p.NickName} has no SOLO info");
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

            //カスタムプロパティが取得できない問題対策
            PlayerPrefs.SetInt("DriversCount", drivers);
            PlayerPrefs.SetInt("EngineersCount", engineers);
            PlayerPrefs.SetInt("MonitorsCount", monitors);
        }
    }

    //切断したセレクターのステータスを削除
    [PunRPC]
    public void RPC_ReleaseSelectorStat(int senderID)
    {
        selectorsStat.Remove(senderID);
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
    }
}
