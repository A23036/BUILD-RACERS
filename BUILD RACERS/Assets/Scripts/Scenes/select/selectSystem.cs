using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class selectSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    //同期対象の変数
    private int selectDriverNum;
    private int selectEngineerNum;
    private int colorNumber;
    private bool isReady;
    private bool isRoomMaster;

    //カラーパレット
    Color[] colorPalette;
    private int playersCount = 16;

    // セレクターが重ならないように
    private string key;
    private string oldkey;
    private string pendingkey;

    private float timer;

    [SerializeField] private Vector3 offset;
    [SerializeField] private bool gamingColor;

    private IconManager im;
    private List<Transform> driverIcons;
    private List<Transform> engineerIcons;

    private TextMeshProUGUI debugMessage;
    private TextMeshProUGUI playersCountText;

    //画像オブジェクト
    private GameObject checkmark;
    private GameObject crown;

    void Start()
    {
        selectDriverNum = -1;
        selectEngineerNum = -1;

        timer = 0f;

        //セレクトの初期化
        PlayerPrefs.SetInt("driverNum", 1);
        PlayerPrefs.SetInt("engineerNum", -1);
    }

    private void Awake()
    {
        debugMessage = GameObject.Find("DebugMessage").GetComponent<TextMeshProUGUI>();
        debugMessage.color = Color.black;
        playersCountText = GameObject.Find("PlayerCountText").GetComponent<TextMeshProUGUI>();
        playersCountText.color = Color.black;
        im = GameObject.Find("IconManager").GetComponent<IconManager>();
        driverIcons = im.GetDriverIconsList();
        engineerIcons = im.GetEngineerIconsList();

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

        //子のチェックマークの取得
        checkmark = transform.Find("i_checkmark").gameObject;

        //チェックマーク　色の変更
        var image = checkmark.GetComponent<Image>();
        if (image != null)
        {
            image.color = Color.green;
        }

        //子の王冠マークの取得
        crown = transform.Find("crown").gameObject;

        //ルームマスターならアクティブにする
        isRoomMaster = false;

        if (photonView.IsMine && PhotonNetwork.IsMasterClient)
        {
            isRoomMaster = true;
            crown.SetActive(true);
        }
    }

    void Update()
    {
        //色の更新
        UpdateColor();

        //チェックマークのアクティブを更新
        UpdateCheckmark();

        if (!photonView.IsMine) return;

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
        if (true)
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
            debugMessage.text = "NOW SELECT : NONE";
        }
        else
        {
            if (selectDriverNum != -1)
            {
                transform.position = driverIcons[selectDriverNum].position + offset;
                debugMessage.text = "NOW SELECT : DRIVER" + (selectDriverNum + 1);
                PlayerPrefs.SetInt("driverNum", selectDriverNum + 1);
                PlayerPrefs.SetInt("engineerNum", -1);
            }
            else
            {
                transform.position = engineerIcons[selectEngineerNum].position + offset;
                debugMessage.text = "NOW SELECT : ENGINEER" + (selectEngineerNum + 1);
                PlayerPrefs.SetInt("driverNum", -1);
                PlayerPrefs.SetInt("engineerNum", selectEngineerNum + 1);
            }
        }

        //人数表示の更新
        playersCountText.text = PhotonNetwork.PlayerList.Length.ToString();
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
        if (isReady)
        {
            Debug.Log("準備完了しています");
            return;
        }

        //送信済みならコールバックまで送信しない
        if (pendingkey != null)
        {
            Debug.Log("予約送信済み");
            return;
        }

        // 予約をリクエスト　ローカルの確定・更新はコールバックで行う
        pendingkey = (driver != -1) ? $"D_{driver + 1}" : $"B_{engineer + 1}";

        // キーの予約リクエストの送信
        if (!TryReserveSlot(pendingkey))
        {
            //予約失敗なら希望値をリセット
            pendingkey = null;
            Debug.Log("予約失敗");
        }
        else
        {
            Debug.Log("予約成功");
        }
    }

    //カスタムプロパティのコールバック
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        Debug.Log("[Custom CallBack]");

        //シーン遷移
        if (changed.ContainsKey("isEveryoneReady") && changed["isEveryoneReady"] is bool isEveryoneReady && isEveryoneReady)
        {
            var sm = GameObject.Find("SceneManager").GetComponent<selectScene>();
            sm.PushStartButton();
        }

        //関係のないコールバックは無視
        if (pendingkey == null || !changed.ContainsKey(pendingkey))
        {
            return;
        }

        //カスタムプロパティをローカルに反映
        bool isHit = false;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        foreach (var kv in props)
        {
            if (kv.Value is int n && n == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                //ローカル更新
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

                isHit = true;
            }
        }

        //キーがなければ画面外に移動
        if (!isHit)
        {
            selectDriverNum = -1;
            selectEngineerNum = -1;

            oldkey = null;
            key = null;
        }

        //希望値をリセット
        pendingkey = null;

        return;
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

        GetComponent<Image>().color = colorPalette[colorNumber % playersCount];
    }

    public void UpdateCheckmark()
    {
        if (isReady != checkmark.activeSelf) checkmark.SetActive(isReady);
    }

    public bool IsReady()
    {
        return isReady;
    }

    //READY押したらフラグ反転
    public void PushedReady()
    {
        isReady = !isReady;
        checkmark.SetActive(isReady);

        //ルームマスターに送信
        //if(!PhotonNetwork.IsMasterClient) SendToMaster(isReady);
        SendToMaster(isReady);
    }

    //ゲーミングカラーのアクティブ変更
    public void GamingMode(bool b)
    {
        gamingColor = b;
    }

    //ルームマスターに準備状態を送信
    void SendToMaster(bool readyStat)
    {
        int viewID = (int)PhotonNetwork.CurrentRoom.CustomProperties["MasterClienViewID"];
        PhotonView target = PhotonView.Find(viewID);

        target.RPC("RPC_OnSelectorChanged", RpcTarget.MasterClient, readyStat, PhotonNetwork.LocalPlayer.ActorNumber);
        Debug.Log("SendToMaster");
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
        if (PhotonNetwork.IsMasterClient) ReleaseSlotAll(otherNumber);
    }
}