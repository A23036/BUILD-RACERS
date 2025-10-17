// WaypointContainer.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inspectorで編集可能なウェイポイントコンテナ。
/// プレハブでAIBotを生成した際、AIDriverはこのコンテナを参照します。
/// 実行中に経路を差し替えたり、編集できます（publicフィールドは使わない設計）。
/// </summary>
public class WaypointContainer : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    public IReadOnlyList<Transform> Waypoints => waypoints;

    private void Awake()
    {
        PopulateFromChildren();
    }

    // Inspectorで子オブジェクトをウェイポイントとして取り込む（ContextMenuで実行可能）
    [ContextMenu("Populate from children")]
    private void PopulateFromChildren()
    {
        waypoints.Clear();
        foreach (Transform t in transform)
        {
            waypoints.Add(t);
        }
    }

    public void SetWaypoints(IEnumerable<Transform> newWps)
    {
        waypoints.Clear();
        waypoints.AddRange(newWps);
    }

    public void AddWaypoint(Transform wp)
    {
        if (wp != null) waypoints.Add(wp);
    }

    public void ClearWaypoints()
    {
        waypoints.Clear();
    }
}
