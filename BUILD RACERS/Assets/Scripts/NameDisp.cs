using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// MonoBehaviourPunCallbacksを継承して、photonViewプロパティを使えるようにする
public class NameDisp : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        var nameLabel = GetComponent<TextMeshPro>();

        var player = photonView.Owner;

        //nameLabel.text = $"{photonView.Owner.NickName}{photonView.OwnerActorNr}";
        if(nameLabel.text != "CPU") nameLabel.text = photonView.Owner.NickName;
    }

    private void Update()
    {
        //名前のビルボード
        transform.forward = Camera.main.transform.forward;
    }
}