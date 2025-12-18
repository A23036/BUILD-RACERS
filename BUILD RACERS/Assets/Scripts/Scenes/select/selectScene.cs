using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        monitorsCounter = GameObject.Find("monitorsCounter").transform.Find("Text").GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        //人数表示の更新
        playersCountText.text = PhotonNetwork.PlayerList.Length.ToString();

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
        //人数制限を設ける
        if(PhotonNetwork.PlayerList.Length > limitPlayers)
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

            //観戦からの切り替えなら観戦者を削除しておく
            if(monitor != null)
            {
                PhotonNetwork.Destroy(monitor);
                monitor = null;
                ms = null;
            }
        }
        
        base.OnJoinedRoom();
    }

    ~selectScene()
    {
    }
}
