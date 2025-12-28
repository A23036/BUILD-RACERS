using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -6); // 追従オフセット
    [SerializeField] private float smoothSpeed = 5f;

    private Transform target;  // カートのTransform

    public void SetTarget(Transform newTarget) => target = newTarget;

    void Start()
    {
    }

    void Awake()
    {
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition;

        Vector3 forwardFlat = target.forward;
        forwardFlat.y = 0;
        forwardFlat.Normalize();

        Quaternion flatRotation = Quaternion.LookRotation(forwardFlat);

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

        // --- スムーズに追従 --- 背面カメラの時はスムーズを適用しない
        if (Input.GetKeyDown("b") || Input.GetKeyUp("b")) transform.position = desiredPosition;
        else transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // --- カメラをプレイヤーの方向に向ける ---
        transform.LookAt(target.position + Vector3.up * 1.5f); // 1.5fで少し上を見させる
    }
}
