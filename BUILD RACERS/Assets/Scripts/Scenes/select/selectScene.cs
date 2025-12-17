using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Fusion.Sockets.NetBitBuffer;

public class selectScene : baseScene
{
    [SerializeField] private GameObject readyButtonText;

    //セレクター関係
    private GameObject selector;
    private selectSystem ss;

    //UIテキスト
    private TextMeshProUGUI debugMessage;
    private TextMeshProUGUI playersCountText;

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
    }

    private void Update()
    {
        //デバッグメッセージの更新処理
        if (ss == null)
        {
            Debug.Log("セレクターが見つかりません");
            return;
        }
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

        //人数表示の更新
        playersCountText.text = PhotonNetwork.PlayerList.Length.ToString();
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
            SceneManager.LoadScene("gamePlay");

            //セレクターの削除
            //if(selector != null && selector.GetPhotonView().IsMine) PhotonNetwork.Destroy(selector);
        }
    }

    public void PushReadyButton()
    {
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
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

        //ネームバーの文字数制限
        if(input.text.Length > 8) input.text = input.text.Substring(0, 8);

        //ゲーミングカラー
        if(input.text == "rainbow")
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

        ss.UpdateNameBar();
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        //選択アイコンの生成　最初は画面外に生成
        selector = PhotonNetwork.Instantiate("Selector", new Vector3(-100,-100,-100), Quaternion.identity);
        ss = selector.GetComponent<selectSystem>();
        ss.DecideColor();

        Debug.Log("接続成功");
    }

    ~selectScene()
    {
    }
}
