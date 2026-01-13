using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class CreateGimmic : MonoBehaviour
{
    public Camera miniMapCamera;   // ミニマップ用カメラ
    public RectTransform miniMapUI;// RawImage の RectTransform
    public GameObject spawnPrefab; // 置きたいプレハブ

    public bool TrySpawnAtScreenPosition(Vector2 screenPos, PartsID partsId, float rotationY)
    {
        // Canvas のカメラを取得
        Canvas canvas = miniMapUI.GetComponentInParent<Canvas>();
        Camera uiCamera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        // ----------------------------
        // RawImage 上かチェック
        // ----------------------------
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            miniMapUI, screenPos, uiCamera))
        {
            Debug.Log("[Gimmick] Not on RawImage");
            return false;
        }
        // ----------------------------
        // RawImage 内ローカル座標取得
        // ----------------------------
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            miniMapUI, screenPos, uiCamera, out Vector2 localPos);

        // 0?1 正規化
        Vector2 normalized = new(
            (localPos.x / miniMapUI.rect.width) + 0.5f,
            (localPos.y / miniMapUI.rect.height) + 0.5f
        );

        // ミニマップ Ray
        Ray ray = miniMapCamera.ViewportPointToRay(normalized);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            Debug.Log($"[Gimmick] Place at {hit.point}");

            Quaternion rot = Quaternion.Euler(0f, -rotationY, 0f);

            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(partsId.ToString(), hit.point, rot);
            }
            else
            {
                Instantiate(Resources.Load(partsId.ToString()), hit.point, rot);
            }
            return true;
        }

        Debug.Log("[Gimmick] Raycast failed");
        return false;
    }


    void ConvertToWorldAndSpawn(Vector2 localPos)
    {
        // --- UI の幅・高さから 0?1 に正規化 ---
        Vector2 normalized = new Vector2(
            (localPos.x / miniMapUI.rect.width) + 0.5f,
            (localPos.y / miniMapUI.rect.height) + 0.5f
        );

        // --- ミニマップカメラの RenderTexture 空間の UV 座標に変換 ---
        Ray ray = miniMapCamera.ViewportPointToRay(normalized);

        // --- 地面との当たり判定 ---
        if (Physics.Raycast(ray, out RaycastHit hit, 500f/*, LayerMask.GetMask("Ground")*/))
        {
            Debug.Log("設置位置" + hit.point);

            //オンラインかそうでないかで処理を分ける
            if(PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate("Mud", hit.point, Quaternion.identity);
            }
            else
            {
                Instantiate(Resources.Load("Mud"),hit.point, Quaternion.identity);
            }
        }
    }
}
