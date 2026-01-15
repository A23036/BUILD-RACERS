using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class robbyScene : baseScene
{
    [SerializeField] private GameObject CreateUI;

    private Dictionary<string, GameObject> roomButtons = new Dictionary<string, GameObject>();

    private GameObject noRoomsText;

    private string createRoomName;
    private string roomStat;

    private int maxPlayers;

    [SerializeField]private float duration = 0.2f;
    private bool moveUp = true;
    private bool isMoving = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "menu";

        maxPlayers = 0;
    }

    private void Awake()
    {
        noRoomsText = GameObject.Find("noRoomsText");

        createRoomName = "";

        //既に接続済みなら処理なし
        if (PhotonNetwork.IsConnected) return;

        //マスターサーバーへの接続
        PhotonNetwork.ConnectUsingSettings();

        base.Awake();
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

            int maxPlayres = -99;
            if(room.CustomProperties.TryGetValue("limitPlayers", out var v) && v is int mp)
            {
                maxPlayres = mp;
            }
            else
            {
                Debug.Log("limitPlayresが取得できません");
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
                scr.SetRoomNameText(room.Name);

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

            //ルーム状態の更新
            if (room.CustomProperties.TryGetValue("masterGameScene", out var s) && s is string stat)
            {
                if (stat == "gamePlay") stat = "In Game";
                else if (stat == "select") stat = "Waiting";
                roomStat = stat;
            }
            Debug.Log($"Update Room Stat : {roomStat}");
            scr.SetRoomStatText(roomStat);

            //生成フラグ
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
        GameObject inputField = GameObject.Find("InputField (TMP)");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        //ネームバーの文字数制限
        int nameLimitNum = 10;
        if (input.text.Length > nameLimitNum) input.text = input.text.Substring(0, nameLimitNum);

        createRoomName = input.text;

        Debug.Log($"Input Room Name : {createRoomName}");
    }

    public void InputTextPlayersNum()
    {
        GameObject inputField = GameObject.Find("InputField (TMP) (1)");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        //数値変換
        if (!int.TryParse(input.text, out int playersNum))
        {
            input.text = "";
            return;
        }

        //2 ~ 16に制限
        if (playersNum < 2) playersNum = 2;
        else if (playersNum > 16) playersNum = 16;

        maxPlayers = playersNum;
        input.text = maxPlayers.ToString();
    }

    public void PushPlusButton()
    {
        maxPlayers++;

        //2 ~ 16に制限
        if (maxPlayers < 2) maxPlayers = 2;
        else if (maxPlayers > 16) maxPlayers = 16;

        GameObject inputField = GameObject.Find("InputField (TMP) (1)");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        input.text = maxPlayers.ToString();
    }

    public void PushMinusButton()
    {
        maxPlayers--;

        //2 ~ 16に制限
        if (maxPlayers < 2) maxPlayers = 2;
        else if (maxPlayers > 16) maxPlayers = 16;

        GameObject inputField = GameObject.Find("InputField (TMP) (1)");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        input.text = maxPlayers.ToString();
    }

    public void PushNewCreate()
    {
        //移動中は処理なし
        if (isMoving) return;
        MoveY();

        //ボタンのテキスト変更
        GameObject inputField = GameObject.Find("CreateNewText");
        TMP_Text text = inputField.GetComponent<TMP_Text>();
        if(moveUp) text.text = "CREATE NEW";
        else text.text = "CANCEL";
    }

    public void MoveY()
    {
        StartCoroutine(Move());
        moveUp = !moveUp;
    }

    IEnumerator Move()
    {
        isMoving = true;

        var rectTransform = CreateUI.GetComponent<RectTransform>();

        Vector2 start = rectTransform.anchoredPosition;
        Vector2 end = start + new Vector2(0, moveUp ? 1000 : -1000);
        //Vector3 start = CreateUI.transform.position;
        //Vector3 end = start + new Vector3(0, moveUp ? 100 : -100, 0);
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            float eased = 1 - Mathf.Pow(1 - t, 5);
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, eased);
            yield return null;
        }

        isMoving = false;
    }

    //ルームを新規作成
    public void PushCreateRoomButton()
    {
        //０文字なら処理なし
        if (createRoomName.Length <= 0) return;

        //プレイ人数が異常値なら処理なし
        if(maxPlayers < 2 || 16 < maxPlayers) return;

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
            "limitPlayers",
            "masterGameScene"
            }
        };

        //ルームのプレイ人数上限設定　観戦はプレイ人数含めて合計20人まで
        PlayerPrefs.SetInt("roomLimitPlayers", maxPlayers);

        //ルームを新規作成　接続
        PhotonNetwork.JoinOrCreateRoom(createRoomName, options, TypedLobby.Default);
    }
}
