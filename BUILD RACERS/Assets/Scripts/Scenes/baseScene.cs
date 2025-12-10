using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

//シーンの親クラス

public class baseScene : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// 前のシーンの名前
    /// </summary>
    protected string preSceneName;

    private float logTimer;

    /// <summary>
    /// そのシーンをネットワークに繋げるか
    /// </summary>
    [SerializeField] protected bool isConnect = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected void Start()
    {
        logTimer = 0f;
    }

    protected void Awake()
    {
        //未接続なら接続処理を実行
        if (isConnect && !PhotonNetwork.IsConnected)
        {
            Connect();
        }
        //オフラインシーンで接続がされていれば切断
        else if(!isConnect && PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        //現在のマスターがいるシーンを更新・共有できるように
        if (PhotonNetwork.IsMasterClient)
        {
            var props = new Hashtable();
            props["masterGameScene"] = SceneManager.GetActiveScene().name;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        //接続状態をコンソールに表示
        logTimer += Time.deltaTime;
        if (logTimer >= 1f)
        {
            if (PhotonNetwork.IsConnected) Debug.Log("[NETWORK STAT] ONLINE");
            else Debug.Log("[NETWORK STAT] OFFLINE");
            
            logTimer = 0f;
        }
    }

    protected void PushBackButton()
    {
        if(preSceneName != null) SceneManager.LoadScene(preSceneName);
    }

    protected void Connect()
    {
        // PhotonServerSettingsの設定内容を使ってマスターサーバーへ接続する
        PhotonNetwork.ConnectUsingSettings();
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster()
    {
        // "Room"という名前のルームに参加する（ルームが存在しなければ作成して参加する）
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions(), TypedLobby.Default);
    }
}