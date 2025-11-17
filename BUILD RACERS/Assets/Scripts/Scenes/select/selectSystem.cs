using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;

public class selectSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    //同期対象の変数
    private int selectDriverNum;
    private int selectBuilderNum;
    private int colorNumber;

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
    private List<Transform> builderIcons;

    private TextMeshProUGUI text;

    void Start()
    {
        selectDriverNum = -1;
        selectBuilderNum = -1;

        timer = 0f;

        //セレクトの初期化
        PlayerPrefs.SetInt("driver", 1);
        PlayerPrefs.SetInt("builder", -1);
    }

    private void Awake()
    {
        text = GameObject.Find("DebugMessage").GetComponent<TextMeshProUGUI>();
        im = GameObject.Find("IconManager").GetComponent<IconManager>();
        driverIcons = im.GetDriverIconsList();
        builderIcons = im.GetBuilderIconsList();

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
    }

    void Update()
    {
        //色の更新
        UpdateColor();

        // 表示（自分のオブジェクトにだけ描画を任せる場合）
        if (!photonView.IsMine) return;

        //ゲーミングカラー
        if (gamingColor)
        {
            timer += Time.deltaTime;
            if (timer >= .1f)
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

        if (selectDriverNum == -1 && selectBuilderNum == -1)
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
                PlayerPrefs.SetInt("driver", selectDriverNum + 1);
                PlayerPrefs.SetInt("builder", -1);
            }
            else
            {
                transform.position = builderIcons[selectBuilderNum].position + offset;
                text.text = "NOW SELECT : BUILDER" + (selectBuilderNum + 1);
                PlayerPrefs.SetInt("driver", -1);
                PlayerPrefs.SetInt("builder", selectBuilderNum + 1);
            }
        }
    }
    public bool TryReserveSlot(string key)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        var propsToSet = new Hashtable { { key, actor } };
        var expected = new Hashtable { { key, null } }; // キーが無ければ予約できる（原子的）
        bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
        if (success) PlayerPrefs.SetString("reservedSlot", key);
        return success;
    }

    public bool ReleaseSlot(string key)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        // 自分が占有しているか確認してから解除するのが安全
        object cur;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out cur))
        {
            if (cur is int owner && owner == actor)
            {
                /*
                var propsToSet = new Hashtable { { key, null } };
                var expected = new Hashtable { { key, actor } }; // 自分が所有していれば解除
                bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
                Debug.Log("DELETE KEY");
                return success;
                */
            }
        }

        var propsToSet = new Hashtable { { key, null } };
        var expected = new Hashtable { { key, actor } }; // 自分が所有していれば解除
        bool success = PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet, expected);
        Debug.Log("DELETE KEY");
        return success;

        return false;
    }

    public void SetNum(int driver, int builder)
    {
        // 予約をリクエスト　ローカルの確定・更新はコールバックで行う
        pendingkey = (driver != -1) ? $"D_{driver + 1}" : $"B_{builder + 1}";
        if (!TryReserveSlot(pendingkey))
        {
            text.text = "NOW SELECT : CAPA OVER";
            return;
        }
    }

    // 
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        base.OnRoomPropertiesUpdate(changed);

        if (pendingkey == null) return;
        // 自分が予約したキーが変更された時だけ処理
        if (!changed.ContainsKey(pendingkey)) return;

        //希望値がとれていればローカルを更新
        object value = changed[pendingkey];
        if (value is int number && number == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            if (pendingkey.StartsWith("D_"))
            {
                selectDriverNum = int.Parse(pendingkey.Substring(2)) - 1;
                selectBuilderNum = -1;
            }
            else
            {
                selectBuilderNum = int.Parse(pendingkey.Substring(2)) - 1;
                selectDriverNum = -1;
            }

            //キーのリリース
            if(oldkey != null)
            {
                ReleaseSlot(oldkey);
            }
            oldkey = pendingkey;
        }

        //希望値をリセット
        pendingkey = null;
    }

    public void GetNums(out int dn, out int bn)
    {
        dn = selectDriverNum;
        bn = selectBuilderNum;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // このクライアントが所有者なら送る
            stream.SendNext(selectDriverNum);
            stream.SendNext(selectBuilderNum);
            stream.SendNext(colorNumber);
        }
        else
        {
            // 他クライアントから受け取る
            selectDriverNum = (int)stream.ReceiveNext();
            selectBuilderNum = (int)stream.ReceiveNext();
            colorNumber = (int)stream.ReceiveNext();
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

    public void PrintLog()
    {
        int actorNumber = photonView.Owner.ActorNumber;
        Debug.Log("No." + actorNumber + " COLOR : " + colorNumber);
    }

    void OnDestroy()
    {
        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }
}