using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class CreateGimmic : MonoBehaviour, IPointerClickHandler
{
    public Camera miniMapCamera;   // ミニマップ用カメラ
    public RectTransform miniMapUI;// RawImage の RectTransform
    public GameObject spawnPrefab; // 置きたいプレハブ

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;

        // RawImage 内でのローカル座標を取得 (0,0が中心)
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            miniMapUI, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            ConvertToWorldAndSpawn(localPoint);
        }
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
            PhotonNetwork.Instantiate("Mud", hit.point, Quaternion.identity);
        }
    }
}
