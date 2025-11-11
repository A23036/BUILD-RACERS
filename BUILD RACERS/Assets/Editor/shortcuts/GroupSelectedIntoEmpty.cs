using UnityEditor;
using UnityEngine;

public static class GroupUnderLastSelected
{
    // Ctrl + G (%g)
    [MenuItem("Tools/Hierarchy/Group Selected Into Empty %g")]
    private static void GroupSelected()
    {
        var sel = Selection.transforms;
        if (sel == null || sel.Length == 0)
        {
            Debug.LogWarning("オブジェクトを選択してください。");
            return;
        }

        // 親となる空オブジェクトを作成
        GameObject group = new GameObject("Group");
        Undo.RegisterCreatedObjectUndo(group, "Create Group");

        Transform groupT = group.transform;

        // 子たちのワールド座標の重心（中心位置）を計算
        Vector3 center = Vector3.zero;
        foreach (var t in sel) center += t.position;
        center /= sel.Length;

        // グループを重心に移動
        groupT.position = center;

        // 各選択オブジェクトをグループの子にする（Undo対応）
        foreach (var t in sel)
        {
            // Skip if it's the group itself (防御処理)
            if (t == groupT) continue;

            // Undo を使って親子付け（ワールド位置を維持）
            Undo.SetTransformParent(t, groupT, "Group Selected");
        }

        // 新しく作ったグループを選択
        Selection.activeGameObject = group;
        Debug.Log($"選択オブジェクト {sel.Length} 個を '{group.name}' にグループ化しました。");
    }

    // Ctrl + Shift + G でアンパック（グループ解除）
    [MenuItem("Tools/Hierarchy/Ungroup Selected %#g")]
    private static void UngroupSelected()
    {
        var sel = Selection.transforms;
        if (sel == null || sel.Length == 0)
        {
            Debug.LogWarning("オブジェクトを選択してください。");
            return;
        }

        foreach (var t in sel)
        {
            if (t.parent == null) continue;
            // 親に対する Undo を登録してから子をルート化
            Undo.SetTransformParent(t, null, "Unparent Selected");
        }

        Debug.Log("選択オブジェクトの親を解除しました。");
    }

    // Validate: Group のメニューを有効にする条件
    [MenuItem("Tools/Hierarchy/Group Selected Into Empty %g", true)]
    private static bool ValidateGroupSelected()
    {
        return Selection.transforms.Length > 0;
    }

    [MenuItem("Tools/Hierarchy/Ungroup Selected %#g", true)]
    private static bool ValidateUngroupSelected()
    {
        return Selection.transforms.Length > 0;
    }
}
