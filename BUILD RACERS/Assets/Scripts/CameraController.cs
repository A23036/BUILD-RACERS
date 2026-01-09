using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -6); // 追従オフセット
    [SerializeField] private float smoothSpeed = 5f;

    private Transform target;  // カートのTransform

    // ------------ 新InputSystem用 ------------
    private InputAction backCameraAction;
    // ----------------------------------------

    public void SetTarget(Transform newTarget) => target = newTarget;

    void Awake()
    {
        // ------------ InputAction 初期化（PC専用） ------------
        backCameraAction = new InputAction(
            name: "BackCamera",
            type: InputActionType.Button
        );
        backCameraAction.AddBinding("<Keyboard>/b");
        // -----------------------------------------------
    }

    void OnEnable()
    {
        backCameraAction.Enable();
    }

    void OnDisable()
    {
        backCameraAction.Disable();
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

        // ------------ スマホでは背面カメラ無効 ------------
        bool isMobile = Touchscreen.current != null;
        bool isBackCamera = !isMobile && backCameraAction.IsPressed();
        // -----------------------------------------------

        if (isBackCamera)
        {
            Vector3 backOffset = offset;
            backOffset.z *= -1;
            desiredPosition = target.position + flatRotation * backOffset;
        }
        else
        {
            desiredPosition = target.position + flatRotation * offset;
        }

        // ------------ PCのみ即時切り替え判定 ------------
        if (!isMobile &&
            (backCameraAction.WasPressedThisFrame() ||
             backCameraAction.WasReleasedThisFrame()))
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position =
                Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
        // -----------------------------------------------

        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
