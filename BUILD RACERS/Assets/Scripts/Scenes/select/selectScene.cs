using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

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

        // PhotonServerSettingsの設定内容を使ってマスターサーバーへ接続する
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void FixUpdate()
    {
        
    }

    public void PushStartButton()
    {
        //名前が一文字以上でシーン遷移
        if(PlayerPrefs.GetString("PlayerName").Length > 0)
        {
            SceneManager.LoadScene("gamePlay");
        }
    }

    public void InputText()
    {
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

        //プレイヤーの名前を保存
        PlayerPrefs.SetString("PlayerName", input.text);
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster()
    {
        // "Room"という名前のルームに参加する（ルームが存在しなければ作成して参加する）
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions(), TypedLobby.Default);
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        //選択アイコンの生成　最初は画面外に生成
        selector = PhotonNetwork.Instantiate("Selector", new Vector3(-100,-100,-100), Quaternion.identity);

        Debug.Log("接続成功");
    }

    ~selectScene()
    {
        //セレクターの削除
        Destroy(selector);
    }
}
