using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class selectScene : baseScene
{
    [SerializeField] private GameObject readyButtonText;

    //セレクターの上限数　これ以上の接続は観戦者にまわす
    [SerializeField] private int limitPlayers;

    //ラップ数を設定
    [SerializeField] private GameObject lapSetter;

    //セレクター関係
    private GameObject selector = null;
    private selectSystem ss = null;

    //観戦者関係
    private GameObject monitor = null;
    private monitorSystem ms = null;

    //UIテキスト
    private TextMeshProUGUI debugMessage = null;
    private TextMeshProUGUI playersCountText;
    private TextMeshProUGUI monitorsCounter;
    private TextMeshProUGUI roomNameText;

    //現在のルームの状態
    private string nowRoomStat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "menu";

        GameObject inputField = GameObject.Find("InputField (TMP)");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();
        input.text = PlayerPrefs.GetString("PlayerName");

        debugMessage = GameObject.Find("DebugMessage").GetComponent<TextMeshProUGUI>();
        debugMessage.color = Color.black;

        playersCountText = GameObject.Find("PlayerCountText").GetComponent<TextMeshProUGUI>();
        playersCountText.color = Color.black;

        roomNameText = GameObject.Find("RoomNameText").GetComponent<TextMeshProUGUI>();
        roomNameText.color = Color.black;

        monitorsCounter = GameObject.Find("monitorsCounter").transform.Find("Text").GetComponent<TextMeshProUGUI>();

        nowRoomStat = "";
    }

    private void Awake()
    {
        base.Awake();

        Debug.Log("=== SELECT SCENE AWAKE ===");
    }

    private void Update()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("masterGameScene", out var stat);
            if (stat is string) nowRoomStat = (string)stat;
        }

        //人数表示の更新
        string curPlayresNum = FindObjectsOfType<selectSystem>().Length.ToString();
        string maxPlayresNum = limitPlayers.ToString();
        playersCountText.text = curPlayresNum + " / " + maxPlayresNum;

        //観戦者数の更新　超過人数を観戦者としてカウント
        monitorsCounter.text = FindObjectsOfType<monitorSystem>().Length.ToString();

        //プレイヤーか観戦者かによって処理を分岐
        if (ss != null) selectorUpdate();
        else if (ms != null) monitorUpdate();
    }

    //セレクターの更新処理
    private void selectorUpdate()
    {
        ss.GetNums(out int dn, out int bn);
        int selectDriverNum = dn, selectEngineerNum = bn;
        if (selectDriverNum == -1 && selectEngineerNum == -1)
        {
            debugMessage.text = "選んでいないよ";
        }
        else
        {
            if (selectDriverNum != -1)
            {
                debugMessage.text = "参加中: ドライバー" + (selectDriverNum + 1);
            }
            else
            {
                debugMessage.text = "参加中: エンジニア" + (selectEngineerNum + 1);
            }
        }
    }

    //観戦者の更新処理
    private void monitorUpdate()
    {
        debugMessage.text = "参加中:観戦者";
    }

    public void PushStartButton()
    {
        // ネットワークオブジェクトにdriverNumとengineerNumを登録
        Hashtable hash = new Hashtable();
        hash["driverNum"] = PlayerPrefs.GetInt("driverNum");
        hash["engineerNum"] = PlayerPrefs.GetInt("engineerNum");

        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        //マスターはラップ数を共有
        if(PhotonNetwork.IsMasterClient)
        {
            int lapCnt = lapSetter.GetComponent<LapSetter>().GetLapCnt();

            hash = new Hashtable();
            hash["lapCnt"] = lapCnt;
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }

        //名前が一文字以上でシーン遷移
        if (PlayerPrefs.GetString("PlayerName").Length > 0)
        {
            //観戦者フラグをたてる
            if(monitor != null)
            {
                PlayerPrefs.SetInt("isMonitor", 1);
            }
            else
            {
                PlayerPrefs.SetInt("isMonitor", 0);
            }

            Debug.Log(hash["driverNum"] + "," + hash["engineerNum"] + "," + PlayerPrefs.GetInt("isMonitor"));

            SceneManager.LoadScene("gamePlay");
            //StartCoroutine(LoadGameScene());
        }
    }

    public void PushReadyButton()
    {
        //観戦者は処理なし
        if (ss == null) return;

        //未選択なら処理なし
        ss.GetNums(out int dn, out int bn);
        if (dn == -1 && bn == -1) return;

        ss.PushedReady();

        var text = readyButtonText.GetComponent<TextMeshProUGUI>();
        if(text == null)
        {
            Debug.Log("NOT FOUND TEXT");
            return;
        }

        if (ss.IsReady())
        {
            text.text = "キャンセル";
            text.color = Color.black;
        }
        else
        {
            text.text = "準備OK!";
            text.color = Color.red;
        }
    }

    public void InputText()
    {
        //共通処理
        GameObject inputField = GameObject.Find("InputField (TMP)");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        //ネームバーの文字数制限
        int nameLimitNum = 8;
        if(input.text.Length > nameLimitNum) input.text = input.text.Substring(0, nameLimitNum);

        //セレクターか観戦者によって処理を分岐
        if(ss != null)
        {
            //ゲーミングカラー
            if (input.text == "rainbow")
            {
                ss.GamingMode(true);
                input.text = "";
            }
            else
            {
                ss.GamingMode(false);
            }

            //プレイヤーの名前を保存
            PlayerPrefs.SetString("PlayerName", input.text);

            //文字数が0なら準備状態を解除する
            if (input.text.Length == 0)
            {
                ss.SetReady(false);

                var text = readyButtonText.GetComponent<TextMeshProUGUI>();
                text.text = "準備OK！";
            }

            ss.UpdateNameBar();
        }
        else if(ms != null)
        {
            //ゲーミングカラー
            if (input.text == "rainbow")
            {
                ms.GamingMode(true);
                input.text = "";
            }
            else
            {
                ms.GamingMode(false);
            }

            //プレイヤーの名前を保存
            PlayerPrefs.SetString("PlayerName", input.text);

            ms.UpdateNameBar();
        }

        PhotonNetwork.LocalPlayer.NickName = input.text;
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        //ルーム名の表示
        roomNameText.text = $"部屋の名前:\n{PhotonNetwork.CurrentRoom.Name}";

        //マスターならプレイ人数を設定　そうでなければ受信
        if (PhotonNetwork.IsMasterClient)
        {
            limitPlayers = PlayerPrefs.GetInt("roomLimitPlayers");
            Debug.Log("INITIALIZE limitPlayers TO " + limitPlayers);

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "limitPlayers", limitPlayers } });
        }
        else
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("limitPlayers", out var v))
            {
                limitPlayers = (int)v;
            }
        }

        //人数制限を設ける
        if (PhotonNetwork.PlayerList.Length > limitPlayers)
        {
            //観戦者処理
            monitor = PhotonNetwork.Instantiate("Monitor", new Vector3(-1000, -1000, -1000), Quaternion.identity);
            ms = monitor.GetComponent<monitorSystem>();
            ms.DecideColor();
        }
        else
        {
            //選択アイコンの生成　最初は画面外に生成
            selector = PhotonNetwork.Instantiate("Selector", new Vector3(-1000, -1000, -1000), Quaternion.identity);
            ss = selector.GetComponent<selectSystem>();
            ss.DecideColor();

            if (PhotonNetwork.IsMasterClient)
            {
                //マスタークライアントのIDを登録
                PhotonView pv = selector.GetComponent<PhotonView>();
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "MasterClientViewID", pv.ViewID } });
            }

            //マスターはSSのプロパティコールバックで行う　ここでやるとまだViewIDを参照できないため
            if (!PhotonNetwork.IsMasterClient)
            {
                //準備状態の初期化
                ss.SendToMaster(false);
                Debug.Log("準備状態の初期化");
            }
        }

        //ラップ数を決めるオブジェクト　マスター以外は表示しない
        if (PhotonNetwork.IsMasterClient) lapSetter.SetActive(true);
    }

    //カスタムプロパティのコールバック
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient && changedProps.TryGetValue("limitPlayers", out var v) && v is int)
        {
            limitPlayers = (int)v;
        }

        //ルームの状態を更新
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if(props.TryGetValue("masterGameScene" , out var scene) && scene is string str && str == "select")
        {
            nowRoomStat = "待機中";
        }
        else
        {
            nowRoomStat = "";
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (photonView == null) return;

        if (photonView.IsMine && PhotonNetwork.IsMasterClient)
        {
            //ルームの上限数を更新
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { "limitPlayers", limitPlayers } });
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //ラップ数を決めるオブジェクト　マスター以外は表示しない
        lapSetter.SetActive(PhotonNetwork.IsMasterClient);
    }

    //空きがあればセレクターを生成
    public void CheckAndSpawnSelector()
    {
        if (PhotonNetwork.PlayerList.Length > limitPlayers)
        {
            return;
        }

        //生成する空きがあれば観戦者を削除してセレクター生成
        PhotonNetwork.Destroy(monitor);

        //選択アイコンの生成　最初は画面外に生成
        selector = PhotonNetwork.Instantiate("Selector", new Vector3(-1000, -1000, -1000), Quaternion.identity);
        ss = selector.GetComponent<selectSystem>();
        ss.DecideColor();
    }

    public void PushBackButton()
    {
        ss.DeleteMyStat();

        base.PushBackButton();
    }

    public void PushMonitorButton()
    {
        Debug.Log("NOW ROOM STAT : " + nowRoomStat);

        //ルームの状態がStartingなら処理なし
        if (nowRoomStat != "待機中") return;

        SwitchSide();
    }

    public void SwitchSide()
    {
        //観戦とプレイヤーを切り替え
        Debug.Log("Switch Monitor");

        if (monitor != null)
        {
            SpawnSelector();
        }
        else if(selector != null)
        {
            //選択を解放する処理
            ss.ReleaseSlotAll(PhotonNetwork.LocalPlayer.ActorNumber);

            //準備状態を削除
            var pv = ss.GetComponent<PhotonView>();
            pv.RPC("RPC_ReleaseSelectorStat", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);

            //観戦生成
            SpawnMonitor();
        }
        else
        {
            Debug.Log("プレイヤーでも観戦でもない状態");
        }
    }

    public void SpawnSelector()
    {
        //生成する空きがあれば観戦者を削除してセレクター生成
        PhotonNetwork.Destroy(monitor);

        selector = PhotonNetwork.Instantiate("Selector", new Vector3(-1000, -1000, -1000), Quaternion.identity);
        ss = selector.GetComponent<selectSystem>();
        ss.DecideColor();
    }

    public void SpawnMonitor()
    {
        //生成する空きがあれば観戦者を削除してセレクター生成
        PhotonNetwork.Destroy(selector);

        monitor = PhotonNetwork.Instantiate("Monitor", new Vector3(-1000, -1000, -1000), Quaternion.identity);
        ms = monitor.GetComponent<monitorSystem>();
        ms.DecideColor();
    }

    ~selectScene()
    {
    }
}
