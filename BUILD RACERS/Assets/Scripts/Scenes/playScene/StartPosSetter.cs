using UnityEngine;
using Photon.Pun;

public class StartPosSetter : MonoBehaviourPunCallbacks
{
    public Transform[] startPosList;
    private bool[] isSet;

    private string debugText = "READY";

    //スタートまでの時間を設定
    [SerializeField] private int untilStartTime;

    private void Awake()
    {
        //ドライバーのスタート地点を取得
        startPosList = new Transform[transform.childCount];
        isSet = new bool[transform.childCount];

        int i = 0;
        foreach (Transform child in transform)
        {
            startPosList[i] = child;

            //フラグ初期化
            isSet[i] = false;

            i++;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //N秒後にドライバー開始
        Invoke(nameof(DriverStart), untilStartTime);
    }

    // Update is called once per frame
    void Update()
    {
        //2秒後に消す
        if(debugText == "GO!")
        {
            Invoke(nameof(ResetDebugText), 2f);
        }
    }

    public Transform GetStartPos()
    {
        int idx = 0;
        for(int i = 0;i < isSet.Length;i++)
        {
            if (isSet[i]) continue;
            isSet[i] = true;
            idx = i;
            break;
        }

        Debug.Log("START POS" + startPosList[idx].position);
        return startPosList[idx];
    }

    public void DriverStart()
    {
        if(!PhotonNetwork.IsConnected)
        {
            //全カートの状態を運転へ
            var karts = FindObjectsOfType<CarController>();
            foreach(var kart in karts)
            {
                kart.StateToDrive();
            }
        }
        else if(PhotonNetwork.IsMasterClient)
        {
            //マスタークライアントのみ実行
            var karts = FindObjectsOfType<CarController>();
            foreach (var kart in karts)
            {
                // PhotonViewを取得してRPCを呼ぶ
                PhotonView photonView = kart.GetComponent<PhotonView>();
                if (photonView != null)
                {
                    //photonView.RPC("RPC_StateToDrive", RpcTarget.AllBuffered);
                    photonView.RPC("RPC_StateToDrive", RpcTarget.All);
                }
            }
        }

        debugText = "GO!";
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 80;
        style.normal.textColor = Color.red;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;  // Bold追加

        float width = 800;
        float height = 400;
        Rect rect = new Rect(
            (Screen.width - width) / 2,
            (Screen.height - height) / 2,
            width,
            height
        );

        GUI.Label(rect, debugText, style);
    }

    void ResetDebugText()
    {
        debugText = "";
    }
}
