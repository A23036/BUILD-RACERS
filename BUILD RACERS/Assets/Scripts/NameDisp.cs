using Photon.Pun;
using TMPro;
using UnityEngine;

// MonoBehaviourPunCallbacksを継承して、photonViewプロパティを使えるようにする
public class NameDisp : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        var nameLabel = GetComponent<TextMeshPro>();

        var player = photonView.Owner;

        //nameLabel.text = $"{photonView.Owner.NickName}{photonView.OwnerActorNr}";
        nameLabel.text = photonView.Owner.NickName;
    }

    private void Update()
    {
        //名前のビルボード
        transform.forward = Camera.main.transform.forward;
    }
}