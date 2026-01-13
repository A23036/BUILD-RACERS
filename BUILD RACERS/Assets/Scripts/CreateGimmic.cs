using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class CreateGimmic : MonoBehaviour
{
    public Camera miniMapCamera;   // ミニマップ用カメラ
    public RectTransform miniMapUI;// RawImage の RectTransform
    public GameObject spawnPrefab; // 置きたいプレハブ

    [SerializeField] private string[] groundTags = { "Road", "Dirt" };

    [SerializeField] private GameObject removeEffectPrefab;
    [SerializeField] private float effectLifeTime = 2f;

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

        Ray ray = miniMapCamera.ViewportPointToRay(normalized);

        // ミニマップ Ray
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);

        if (hits.Length == 0)
        {
            Debug.Log("[Gimmick] RaycastAll hit nothing");
            return false;
        }

        // 距離順ソート
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (!IsValidGround(hit.collider))
                continue;

            Vector3 placePos = hit.point;
            Quaternion rot = Quaternion.Euler(0f, -rotationY, 0f);

            // ★ 既存ギミック削除
            RemoveExistingGimmicks(placePos);

            // ★ 新規生成
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(
                    partsId.ToString(),
                    placePos,
                    rot
                );
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>(partsId.ToString());
                Instantiate(prefab, placePos, rot);
            }

            Debug.Log($"[Gimmick] Placed on {hit.collider.tag}");
            return true;
        }

        Debug.Log("[Gimmick] No valid ground found");
        return false;
    }

    // ----------------------------
    // 地面タグ判定
    // ----------------------------
    private bool IsValidGround(Collider col)
    {
        foreach (var tag in groundTags)
        {
            if (col.CompareTag(tag))
                return true;
        }
        return false;
    }

    // ----------------------------
    // 既存ギミック削除
    // ----------------------------
    private void RemoveExistingGimmicks(Vector3 position)
    {
        Collider[] cols = Physics.OverlapSphere(position, 1.0f);

        foreach (var col in cols)
        {
            if (!col.CompareTag("Gimmick"))
                continue;

            GameObject gimmick = col.gameObject;
            Vector3 effectPos = gimmick.transform.position;

            // 削除エフェクト生成
            SpawnRemoveEffect(effectPos);

            if (PhotonNetwork.IsConnected)
            {
                PhotonView pv = gimmick.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    PhotonNetwork.Destroy(gimmick);
                }
            }
            else
            {
                Destroy(gimmick);
            }
        }
    }

    // 削除時エフェクト生成
    private void SpawnRemoveEffect(Vector3 position)
    {
        if (removeEffectPrefab == null)
            return;

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Instantiate(
                removeEffectPrefab.name,
                position,
                Quaternion.identity
            );
        }
        else
        {
            GameObject effect =
                Instantiate(removeEffectPrefab, position, Quaternion.identity);
            Destroy(effect, effectLifeTime);
        }
    }

#if UNITY_EDITOR
    // デバッグ用：削除範囲表示
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
#endif
}