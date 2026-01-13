using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.VisualScripting;
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

    // カメラ固定フラグ
    private bool isFixed = false;
    private Vector3 fixedPos;
    private float elapsedTime = 0f;
    [SerializeField]private float fixedTime = 3f;

    //リザルトフラグ
    private bool isResult = false;

    //リザルトカメラワーク座標
    [SerializeField] private Transform[] viewPoints;
    private float switchTime = 0f;

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

        //リザルトなら定期的にカメラワークを変更
        if(isResult)
        {
            if(switchTime == 0f)
            {
                //固定と追従を交互にする
                int r = 0;
                if (isFixed)
                {
                    //最短地点を固定座標にする
                    float len = 10000;
                    Vector3 kartPos = Vector3.zero;
                    var karts = FindObjectsOfType<CarController>();
                    foreach(var kart in karts)
                    {
                        if(kart.isMine) kartPos = kart.transform.position;
                    }

                    for(int i = 0;i < viewPoints.Length;i++)
                    {
                        float dist = (kartPos - viewPoints[i].transform.position).magnitude;
                        if (len > dist)
                        {
                            len = dist;
                            r = i;
                        }
                    }
                    SetFixedPos(viewPoints[r].transform.position);
                    isFixed = false;
                    if (len != 1e6) Debug.Log("Fix");
                    else Debug.LogError("Not Found ViewPoint");
                }
                else
                {
                    r = Random.Range(0, viewPoints.Length);
                    isFixed = true;
                    Debug.Log("Target");
                }

                switchTime = 3f;
            }
            else
            {
                switchTime = Mathf.Max(0f, switchTime - Time.deltaTime);
            }
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

        // ------------ カメラ固定処理 ------------
        if (switchTime > 0f && !isFixed)
        {
            transform.position = fixedPos;

            //視線が通らなければ追従へ戻す
            float distance = 1000f;

            // Rayはカメラの位置からとばす
            var rayStartPosition = transform.position;
            // Rayはカメラが向いてる方向にとばす
            var rayDirection = transform.forward.normalized;

            // Hitしたオブジェクト格納用
            RaycastHit raycastHit;

            // Rayを飛ばす（out raycastHit でHitしたオブジェクトを取得する）
            var isHit = Physics.Raycast(rayStartPosition, rayDirection, out raycastHit, distance);

            // Debug.DrawRay (Vector3 start(rayを開始する位置), Vector3 dir(rayの方向と長さ), Color color(ラインの色));
            Debug.DrawRay(rayStartPosition, rayDirection * distance, Color.red);

            // なにかを検出したら
            if (isHit)
            {
                //カートに視線が通っていれば固定続行
                var cc = raycastHit.collider.gameObject.GetComponent<CarController>();
                if(cc != null)
                {
                    isHit = false;
                }
                else
                {
                    isHit= true;
                }
            }
            else
            {
                Debug.Log("RAY NOT HIT");
            }

            Debug.DrawRay(rayStartPosition, rayDirection * distance, Color.red);

            //最短地点を固定座標にする
            float len = 10000;
            Vector3 kartPos = Vector3.zero;
            var karts = FindObjectsOfType<CarController>();
            foreach (var kart in karts)
            {
                if (kart.isMine) kartPos = kart.transform.position;
            }

            int idx = 0;
            for (int i = 0; i < viewPoints.Length; i++)
            {
                float dist = (kartPos - viewPoints[i].transform.position).magnitude;
                if (len > dist)
                {
                    len = dist;
                    idx = i;
                }
            }
            SetFixedPos(viewPoints[idx].transform.position);

            if (elapsedTime >= fixedTime || isHit)
            {
                isFixed = false;
                elapsedTime = 0f;
            }
        }
    }

    private void Update()
    {
        if (isResult)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// カメラを一定時間固定
    /// </summary>
    /// <param name="pos">固定座標</param>
    /// <param name="f">固定時間</param>
    public void SetFixedPos(Vector3 pos , float f = 3f)
    {
        fixedPos = pos;
        isFixed = true;
        elapsedTime = 0f;
        fixedTime = f;
    }

    //リザルトシーンに遷移したらtrueをセット
    public void SetIsResult(bool b)
    {
        isResult = b;
        isFixed = b;
    }
}
