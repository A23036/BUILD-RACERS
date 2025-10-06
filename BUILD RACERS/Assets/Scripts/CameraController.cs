using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;  // カートのTransform
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -6); // 追従オフセット
    [SerializeField] private float smoothSpeed = 5f;

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
        Vector3 desiredPosition = target.position + flatRotation * offset;

        // --- スムーズに追従 ---
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // --- カメラをプレイヤーの方向に向ける ---
        transform.LookAt(target.position + Vector3.up * 1.5f); // 1.5fで少し上を見させる
    }
}
