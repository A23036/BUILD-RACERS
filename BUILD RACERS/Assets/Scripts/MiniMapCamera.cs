using UnityEngine;
using UnityEngine.InputSystem;

public class MiniMapCamera : MonoBehaviour
{
    [SerializeField] private Camera miniMapCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minSize = 10f;
    [SerializeField] private float maxSize = 80f;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0, 50, 0);

    private Transform target;

    // Input Actions
    private InputAction scrollAction;

    // ピンチ用
    private float previousPinchDistance;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    private void OnEnable()
    {
        // マウスホイール
        scrollAction = new InputAction(
            type: InputActionType.Value,
            binding: "<Mouse>/scroll/y"
        );
        scrollAction.Enable();
    }

    private void OnDisable()
    {
        scrollAction?.Disable();
    }

    private void LateUpdate()
    {
        if (miniMapCamera == null) return;

        //---------------------------------
        // ターゲット追従
        //---------------------------------
        if (target != null)
        {
            transform.position = target.position + offset;
        }

        //---------------------------------
        // PC：マウスホイール
        //---------------------------------
        float scroll = scrollAction.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            miniMapCamera.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
        }

        //---------------------------------
        // スマホ：ピンチ（New Input System）
        //---------------------------------
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;

            if (touches.Count >= 2 &&
                touches[0].press.isPressed &&
                touches[1].press.isPressed)
            {
                Vector2 pos0 = touches[0].position.ReadValue();
                Vector2 pos1 = touches[1].position.ReadValue();

                float currentDistance = Vector2.Distance(pos0, pos1);

                if (previousPinchDistance > 0f)
                {
                    float delta = currentDistance - previousPinchDistance;
                    miniMapCamera.orthographicSize -= delta * 0.1f;
                }

                previousPinchDistance = currentDistance;
            }
            else
            {
                previousPinchDistance = 0f;
            }
        }

        //---------------------------------
        // ズーム範囲制限
        //---------------------------------
        miniMapCamera.orthographicSize =
            Mathf.Clamp(miniMapCamera.orthographicSize, minSize, maxSize);
    }
}
