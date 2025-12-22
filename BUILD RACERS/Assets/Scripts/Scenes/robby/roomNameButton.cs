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

    public void SetText(string text)
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

    //ルームへ接続
    public void PushRoomNameButton()
    {
        //シーン遷移
        SceneManager.LoadScene("select");

        //ルームのオプション設定
        RoomOptions options = new RoomOptions
        {
            //離脱したプレイヤーが生成したオブジェクトが自動削除される設定
            CleanupCacheOnLeave = true,

            //部屋のカスタムプロパティをロビーから確認できる設定
            CustomRoomPropertiesForLobby = new string[]
            {
                "limitPlayers"
            }
        };

        TextMeshProUGUI Text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        PhotonNetwork.JoinOrCreateRoom(Text.text, options, TypedLobby.Default);
    }
}
