using Photon.Pun;
using TMPro;

// MonoBehaviourPunCallbacksを継承して、photonViewプロパティを使えるようにする
public class NameDisp : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        var nameLabel = GetComponent<TextMeshPro>();

        var player = photonView.Owner;
        /*
        //プレイヤー名の割り当て
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (PhotonNetwork.PlayerList[i] != player) continue;
            nameLabel.text = $"{photonView.Owner.NickName}{i + 1}";
        }
        */

        int number = System.Array.IndexOf(PhotonNetwork.PlayerList, player);
        nameLabel.text = $"{photonView.Owner.NickName}{number}";
    }
}