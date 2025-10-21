using UnityEngine;
using UnityEngine.UIElements;
public class AIDriver_v2 : MonoBehaviour, IDriver
{
    private Transform tf;

    //センサーが検知したタグ
    private string underTag;
    private string frontTag;
    private string rightTag;
    private string leftTag;

    //旋回速度
    [SerializeField] private float turnSpeed = 1f;

    //目的地関係
    private Vector3 targetPos = Vector3.zero;
    [SerializeField] private float targetRadius = 3;

    void Awake()
    {
        tf = transform;
    }

    public void GetInputs(out float throttle, out float brake, out float steer)
    {
        throttle = steer = brake = 0;

        //センサーの出力
        float frontDist = 5f;  // 前方距離
        float sideOffset = 1.5f; // 左右オフセット
        float rayLen = 10f;

        Vector3 underPos = tf.position + new Vector3(0,1,0);
        Vector3 front = tf.position + new Vector3(0,1,0) + tf.forward * frontDist;
        Vector3 right = tf.position + new Vector3(0,1,0) + tf.forward * frontDist * 1f +  tf.right * sideOffset;
        Vector3 left =  tf.position + new Vector3(0,1,0) + tf.forward * frontDist * 1f + -tf.right * sideOffset;

        CastRay(underPos, rayLen, ref underTag);
        CastRay(front, rayLen,ref frontTag);
        CastRay(right, rayLen,ref rightTag);
        CastRay(left, rayLen ,ref leftTag);

        //現在地の判定
        if(underTag == "Road")
        {
            //ダートを避けて進む
            if (rightTag == "Dirt") steer -= turnSpeed;
            else if (leftTag == "Dirt") steer += turnSpeed;

            //地面にいた地点を更新する　復帰用
            targetPos = tf.position;
        }
        else
        {
            //Roadに戻る
            float dist = (tf.position - targetPos).magnitude;
            if (dist < targetRadius) return;

            //外積で左右どちらに曲がるか決定
            Vector3 targetDir = (targetPos - tf.position).normalized;
            Vector3 nowDir = tf.forward.normalized;
            targetDir.y = 0; nowDir.y = 0;
            float crossY = Vector3.Cross(nowDir, targetDir).y;

            if (crossY > 0) steer += turnSpeed;
            else steer -= turnSpeed;
        }

        //アクセルは常にON
        throttle = 1f;

        Debug.DrawRay(targetPos, Vector3.up, Color.red);
    }

    void CastRay(Vector3 origin, float len,ref string tag)
    {
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, len))
            tag = hit.collider.tag;
        else
            tag = "Default";

        Debug.DrawRay(origin, Vector3.down * len, Color.red);
    }
    public void SetWaypointContainer(WaypointContainer container)
    {

    }
}
