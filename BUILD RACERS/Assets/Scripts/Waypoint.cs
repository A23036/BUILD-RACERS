using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Waypoint : MonoBehaviour
{
    [Tooltip("ウェイポイントID")]
    public string id = "0";

    [SerializeField, Tooltip("ラベルの文字サイズ")]
    private int labelFontSize = 12;

    [SerializeField, Tooltip("ギズモの半径")]
    private float sphereRadius = 0.3f;

    [SerializeField, Tooltip("ギズモの透明度 (0〜1)")]
    [Range(0f, 1f)] private float gizmoAlpha = 0.4f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 半透明シアン
        Gizmos.color = new Color(0f, 1f, 1f, gizmoAlpha);
        Gizmos.DrawSphere(transform.position, sphereRadius);

        // ラベル描画
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = labelFontSize;
        style.alignment = TextAnchor.MiddleCenter;

        Handles.Label(transform.position, id, style);
    }
#endif
}
