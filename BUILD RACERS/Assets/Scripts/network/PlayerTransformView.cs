using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTransformView : MonoBehaviourPunCallbacks, IPunObservable
{
    /// <summary>
    /// trueなら線形補間をおこなう
    /// </summary>
    [SerializeField]
    private bool LinearInterpolation;

    /// <summary>
    /// trueなら線形外挿をおこなう
    /// </summary>
    [SerializeField]
    private bool LinearExtrapolation;

    private Vector3 p1, p2;
    private Quaternion r1, r2;

    private float interpolate;

    private float timer;
    private float lastRecvTime;

    //同期フレーム
    [SerializeField]
    private int INTERPOLATE;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //同期フレームの設定
        PhotonNetwork.SendRate = INTERPOLATE;
        PhotonNetwork.SerializationRate = INTERPOLATE;

        p1 = transform.position;
        p2 = transform.position;

        r1 = transform.rotation;
        r2 = transform.rotation;

        //同期フレームに応じて算出
        interpolate = 1f / PhotonNetwork.SerializationRate;
        timer = 0;

        //デフォルトで線形補間
        LinearInterpolation = true;
        LinearExtrapolation = false;
    }

    // Update is called once per frame
    void Update()
    {
        //自分でなければ補間
        if (!photonView.IsMine)
        {
            timer += Time.deltaTime;
            float rate = timer / interpolate;

            //同期処理なし
            if (rate > 1)
            {
                transform.position = p2;
                transform.rotation = r2;
                return;
            }

            //補間・予測処理
            if (LinearInterpolation)
            {
                //線形補間 0 <= rate <= 1
                transform.position = Vector3.Lerp(p1, p2, rate);
                transform.rotation = Quaternion.Slerp(r1, r2, rate);
            }
            else if (LinearExtrapolation)
            {
                //線形外挿 rateに制限はない
                transform.position = Vector3.LerpUnclamped(p1, p2, rate);
                transform.rotation = Quaternion.SlerpUnclamped(r1, r2, rate);
            }

            //線形補間と線形外挿の切り替え
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                LinearInterpolation = LinearExtrapolation;
                LinearExtrapolation = !LinearInterpolation;
            }
        }
    }

    //同期フレーム毎に呼ばれる
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (photonView.IsMine)
        {
            //自分なら座標を送信
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            var nextPos = (Vector3)stream.ReceiveNext();
            var nextRot = (Quaternion)stream.ReceiveNext();

            //他者なら補間のための座標を更新
            p1 = transform.position;
            p2 = nextPos;

            r1 = transform.rotation;
            r2 = nextRot;

            timer = 0;
        }
    }
}
