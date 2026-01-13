using Photon.Pun;
using System.Linq;
using UnityEngine;

public class iconButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private int driverNum = -1;
    [SerializeField] private int builderNum = -1;

    private selectSystem ss;

    void Start()
    {
        //オフライン時はここで取得
        if(!PhotonNetwork.IsConnected) ss = FindObjectOfType<selectSystem>();
    }

    private void Awake()
    {
    }

    // Update is called once per frame
    
    public void PushIcon()
    {
        //オフラインプレイ時の処理
        if (!PhotonNetwork.IsConnected)
        {
            if(ss == null)
            {
                ss = FindObjectOfType<selectSystem>();
            }

            ss.SetNumOffline(driverNum, builderNum);
            return;
        }

        //オンラインプレイ時の処理
        if (ss == null)
        {
            //自分のセレクターを検索する
            PhotonView[] allss = FindObjectsOfType<PhotonView>();

            //接続数をコンソールに出力
            Debug.Log("CONNECTS COUNT : " + allss.Count());
            
            foreach(var css in allss)
            {
                if (css.IsMine)
                {
                    ss = css.GetComponent<selectSystem>();
                }
            }
        }
        
        //ssが見つかれば位置を更新
        if(ss != null && ss.IsReady() == false)
        {
            ss.SetNum(driverNum, builderNum);
        }
    }
}
