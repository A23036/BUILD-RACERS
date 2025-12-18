using UnityEngine;

public class MiniMapCamera : MonoBehaviour
{
    public Camera miniMapCamera;
    public float zoomSpeed = 5f;
    public float minSize = 10f;
    public float maxSize = 80f;

    private Transform target;
    public Vector3 offset = new Vector3(0, 50, 0);

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void LateUpdate()
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
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            miniMapCamera.orthographicSize -= scroll * zoomSpeed;
        }

        //---------------------------------
        // スマホ：ピンチ（2本指）
        //---------------------------------
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float curDist = (t0.position - t1.position).magnitude;
            float prevDist =
                ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;

            float delta = curDist - prevDist;
            miniMapCamera.orthographicSize -= delta * 0.1f;
        }

        //---------------------------------
        // ズーム範囲制限
        //---------------------------------
        miniMapCamera.orthographicSize =
            Mathf.Clamp(miniMapCamera.orthographicSize, minSize, maxSize);
    }
}
