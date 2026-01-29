using ExitGames.Client.Photon.StructWrapping;
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
    [SerializeField] private GameObject PassInputer;

    Dictionary<GameObject,bool> upFlags = new Dictionary<GameObject,bool>();

    private Dictionary<string, GameObject> roomButtons = new Dictionary<string, GameObject>();

    private GameObject noRoomsText;

    private string createRoomName;
    private string roomStat;

    private int maxPlayers;

    [SerializeField]private float duration = 0.2f;
    private bool isMoving = false;

    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    private const int MAX_CCU = 20;

    private string selectRoomName = "";

    void Start()
    {
        preSceneName = "menu";

        if (fade != null)
        {
            fade.SetStartRange();
            fade.FadeOut(fadeOutDuration);
        }

        maxPlayers = 0;

        upFlags[CreateUI] = true;
        upFlags[PassInputer] = true;

        //パスワード情報をリセット
        PlayerPrefs.SetString("roomPassCode", "");
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
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("select");
        });
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
        //人数制限の確認
        int totalInRooms = CalculateTotalPlayers(roomList);
        if(totalInRooms >= MAX_CCU)
        {
            Debug.Log($"サーバーが満員です {totalInRooms} / {MAX_CCU}");
            Debug.Log("メニューに戻ります");
            SceneManager.LoadScene("menu");
        }

        Dictionary<string, bool> geneFlag = new Dictionary<string, bool>();

        foreach (var room in roomList)
        {
            if (room.RemovedFromList) continue;

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

            int maxPlayres = 00;
            if (room.CustomProperties.TryGetValue("limitPlayers", out var v) && v is int mp)
            {
                maxPlayres = mp;
                //人数表示の更新
                scr.SetCounterText($"{room.PlayerCount}/{maxPlayres}");
            }
            else
            {
                Debug.Log("limitPlayresが取得できません");
                string temp = scr.GetCounterText();
                int cutidx = 0;
                foreach(char c in temp)
                {
                    if(c == '/') break;
                    cutidx++;
                }
                //人数表示の更新
                scr.SetCounterText($"{room.PlayerCount}/{temp.Substring(temp.Length - cutidx)}");
            }

            Debug.Log(
                $"Room: {room.Name} " +
                $"Players: {room.PlayerCount}/{maxPlayres}"
            );

            //ルーム状態の更新
            if (room.CustomProperties.TryGetValue("masterGameScene", out var s) && s is string stat)
            {
                Debug.Log($"Update Room Stat : {stat}");
                if (stat == "Waiting") stat = "待機中";
                else if (stat == "Starting") stat = "開始中";
                else if (stat == "Finished") stat = "終了済み";
                else stat = "レース中";
                roomStat = stat;
            }
            Debug.Log($"Update Room Stat : {roomStat}");
            scr.SetRoomStatText(roomStat);

            //マップを取得
            if(room.CustomProperties.TryGetValue("playSceneName" , out var str) && str is string)
            {
                scr.roomPlaySceneName = (string)str;
            }

            //パスワード取得
            if (room.CustomProperties.TryGetValue("roomPassCode", out var pass) && pass is string)
            {
                scr.SetRoomPassCode((string)pass);
            }

            //生成フラグ
            geneFlag[room.Name] = true;

            //ボタンの削除
            if(roomStat == "終了済み") geneFlag[room.Name] = false;
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
    private int CalculateTotalPlayers(List<RoomInfo> roomList)
    {
        int total = 0;

        foreach (RoomInfo room in roomList)
        {
            if (!room.RemovedFromList)
            {
                total += room.PlayerCount;
                Debug.Log($"  Room [{room.Name}]: {room.PlayerCount}/{room.MaxPlayers}人");
            }
        }
        Debug.Log($"接続人数 : {total} / {MAX_CCU}");

        return total;
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

    public void SetPass()
    {
        GameObject inputField = GameObject.Find("PassInputField");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        //パスワード設定
        string pass = input.text;
        PlayerPrefs.SetString("roomPassCode", pass);
    }

    public void InputPass()
    {
        GameObject inputField = GameObject.Find("PassInputer");
        TMP_InputField input = inputField.GetComponent<TMP_InputField>();

        //パスワード照合
        var roomButtonList = GameObject.FindObjectsOfType<roomNameButton>();
        foreach (var roomButton in roomButtonList)
        {
            if (roomButton.GetRoomName() == selectRoomName)
            {
                string correctPass = roomButton.GetRoomPassCode();
                if (input.text == correctPass)
                {
                    Debug.Log("パスワード一致");
                    PlayerPrefs.SetString("roomPassCode", input.text);
                    roomButton.PushRoomNameButton();
                }
                else
                {
                    Debug.Log("パスワード不一致");
                }
                break;
            }
        }
    }

    public void SetSelectRoomName(string name)
    {
        selectRoomName = name;
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
        MoveY(CreateUI);

        //パスワードUI出てれば閉じる
        if (!upFlags[PassInputer]) MoveY(PassInputer);

        //ボタンのテキスト変更
        GameObject inputField = GameObject.Find("CreateNewText");
        TMP_Text text = inputField.GetComponent<TMP_Text>();
        if (upFlags[CreateUI]) text.text = "部屋を作る";
        else text.text = "作るのをやめる";
    }

    public void PushCreateCancelButton()
    {
        if (isMoving || upFlags[CreateUI]) return;
        MoveY(CreateUI);

        //ボタンのテキスト変更
        GameObject inputField = GameObject.Find("CreateNewText");
        TMP_Text text = inputField.GetComponent<TMP_Text>();
        text.text = "部屋を作る";
    }

    public void PushPassInputCancelButton()
    {
        if (isMoving || upFlags[PassInputer]) return;
        MoveY(PassInputer);
    }

    //部屋に入る用のパスワード入力UIを出す
    public void ShowPassInputer()
    {
        //移動中は処理なし
        if (isMoving) return;

        MoveY(PassInputer);
    }

    public void MoveY(GameObject obj)
    {
        //パスワードをリセット
        PlayerPrefs.SetString("roomPassCode", "");

        StartCoroutine(Move(obj));
        upFlags[obj] = !upFlags[obj];
    }

    IEnumerator Move(GameObject obj)
    {
        isMoving = true;

        var rectTransform = obj.GetComponent<RectTransform>();

        if (rectTransform == null) yield break;

        Vector2 start = rectTransform.anchoredPosition;
        Vector2 end = start + new Vector2(0, upFlags[obj] ? 1000 : -1000);
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
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("select");
        });

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

        //ルームのプレイ人数上限設定　観戦はプレイ人数含めて合計20人まで
        PlayerPrefs.SetInt("roomLimitPlayers", maxPlayers);

        //ルームを新規作成　接続
        PhotonNetwork.JoinOrCreateRoom(createRoomName, options, TypedLobby.Default);
    }
}
