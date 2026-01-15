using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class selectSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    //同期対象の変数
    private int selectDriverNum;
    private int selectEngineerNum;
    private int colorNumber;
    private bool isReady;
    private bool isRoomMaster;
    private string playerName;

    //カラーパレット
    Color[] colorPalette;
    private int playersCount = 16;

    // セレクターが重ならないように
    private string key;
    private string oldkey;
    private string pendingkey;

    [SerializeField]private float selectCooltime = 0.2f;
    private float selectTimer = 0f;

    private float timer;

    [SerializeField] private Vector3 offset;
    [SerializeField] private bool gamingColor;

    private IconManager im;
    private List<Transform> driverIcons;
    private List<Transform> engineerIcons;

    private TextMeshProUGUI nameBar;

    //画像オブジェクト
    private GameObject checkmark;
    private GameObject crown;

    private SelectorManager selectorManager;

    void Start()
    {
        selectDriverNum = -1;
        selectEngineerNum = -1;

        timer = 0f;

        //セレクトの初期化
        if(PhotonNetwork.IsConnected && photonView.IsMine)
        {
            PlayerPrefs.SetInt("driverNum", 1);
            PlayerPrefs.SetInt("engineerNum", -1);
            PlayerPrefs.SetInt("isMonitor", -1);
        }

        //キャンバスの子供に設定
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        transform.SetParent(canvas.transform, false);

        //セレクターの色をプレイヤーの数で分割
        Color[] cols = new Color[playersCount];
        for (int i = 0; i < playersCount; i++)
        {
            float h = i / (float)playersCount; // 0..1
            cols[i] = Color.HSVToRGB(h, 1, 1);
        }
        colorPalette = cols;

        //チェックマーク　色の変更
        var image = checkmark.GetComponent<Image>();
        if (image != null)
        {
            image.color = Color.green;
        }

        //シングルプレイなら以下処理は省略
        if (!PhotonNetwork.IsConnected) return;

        //ルームマスターならアクティブにする
        isRoomMaster = false;

        if (photonView.IsMine && PhotonNetwork.IsMasterClient)
        {
            isRoomMaster = true;
            crown.SetActive(true);
        }

        selectorManager = this.gameObject.GetComponent<SelectorManager>();
    }

    private void Awake()
    {
        // セレクトシーン以外では即終了
        if (PhotonNetwork.IsConnected && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "select")
        {
            gameObject.SetActive(false);
            return;
        }

        //ボタンの配列を取得
        im = GameObject.Find("IconManager").GetComponent<IconManager>();
        driverIcons = im.GetDriverIconsList();
        engineerIcons = im.GetEngineerIconsList();

        //子のチェックマークの取得
        checkmark = transform.Find("i_checkmark").gameObject;

        //子の王冠マークの取得
        crown = transform.Find("crown").gameObject;

        //ネームバーの取得
        nameBar = transform.Find("NameBar").gameObject.GetComponent<TextMeshProUGUI>();
        playerName = PlayerPrefs.GetString("PlayerName");
    }

    void Update()
    {
        //色の更新
        UpdateColor();

        //チェックマークのアクティブを更新
        UpdateCheckmark();

        //ネームバーの更新
        UpdateNameBar();

        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;

        //選択クールタイムの更新
        selectTimer -= Time.deltaTime;

        //ゲーミングカラー
        if (gamingColor)
        {
            timer += Time.deltaTime;
            if (timer >= .2f)
            {
                colorNumber++;
                timer = 0f;
            }
        }

        //Debug numbers
        if (PhotonNetwork.IsConnected)
        {
            timer += Time.deltaTime;
            if (timer >= 1f)
            {
                //カスタムプロパティ確認
                var props = PhotonNetwork.CurrentRoom.CustomProperties;

                Debug.Log($"[RoomPropCount] {props.Count}");
                foreach (var kv in props)
                {
                    Debug.Log($"[RoomProp] {kv.Key} = {kv.Value}");
                }

                timer = 0f;
            }
        }

        if (selectDriverNum == -1 && selectEngineerNum == -1)
        {
            transform.position = new Vector3(-100, -100, -100);
        }
        else
        {
            if (selectDriverNum != -1)
            {
                transform.position = driverIcons[selectDriverNum].position + offset;
                PlayerPrefs.SetInt("driverNum", selectDriverNum + 1);
                PlayerPrefs.SetInt("engineerNum", -1);
            }
            else
            {
                transform.position = engineerIcons[selectEngineerNum].position + offset;
                PlayerPrefs.SetInt("driverNum", -1);
                PlayerPrefs.SetInt("engineerNum", selectEngineerNum + 1);
            }
        }
    }
    public bool TryReserveSlot(string pendkey)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;

        //獲得と解放を同時に行う　原子性
        var propsToSet = new Hashtable();
        var expected = new Hashtable();

        //自分を選択のときはCASの確認をせずにセット
        if (pendingkey != null && key == pendkey)
        {
            Debug.Log("自分を選択");
            propsToSet[pendkey] = null;
            expected[pendkey] = actor;
        }
        else
        {
            Debug.Log("自分以外を選択");
            propsToSet[pendkey] = actor;
            expected[pendkey] = null;

            //キーを取得していれば解放も行う
            if (oldkey != null)
            {
                propsToSet[oldkey] = null;
                expected[oldkey] = actor;
            }
        }

        bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
        return success;
    }

    //すべてのキーの解放　切断時に呼び出す
    public void ReleaseSlotAll(int actor)
    {
        //接続確認
        if (!PhotonNetwork.IsConnected) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        var propsToSet = new Hashtable();

        foreach (var kv in props)
        {
            if (kv.Value is int n && n == actor)
            {
                propsToSet[kv.Key] = null;
            }
        }

        //プロパティに反映させる
        if (propsToSet.Count > 0)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet);
            Debug.Log("Release : " + propsToSet.Count);
        }
    }

    public void SetNum(int driver, int engineer)
    {
        if (selectTimer > 0f) return;

        selectTimer = selectCooltime;

        // 予約をリクエスト　ローカルの確定・更新はコールバックで行う
        pendingkey = (driver != -1) ? $"D_{driver + 1}" : $"B_{engineer + 1}";

        TryReserveSlot(pendingkey);
    }

    public void SetNumOffline(int driver, int engineer)
    {
        selectDriverNum = driver;
        selectEngineerNum = engineer;
    }

    //カスタムプロパティのコールバック
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        if (!photonView.IsMine) return;

        Debug.Log($"[Custom CallBack] changed keys: {changed.Count}");

        //準備状態の初期化
        if (changed.ContainsKey("MasterClientViewID"))
        {
            SendToMaster(false);
            Debug.Log("準備状態の初期化");
        }

        //シーン遷移
        if (changed.ContainsKey("isEveryoneReady") && changed["isEveryoneReady"] is bool isEveryoneReady && isEveryoneReady)
        {
            PhotonNetwork.IsMessageQueueRunning = false;
            var sm = GameObject.Find("SceneManager").GetComponent<selectScene>();
            sm.PushStartButton();
        }

        if (pendingkey == null) return;

        // 差分の中に自分のpendingkeyがあるかチェック
        if (changed.ContainsKey(pendingkey))
        {
            var value = changed[pendingkey];

            // 自分が取得成功
            if (value is int actorNum && actorNum == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // ローカル更新
                if (pendingkey.StartsWith("D_"))
                {
                    selectDriverNum = int.Parse(pendingkey.Substring(2)) - 1;
                    selectEngineerNum = -1;
                }
                else
                {
                    selectEngineerNum = int.Parse(pendingkey.Substring(2)) - 1;
                    selectDriverNum = -1;
                }

                oldkey = pendingkey;
                key = pendingkey;

                Debug.Log($"Slot acquired: {pendingkey}");
            }
            // 他の人が取得した、またはnullになった
            else
            {
                Debug.Log($"Slot failed: {pendingkey} (value={value})");
            }

            pendingkey = null;
        }

        // 自分の古いキーが解放されたか確認
        if (oldkey != null && changed.ContainsKey(oldkey))
        {
            if (changed[oldkey] == null || !(changed[oldkey] is int n && n == PhotonNetwork.LocalPlayer.ActorNumber))
            {
                // 自分のスロットが解放された
                selectDriverNum = -1;
                selectEngineerNum = -1;
                oldkey = null;
                key = null;

                Debug.Log("Slot released");
            }
        }
    }

    public void GetNums(out int dn, out int bn)
    {
        dn = selectDriverNum;
        bn = selectEngineerNum;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // このクライアントが所有者なら送る
            stream.SendNext(selectDriverNum);
            stream.SendNext(selectEngineerNum);
            stream.SendNext(colorNumber);
            stream.SendNext(isReady);
            stream.SendNext(isRoomMaster);
            stream.SendNext(playerName);
        }
        else
        {
            // 他クライアントから受け取る
            selectDriverNum = (int)stream.ReceiveNext();
            selectEngineerNum = (int)stream.ReceiveNext();
            colorNumber = (int)stream.ReceiveNext();
            isReady = (bool)stream.ReceiveNext();
            bool isRM = (bool)stream.ReceiveNext();

            //変化があればSetActive
            if (isRM != isRoomMaster)
            {
                crown.SetActive(isRM);
            }
            isRoomMaster = isRM;

            //変化があればネームバーの更新
            string name = (string)stream.ReceiveNext();
            if(name != playerName)
            {
                UpdateNameBar();
                playerName = name;
            }
        }
    }

    //セレクターの色の割り当て
    public void DecideColor()
    {
        //自分のみ色を指定　他プレイヤーは同期で色を受け取る
        if (!photonView.IsMine) return;

        colorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
    }

    public void UpdateColor()
    {
        //未設定なら処理なし
        if (colorNumber == -1) return;

        //セレクターの色更新
        GetComponent<Image>().color = colorPalette[colorNumber % playersCount];

        //ネームバーの色更新
        nameBar.color = colorPalette[colorNumber % playersCount];
    }

    public void UpdateCheckmark()
    {
        if (isReady != checkmark.activeSelf) checkmark.SetActive(isReady);
    }

    public void UpdateNameBar()
    {
        if(!PhotonNetwork.IsConnected)
        {
            nameBar.text = PlayerPrefs.GetString("PlayerName");
            return;
        }

        if (photonView.IsMine)
        {
            nameBar.text = PlayerPrefs.GetString("PlayerName");
            playerName = PlayerPrefs.GetString("PlayerName");
        }
        else nameBar.text = playerName;
    }

    public void SetReady(bool b)
    {
        isReady = b;
    }
    public bool IsReady()
    {
        return isReady;
    }

    //READY押したらフラグ反転
    public void PushedReady()
    {
        //名前が未入力なら処理なし
        if(PlayerPrefs.GetString("PlayerName") == "")
        {
            return;
        }

        isReady = !isReady;
        checkmark.SetActive(isReady);

        //ルームマスターに送信
        SendToMaster(isReady);

        // ネットワークオブジェクトにdriverNumとengineerNumを登録
        Hashtable hash = new Hashtable();
        hash["driverNum"] = PlayerPrefs.GetInt("driverNum");
        hash["engineerNum"] = PlayerPrefs.GetInt("engineerNum");
        hash["isMonitor"] = PlayerPrefs.GetInt("isMonitor");

        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    //ゲーミングカラーのアクティブ変更
    public void GamingMode(bool b)
    {
        gamingColor = b;
    }

    //ルームマスターに準備状態を送信
    public void SendToMaster(bool readyStat)
    {
        var pv = GetComponent<PhotonView>();

        pv.RPC("RPC_OnSelectorChanged", RpcTarget.All, readyStat, PhotonNetwork.LocalPlayer.ActorNumber);
        Debug.Log("*** SEND TO MASTER ***");

        return;
    }

    //ルームマスターに削除命令を送信
    public void DeleteMyStat()
    {
        return;

        int viewID = (int)PhotonNetwork.CurrentRoom.CustomProperties["MasterClientViewID"];
        PhotonView target = PhotonView.Find(viewID);

        if (target != null)
        {
            target.RPC("RPC_ReleaseSelectorStat", RpcTarget.MasterClient,PhotonNetwork.LocalPlayer.ActorNumber);
            Debug.Log("SendToMaster");
        }
        else
        {
            Debug.Log($"not found : {viewID}'s MasterClientViewID");
        }
    }

    public void PrintLog()
    {
        int actorNumber = photonView.Owner.ActorNumber;
        Debug.Log("No." + actorNumber + " COLOR : " + colorNumber);
    }

    //ルームマスターが変更された際のコールバック
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
    {
        if (photonView.IsMine && PhotonNetwork.IsMasterClient)
        {
            isRoomMaster = true;
            crown.SetActive(true);

            //マスタークライアントのIDを登録
            PhotonView pv = GetComponent<PhotonView>();
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "MasterClientViewID", pv.ViewID } });
        }
    }

    void OnDestroy()
    {
        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }

    //ルームから誰か抜けた時に呼ばれるコールバック
    public override void OnPlayerLeftRoom(Photon.Realtime.Player other)
    {
        //キーの解放　重複の可能性も考えてすべてのプロパティを確認
        int otherNumber = other.ActorNumber;
        if (PhotonNetwork.IsMasterClient)
        {
            ReleaseSlotAll(otherNumber);

            photonView.RPC("RPC_ReleaseSelectorStat", RpcTarget.MasterClient, otherNumber);
        }
    }
}