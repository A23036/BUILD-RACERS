using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PartsBase))]
public class PartsEditor : Editor
{
    private const int CellSize = 25;
    private bool coreEditMode = false;

    public override void OnInspectorGUI()
    {
        PartsBase parts = (PartsBase)target;

        // 通常のプロパティ（width / height）
        parts.width = EditorGUILayout.IntField("Width", parts.width);
        parts.height = EditorGUILayout.IntField("Height", parts.height);

        GUILayout.Space(10);

        // コア位置の表示と編集モード切替
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Core Position: ({parts.coreX}, {parts.coreY})", EditorStyles.boldLabel);

        Color prevBgColor = GUI.backgroundColor;
        GUI.backgroundColor = coreEditMode ? Color.yellow : Color.white;
        if (GUILayout.Button(coreEditMode ? "編集中 (クリックで完了)" : "コア位置を編集", GUILayout.Width(150)))
        {
            coreEditMode = !coreEditMode;
        }
        GUI.backgroundColor = prevBgColor;
        EditorGUILayout.EndHorizontal();

        // コアが有効なセル上にない場合は警告
        if (!parts.IsCoreValid())
        {
            EditorGUILayout.HelpBox("警告: コアが空白セル上にあります。有効なセルに配置してください。", MessageType.Warning);
        }

        GUILayout.Space(10);
        GUILayout.Label("パーツ形状（クリックで切り替え）", EditorStyles.boldLabel);

        if (coreEditMode)
        {
            EditorGUILayout.HelpBox("コア位置編集モード: グリッドをクリックしてコア位置を設定", MessageType.Info);
        }

        // グリッド描画
        for (int y = 0; y < parts.height; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < parts.width; x++)
            {
                bool current = parts.GetCell(x, y);
                bool isCore = (parts.coreX == x && parts.coreY == y);

                Color prevColor = GUI.backgroundColor;

                // 色分け: コア=赤、有効セル=緑、無効セル=灰色
                if (isCore)
                    GUI.backgroundColor = Color.red;
                else if (current)
                    GUI.backgroundColor = Color.green;
                else
                    GUI.backgroundColor = Color.gray;

                string buttonLabel = isCore ? "●" : "";

                if (GUILayout.Button(buttonLabel, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    if (coreEditMode)
                    {
                        // コア編集モード: コア位置を設定
                        Undo.RecordObject(parts, "Set Core Position");
                        parts.SetCore(x, y);
                        EditorUtility.SetDirty(parts);
                    }
                    else
                    {
                        // 通常モード: セルのオン/オフ切替
                        Undo.RecordObject(parts, "Toggle Cell");
                        parts.SetCell(x, y, !current);
                        EditorUtility.SetDirty(parts);
                    }
                }

                GUI.backgroundColor = prevColor;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear All"))
        {
            Undo.RecordObject(parts, "Clear Grid");
            for (int y = 0; y < parts.height; y++)
                for (int x = 0; x < parts.width; x++)
                    parts.SetCell(x, y, false);

            EditorUtility.SetDirty(parts);
        }

        GUILayout.Space(5);

        // コアを中央に設定するボタン
        if (GUILayout.Button("Set Core to Center"))
        {
            Undo.RecordObject(parts, "Set Core to Center");
            parts.SetCore(parts.width / 2, parts.height / 2);
            EditorUtility.SetDirty(parts);
        }
    }
}