using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class roomNameButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //コンテントの子供に設定
        GameObject content = GameObject.Find("Content");
        transform.SetParent(content.transform, false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetRoomNameText(string text)
    {
        TextMeshProUGUI Text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        Text.text = text;
    }

    public void SetCounterText(string text)
    {
        TextMeshProUGUI Text = transform.Find("backImage").
            gameObject.transform.Find("counterText").GetComponent<TextMeshProUGUI>();
        Text.text = text;
    }

    public void SetRoomStatText(string text)
    {
        TextMeshProUGUI Text = transform.Find("backImage (1)").
            gameObject.transform.Find("roomStatText").GetComponent<TextMeshProUGUI>();
        Text.text = text;
    }

    //ルームへ接続
    public void PushRoomNameButton()
    {
        //シーン遷移　ルームの状態によって処理分岐
        TextMeshProUGUI statText = transform.Find("backImage (1)").
            gameObject.transform.Find("roomStatText").GetComponent<TextMeshProUGUI>();
        string roomStat = statText.text;
        if (roomStat == "Starting")
        {
            //開始中は参加不可にする
            return;
        }
        else if (roomStat == "Waiting")
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
            SceneManager.LoadScene("gamePlay");
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
                "masterGameScene"
            }
        };

        TextMeshProUGUI Text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        PhotonNetwork.JoinOrCreateRoom(Text.text, options, TypedLobby.Default);
    }
}
