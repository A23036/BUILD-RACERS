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

        if(nameLabel.text != "CPU")
        {
            //シングルプレイと処理を分岐
            if(PhotonNetwork.IsConnected)
            {
                nameLabel.text = photonView.Owner.NickName;
            }
            else
            {
                nameLabel.text = PlayerPrefs.GetString("PlayerName");
            }
        }
    }

    private void Update()
    {
        //名前のビルボード
        transform.forward = Camera.main.transform.forward;
    }
}