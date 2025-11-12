using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class selectSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    //同期対象の変数
    private int selectDriverNum;
    private int selectBuilderNum;
    private int colorNumber;

    //カラーパレット
    Color[] colorPalette;
    private int playersCount = 16;

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
        colorNumber = -1;

        timer = 0f;
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
        if(gamingColor)
        {
            timer += Time.deltaTime;
            if (timer >= .5f)
            {
                colorNumber++;
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

    public void SetNum(int driver, int builder)
    {
        // 他プレイヤーと重ならないように確認
        bool isCapaOver = false;
        PhotonView[] allss = FindObjectsOfType<PhotonView>();
        foreach (var css in allss)
        {
            if (css.IsMine) continue;
            var ss = css.GetComponent<selectSystem>();
            if (ss == null) continue;
            ss.GetNums(out int dn, out int bn);
            if (dn == driver && bn == builder)
            {
                isCapaOver = true;
                break;
            }
        }

        if (isCapaOver)
        {
            text.text = "NOW SELECT : CAPA OVER";
            return;
        }

        if (driver == selectDriverNum && builder == selectBuilderNum)
        {
            selectDriverNum = -1;
            selectBuilderNum = -1;
            return;
        }

        selectDriverNum = driver;
        selectBuilderNum = builder;
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

        photonView.RPC(nameof(RPC_SetColorNumber), RpcTarget.OthersBuffered, colorNumber);
    }

    [PunRPC]
    void RPC_SetColorNumber(int receivedColorNumber, PhotonMessageInfo info)
    {
        colorNumber = receivedColorNumber;
        UpdateColor();
    }

    public void UpdateColor()
    {
        //未設定なら処理なし
        if (colorNumber == -1) return;

        GetComponent<Image>().color = colorPalette[colorNumber % playersCount];
    }

    void OnDestroy()
    {
        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }
}
