using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;

public class monitorSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    //同期対象の変数
    private int colorNumber;
    private string playerName;

    //カラーパレット
    Color[] colorPalette;
    private int playersCount = 16;

    private float timer;

    [SerializeField] private Vector3 offset;
    [SerializeField] private bool gamingColor;

    private TextMeshProUGUI nameBar;

    void Start()
    {
        timer = 0f;

        //セレクトの初期化
        PlayerPrefs.SetInt("driverNum", -1);
        PlayerPrefs.SetInt("engineerNum", -1);

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

    private void Awake()
    {
        //ネームバーの取得
        nameBar = transform.Find("NameBar").gameObject.GetComponent<TextMeshProUGUI>();
        playerName = PlayerPrefs.GetString("PlayerName");
    }

    void Update()
    {
        //色の更新
        UpdateColor();

        //ネームバーの更新
        UpdateNameBar();

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
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // このクライアントが所有者なら送る
            stream.SendNext(colorNumber);
            stream.SendNext(playerName);
        }
        else
        {
            // 他クライアントから受け取る
            colorNumber = (int)stream.ReceiveNext();

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

    public bool IsReady()
    {
        return false;
    }

    //ゲーミングカラーのアクティブ変更
    public void GamingMode(bool b)
    {
        gamingColor = b;
    }

    public void PrintLog()
    {
        int actorNumber = photonView.Owner.ActorNumber;
        Debug.Log("No." + actorNumber + " COLOR : " + colorNumber);
    }

    //ルームマスターが変更された際のコールバック
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMaster)
    {
        
    }

    void OnDestroy()
    {
        Debug.Log($"selectSystem OnDestroy called on {gameObject.name} instID={this.GetInstanceID()}");
    }

    //ルームから誰か抜けた時に呼ばれるコールバック
    public override void OnPlayerLeftRoom(Photon.Realtime.Player other)
    {
        //空きがあればセレクターとして参加
        var sceneManager = GameObject.Find("SceneManager");
        if (sceneManager != null)
        {
            selectScene sceneScript = sceneManager.GetComponent<selectScene>();
            if (sceneScript != null)
            {
                sceneScript.OnJoinedRoom();
            }
        }
    }
}