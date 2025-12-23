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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "menu";

        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();
        input.text = PlayerPrefs.GetString("PlayerName");

        debugMessage = GameObject.Find("DebugMessage").GetComponent<TextMeshProUGUI>();
        debugMessage.color = Color.black;

        playersCountText = GameObject.Find("PlayerCountText").GetComponent<TextMeshProUGUI>();
        playersCountText.color = Color.black;

        roomNameText = GameObject.Find("RoomNameText").GetComponent<TextMeshProUGUI>();
        roomNameText.color = Color.black;

        monitorsCounter = GameObject.Find("monitorsCounter").transform.Find("Text").GetComponent<TextMeshProUGUI>();
    }

    private void Awake()
    {
        base.Awake();

        Debug.Log("=== SELECT SCENE AWAKE ===");
    }

    private void Update()
    {
        //人数表示の更新
        string curPlayresNum = PhotonNetwork.PlayerList.Length.ToString();
        string maxPlayresNum = limitPlayers.ToString();
        playersCountText.text = curPlayresNum + " / " + maxPlayresNum;

        //観戦者数の更新　超過人数を観戦者としてカウント
        monitorsCounter.text = "×" + (Mathf.Max(PhotonNetwork.PlayerList.Length - limitPlayers,0)).ToString();

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
            debugMessage.text = "NOW SELECT : NONE";
        }
        else
        {
            if (selectDriverNum != -1)
            {
                debugMessage.text = "NOW SELECT : DRIVER" + (selectDriverNum + 1);
            }
            else
            {
                debugMessage.text = "NOW SELECT : ENGINEER" + (selectEngineerNum + 1);
            }
        }
    }

    //観戦者の更新処理
    private void monitorUpdate()
    {
        debugMessage.text = "YOU ARE MONITOR";
    }

    public void PushStartButton()
    {
        // ネットワークオブジェクトにdriverNumとengineerNumを登録
        Hashtable hash = new Hashtable();
        hash["driverNum"] = PlayerPrefs.GetInt("driverNum");
        hash["engineerNum"] = PlayerPrefs.GetInt("engineerNum");

        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

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

    IEnumerator LoadGameScene()
    {
        // 1フレーム返す
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync("gamePlay");
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }
    }

    public void PushReadyButton()
    {
        //観戦者は処理なし
        if (ss == null) return;

        ss.PushedReady();

        var text = readyButtonText.GetComponent<TextMeshProUGUI>();
        if(text == null)
        {
            Debug.Log("NOT FOUND TEXT");
            return;
        }

        if (ss.IsReady())
        {
            text.text = "CANCEL";
        }
        else
        {
            text.text = "READY";
        }
    }

    public void InputText()
    {
        //共通処理
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

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
                text.text = "READY";
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
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        //ルーム名の表示
        roomNameText.text = $"RoomName:\n{PhotonNetwork.CurrentRoom.Name}";

        //マスターならプレイ人数を設定　そうでなければ受信
        if (PhotonNetwork.IsMasterClient)
        {
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
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "MasterClienViewID", pv.ViewID } });
            }

            //マスターはSSのプロパティコールバックで行う　ここでやるとまだViewIDを参照できないため
            if (!PhotonNetwork.IsMasterClient)
            {
                //準備状態の初期化
                ss.SendToMaster(false);
                Debug.Log("準備状態の初期化");
            }
        }
    }

    //カスタムプロパティのコールバック
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient && changedProps.TryGetValue("limitPlayers", out var v))
        {
            limitPlayers = (int)v;
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

    ~selectScene()
    {
    }
}
