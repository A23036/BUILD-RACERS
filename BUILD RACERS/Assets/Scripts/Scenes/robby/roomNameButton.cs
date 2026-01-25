using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class roomNameButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public string roomPlaySceneName;

    private string roomPassCode = "";

    private robbyScene sceneManager;

    private string roomName;

    void Start()
    {
        //コンテントの子供に設定
        GameObject content = GameObject.Find("Content");
        transform.SetParent(content.transform, false);

        sceneManager = GameObject.Find("SceneManager").GetComponent<robbyScene>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetRoomNameText(string text)
    {
        TextMeshProUGUI Text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        Text.text = text;

        roomName = text;
    }

    public void SetCounterText(string text)
    {
        TextMeshProUGUI Text = transform.Find("backImage").
            gameObject.transform.Find("counterText").GetComponent<TextMeshProUGUI>();
        Text.text = text;
    }

    public string GetCounterText()
    {
        TextMeshProUGUI Text = transform.Find("backImage").
            gameObject.transform.Find("counterText").GetComponent<TextMeshProUGUI>();
        return Text.text;
    }

    public void SetRoomStatText(string text)
    {
        TextMeshProUGUI Text = transform.Find("backImage (1)").
            gameObject.transform.Find("roomStatText").GetComponent<TextMeshProUGUI>();
        Text.text = text;
    }

    public void SetRoomPassCode(string pass)
    {
        //マスターのパスワードを受け取る
        roomPassCode = pass;

        //パスワードなければロック画像非表示
        if(roomPassCode == "")
        {
            var lockImage = transform.Find("lockImage").gameObject;
            lockImage.SetActive(false);
        }
    }

    public string GetRoomPassCode()
    {
        return roomPassCode;
    }

    public string GetRoomName()
    {
        return roomName;
    }

    //ルームへ接続
    public void PushRoomNameButton()
    {
        //シーン遷移　ルームの状態によって処理分岐
        TextMeshProUGUI statText = transform.Find("backImage (1)").
            gameObject.transform.Find("roomStatText").GetComponent<TextMeshProUGUI>();
        string roomStat = statText.text;
        if (roomStat == "開始中")
        {
            //開始中 or パスワード不一致で参加不可
            return;
        }
        else if(roomPassCode != PlayerPrefs.GetString("roomPassCode"))
        {
            //パスワード入力UIを表示
            sceneManager.ShowPassInputer();

            //入力前にリセット
            PlayerPrefs.SetString("roomPassCode", "");

            //選択ルーム名を登録
            sceneManager.SetSelectRoomName(roomName);

            return;
        }
        else if (roomStat == "待機中")
        {
            //開始前ならセレクトシーンへ
            SceneManager.LoadScene("select");
        }
        else
        {
            //途中参加は観戦扱い　ゲームプレイシーンへ
            PlayerPrefs.SetInt("driverNum", -1);
            PlayerPrefs.SetInt("engineerNum", -1);
            PlayerPrefs.SetInt("isMonitor", 1);

            Debug.Log($"LOAD SCENE : {roomPlaySceneName}");
            SceneManager.LoadScene(roomPlaySceneName);
        }

        //ルームのオプション設定
        RoomOptions options = new RoomOptions
        {
            //離脱したプレイヤーが生成したオブジェクトが自動削除される設定
            CleanupCacheOnLeave = true,

            //部屋のカスタムプロパティをロビーから確認できる設定
            CustomRoomPropertiesForLobby = new string[]
            {
                "limitPlayers",
                "masterGameScene",
                "playSceneName",
                "roomPassCode"
            }
        };

        TextMeshProUGUI Text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        PhotonNetwork.JoinOrCreateRoom(Text.text, options, TypedLobby.Default);
    }
}
