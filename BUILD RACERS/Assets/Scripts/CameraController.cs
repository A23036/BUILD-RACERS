using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -6); // 追従オフセット
    [SerializeField] private float smoothSpeed = 5f;

    private Transform target;  // カートのTransform

    public void SetTarget(Transform newTarget) => target = newTarget;

    private void LateUpdate()
    {
        if (target == null) return;

        // --- 回転は水平方向だけ追従 ---
        // target の forward から XZ 平面上の方向だけ取り出す
        Vector3 forwardFlat = target.forward;
        forwardFlat.y = 0; // 上下の回転を無視
        forwardFlat.Normalize();

        // --- カメラの追従位置を計算 ---
        Quaternion flatRotation = Quaternion.LookRotation(forwardFlat);
        Vector3 desiredPosition;
        //Bで後方カメラ
        if (Input.GetKey(KeyCode.B))
        {
            Vector3 backOffset = offset;
            backOffset.z *= -1;
            desiredPosition = target.position + flatRotation * backOffset;
        }
        else
        {
            desiredPosition = target.position + flatRotation * offset;
        }

        // --- スムーズに追従 ---
        if(Input.GetKey(KeyCode.B)) transform.position = desiredPosition;
        else transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        //背面カメラから戻るときにスムーズが入らないように
        if (Input.GetKeyUp("b"))
        {
            transform.position = desiredPosition;
        }

        // --- カメラをプレイヤーの方向に向ける ---
        transform.LookAt(target.position + Vector3.up * 1.5f); // 1.5fで少し上を見させる
    }
}
