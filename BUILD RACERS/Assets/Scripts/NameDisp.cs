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

        if(nameLabel.text.Substring(0,3) != "CPU")
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

        //自分は非表示
        var parent = transform.parent;
        var cc = parent.gameObject.GetComponent<CarController>();
        if (cc.isMine) nameLabel.text = "";
    }

    private void Update()
    {
        //名前のビルボード
        transform.forward = Camera.main.transform.forward;
    }
}