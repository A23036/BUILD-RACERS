using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class robbyScene : baseScene
{
    private Dictionary<string, GameObject> roomButtons = new Dictionary<string, GameObject>();

    private GameObject noRoomsText;

    private string createRoomName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "multi";
    }

    private void Awake()
    {
        noRoomsText = GameObject.Find("noRoomsText");

        //既に接続済みなら処理なし
        if (PhotonNetwork.IsConnected) return;

        //マスターサーバーへの接続
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }

    public void PushSelectButton()
    {
        SceneManager.LoadScene("select");
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster()
    {
        Debug.Log("マスターサーバーへ接続成功");

        //ロビーへ接続
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Dictionary<string, bool> geneFlag = new Dictionary<string, bool>();

        foreach (var room in roomList)
        {
            if (room.RemovedFromList) continue;

            int maxPlayres = 0;
            if(room.CustomProperties.TryGetValue("limitPlayers", out var v) && v is int mp)
            {
                maxPlayres = mp;
            }

            Debug.Log(
                $"Room: {room.Name} " +
                $"Players: {room.PlayerCount}/{maxPlayres}"
            );

            //ボタンのスクリプト
            roomNameButton scr = null;

            //新たな部屋があれば生成
            if (!roomButtons.TryGetValue(room.Name,out var obj) || obj == null)
            {
                GameObject prefab = (GameObject)Resources.Load("roomNameButton");
                // プレハブからインスタンスを生成
                var button = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                button.transform.position += new Vector3(-100, 0, 0);
                roomButtons[room.Name] = button;
                scr = button.GetComponent<roomNameButton>();
                scr.SetText(room.Name);

                //プレハブなのでクリック時の関数を登録
                Button btn = button.GetComponent<Button>();
                btn.onClick.AddListener(scr.PushRoomNameButton);
            }
            else
            {
                scr = roomButtons[room.Name].GetComponent<roomNameButton>();
            }

            //人数表示の更新
            scr.SetCounterText($"{ room.PlayerCount}/{ maxPlayres}");

            geneFlag[room.Name] = true;
        }

        //古い部屋があれば削除する
        foreach (var room in roomList)
        {
            //部屋があれば処理なし
            if (geneFlag.TryGetValue(room.Name,out bool b) && b) continue;

            //部屋ボタンの削除
            Destroy(roomButtons[room.Name]);
            roomButtons.Remove(room.Name);
        }

        //部屋が１つもなければその旨を表示
        if (roomButtons.Count == 0) noRoomsText.SetActive(true);
        else noRoomsText.SetActive(false);
    }

    public void InputText()
    {
        //共通処理
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

        //ネームバーの文字数制限
        int nameLimitNum = 10;
        if (input.text.Length > nameLimitNum) input.text = input.text.Substring(0, nameLimitNum);

        createRoomName = input.text;
    }

    //ルームを新規作成
    public void PushCreateRoomButton()
    {
        //０文字なら処理なし
        if (createRoomName.Length <= 0) return;

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

        //ルームを新規作成　接続
        PhotonNetwork.JoinOrCreateRoom(createRoomName, options, TypedLobby.Default);
    }
}
