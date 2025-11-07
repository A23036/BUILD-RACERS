using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class selectSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    private int selectDriverNum;
    private int selectBuilderNum;

    [SerializeField] private Vector3 offset;

    private IconManager im;
    private List<Transform> driverIcons;
    private List<Transform> builderIcons;

    private TextMeshProUGUI text;

    void Start()
    {
        selectDriverNum = -1;
        selectBuilderNum = -1;
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
    }

    void Update()
    {
        // 表示（自分のオブジェクトにだけ描画を任せる場合）
        if (!photonView.IsMine) return;

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
            }
            else
            {
                transform.position = builderIcons[selectBuilderNum].position + offset;
                text.text = "NOW SELECT : BUILDER" + (selectBuilderNum + 1);
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
            Debug.Log("PUSH : " + driver + " " + builder);
            Debug.Log("FILLED : " + dn + " " + bn);
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

            Debug.Log($"[Serialize] Send: {selectDriverNum}, {selectBuilderNum}");
        }
        else
        {
            // 他クライアントから受け取る
            selectDriverNum = (int)stream.ReceiveNext();
            selectBuilderNum = (int)stream.ReceiveNext();

            Debug.Log($"[Serialize] Recv: {selectDriverNum}, {selectBuilderNum}");
        }
    }

    void OnDestroy()
    {
        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }
}
