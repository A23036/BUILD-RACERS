using Photon.Pun;
using TMPro;

// MonoBehaviourPunCallbacksを継承して、photonViewプロパティを使えるようにする
public class NameDisp : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        var nameLabel = GetComponent<TextMeshPro>();

        var player = photonView.Owner;

        int number = System.Array.IndexOf(PhotonNetwork.PlayerList, player);
        nameLabel.text = $"{photonView.Owner.NickName}{number + 1}";
    }
}