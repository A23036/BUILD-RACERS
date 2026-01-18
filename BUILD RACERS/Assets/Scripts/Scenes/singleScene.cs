using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class singleScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //ラップ数設定
    [SerializeField] GameObject lapSetter;

    //セレクター関係
    private selectSystem ss;

    void Start()
    {
        preSceneName = "menu";

        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();
        input.text = PlayerPrefs.GetString("PlayerName");

        //セレクターの生成
        var selector = Instantiate(Resources.Load("Selector"), new Vector3(-100, -100, -100), Quaternion.identity);
        var rect = selector.GetComponent<RectTransform>();
        rect.localScale *= 1.7f;
        ss = selector.GetComponent<selectSystem>();

        //観戦者フラグ初期化
        PlayerPrefs.SetInt("isMonitor", 0);
    }

    // Update is called once per frame
    void Update()
    {
        //デバッグメッセージの更新処理
        if (ss == null)
        {
            Debug.Log("セレクターが見つかりません");
            return;
        }
        base.Update();
    }

    public void PushStartButton()
    {
        //名前が未入力なら処理なし
        string name = PlayerPrefs.GetString("PlayerName");
        if (name == null || name == "") return;

        //選択なしで処理なし
        if (PlayerPrefs.GetInt("driverNum") == -1 && PlayerPrefs.GetInt("engineerNum") == -1) return;

        //ラップ数を記録
        var ls = lapSetter.GetComponent<LapSetter>();
        PlayerPrefs.SetInt("lapCnt",ls.GetLapCnt());

        //シングルプレイへ
        SceneManager.LoadScene("singlePlay");
    }

    public void InputText()
    {
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

        //ネームバーの文字数制限
        if (input.text.Length > 8) input.text = input.text.Substring(0, 8);

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

        ss.UpdateNameBar();
    }
}
