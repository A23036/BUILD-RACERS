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

    private TextMeshProUGUI text;

    private GameObject checkmark;

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
        text = GameObject.Find("DebugMessage").GetComponent<TextMeshProUGUI>();
        text.color = Color.black;
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
        if(image != null)
        {
            image.color = Color.green;  
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
                //座標確認
                //Debug.Log(selectDriverNum + "," + selectBuilderNum);

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
            text.text = "NOW SELECT : NONE";
        }
        else
        {
            if (selectDriverNum != -1)
            {
                transform.position = driverIcons[selectDriverNum].position + offset;
                text.text = "NOW SELECT : DRIVER" + (selectDriverNum + 1);
                PlayerPrefs.SetInt("driverNum", selectDriverNum + 1);
                PlayerPrefs.SetInt("engineerNum", -1);
            }
            else
            {
                transform.position = engineerIcons[selectEngineerNum].position + offset;
                text.text = "NOW SELECT : ENGINEER" + (selectEngineerNum + 1);
                PlayerPrefs.SetInt("driverNum", -1);
                PlayerPrefs.SetInt("engineerNum", selectEngineerNum + 1);
            }
        }
    }
    public bool TryReserveSlot(string pendkey)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        
        //自分を選択のときはCASの確認をせずにセット
        if(pendingkey != null && key == pendkey)
        {
            Debug.Log("自分を選択");
            var propsToSet = new Hashtable { { pendkey, null } };
            bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet);
            return success;
        }
        else
        {
            Debug.Log("自分以外を選択");
            var propsToSet = new Hashtable { { pendkey, actor } };
            var expected = new Hashtable { { pendkey, null } }; // キーが無ければ予約できる（原子的）
            bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
            return success;
        }
    }

    public bool ReleaseSlot(string key)
    {
        if (key == null) return false;
        if (!PhotonNetwork.IsConnected) return false;

        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        // 自分が占有しているか確認してから解除するのが安全
        object cur;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out cur))
        {
            if (cur is int owner && owner == actor)
            {
                ///*
                var propsToSet = new Hashtable { { key, null } };
                var expected = new Hashtable { { key, actor } }; // 自分が所有していれば解除
                bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
                Debug.Log("DELETE KEY");
                return success;
                //*/
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public void SetNum(int driver, int engineer)
    {
        if(isReady)
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
        if(changed.ContainsKey("isEveryoneReady") && changed["isEveryoneReady"] is bool isEveryoneReady && isEveryoneReady)
        {
            var sm = GameObject.Find("SceneManager").GetComponent<selectScene>();
            sm.PushStartButton();
        }

        base.OnRoomPropertiesUpdate(changed);

        // 希望値がない or 希望値が含まれていない　なら処理なし
        if (pendingkey == null)
        {
            Debug.Log("予約なし");
            return;
        }

        if(!changed.ContainsKey(pendingkey))
        {
            Debug.Log("希望値を含まない");
            return;
        }

        //希望値がとれていればローカルを更新
        object value = changed[pendingkey];

        Debug.Log(value);
        
        if (value is int number && number == PhotonNetwork.LocalPlayer.ActorNumber)
        {
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

            //キーのリリース
            if(oldkey != null)
            {
                ReleaseSlot(oldkey);
            }

            oldkey = pendingkey;
            key = pendingkey;

            Debug.Log("獲得成功");
        }
        else if(pendingkey == key)
        {
            if(key != null) ReleaseSlot(key);

            selectDriverNum = -1;
            selectEngineerNum = -1;

            oldkey = null;
            key = null;

            Debug.Log("解除成功");
        }
        else
        {
            Debug.Log("獲得失敗");
        }

        //希望値をリセット
        pendingkey = null;
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
        }
        else
        {
            // 他クライアントから受け取る
            selectDriverNum = (int)stream.ReceiveNext();
            selectEngineerNum = (int)stream.ReceiveNext();
            colorNumber = (int)stream.ReceiveNext();
            isReady = (bool)stream.ReceiveNext();
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
        checkmark.SetActive(isReady);
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
        //テストでID201で固定
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

    void OnDestroy()
    {
        if (key != null) ReleaseSlot(key);

        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }
}