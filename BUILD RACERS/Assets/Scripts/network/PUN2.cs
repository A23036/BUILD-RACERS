using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// MonoBehaviourPunCallbacksを継承して、PUNのコールバックを受け取れるようにする
public class PUN2 : MonoBehaviourPunCallbacks
{
    [Tooltip("BOTの生成数")]
    [SerializeField] int GenerateBotsNum = 0;
    private void Start()
    {
        // PhotonServerSettingsの設定内容を使ってマスターサーバーへ接続する
        PhotonNetwork.ConnectUsingSettings();

        //タイトルで決めた名前を反映
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName");
    }

    // マスターサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnConnectedToMaster()
    {
        // "Room"という名前のルームに参加する（ルームが存在しなければ作成して参加する）
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions(), TypedLobby.Default);
    }

    // ゲームサーバーへの接続が成功した時に呼ばれるコールバック
    public override void OnJoinedRoom()
    {
        // プレイヤー生成（自分）
        var position = new Vector3(Random.Range(-3f, 3f), 3, Random.Range(-3f, 3f));
        var player = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
        var playerCc = player.GetComponent<CarController>();
        playerCc.SetCamera();

        float geneX = 0, geneZ = 0;
        for(int i = 0;i < GenerateBotsNum;i++)
        {
            // CPUの生成　テスト
            position = new Vector3(geneX, 3, -i*geneZ);
            var cpu = PhotonNetwork.Instantiate("Player", position, Quaternion.identity);
            var cpuCc = cpu.GetComponent<CarController>();

            // cpu に AI を設定する。WaypointContainer を渡す（シーンに複数ある場合は適切に選択）
            var wpContainer = FindObjectOfType<WaypointContainer>(); // 単一ならこれでOK
            cpuCc.SetAI(wpContainer);

            geneX++;
            if(geneX >= 3)
            {
                geneX = 0;
                geneZ += .1f;
            }
        }
    }
}