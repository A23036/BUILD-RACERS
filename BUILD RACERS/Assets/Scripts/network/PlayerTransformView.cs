using Photon.Pun;
using UnityEngine;

public class AvatarTransformView : MonoBehaviourPunCallbacks, IPunObservable
{
    private const float InterpolationPeriod = 0.1f;

    private Vector3 p1, p2;
    private Quaternion r1, r2;
    private float elapsedTime;
    private bool isInterpolating;

    private void Start()
    {
        p1 = transform.position;
        p2 = p1;
        r1 = transform.rotation;
        r2 = r1;
        elapsedTime = 0f;
        isInterpolating = false;
    }

    private void Update()
    {
        if (!photonView.IsMine && isInterpolating)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / InterpolationPeriod);

            // 位置補間
            transform.position = Vector3.Lerp(p1, p2, t);
            // 回転補間
            transform.rotation = Quaternion.Slerp(r1, r2, t);

            // 補間が終わったら停止
            if (t >= 1f)
            {
                isInterpolating = false;
            }
        }
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 新しいデータを受信した時だけ補間開始
            p1 = transform.position;
            p2 = (Vector3)stream.ReceiveNext();

            r1 = transform.rotation;
            r2 = (Quaternion)stream.ReceiveNext();

            elapsedTime = 0f;
            isInterpolating = true;
        }
    }
}
