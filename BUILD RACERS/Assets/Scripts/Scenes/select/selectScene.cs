using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using ExitGames.Client.Photon;

public class selectScene : baseScene
{
    private GameObject selector;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "menu";

        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();
        input.text = PlayerPrefs.GetString("PlayerName");
    }

    // Update is called once per frame
    void FixUpdate()
    {
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
            PhotonNetwork.Destroy(selector);
        }
    }

    public void InputText()
    {
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

        //プレイヤーの名前を保存
        PlayerPrefs.SetString("PlayerName", input.text);
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        //選択アイコンの生成　最初は画面外に生成
        selector = PhotonNetwork.Instantiate("Selector", new Vector3(-100,-100,-100), Quaternion.identity);
        var ss = selector.GetComponent<selectSystem>();
        ss.DecideColor();

        Debug.Log("接続成功");
    }

    ~selectScene()
    {
    }
}
