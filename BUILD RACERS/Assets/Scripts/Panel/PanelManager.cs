using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Runtime.Serialization.Formatters;

public class PanelManager : MonoBehaviour
{
    // デバッグ用テキスト
    [SerializeField] private TextMeshProUGUI debugText;

    private const int PanelWidth = 5;
    private const int PanelHeight = 5;
    private bool[,] panelState = new bool[PanelWidth, PanelHeight];
    private PartsType[,] placedPartsTypes = new PartsType[PanelWidth, PanelHeight];

    // グリッドオブジェクトの参照を保持
    private PanelGrid[,] panelGrids = new PanelGrid[PanelWidth, PanelHeight];

    // 配置済みパーツの管理
    private Dictionary<Parts, List<Vector2Int>> placedParts = new Dictionary<Parts, List<Vector2Int>>();

    private int? currentX;
    private int? currentY;

    private PartsBase previewPartsData = null;
    private PartsType previewPartsType;

    private int previewOriginX = -1;
    private int previewOriginY = -1;

    private Engineer engineer;

    public void SetEngineer(Engineer en)
    {
        engineer = en;
    }

    void Awake()
    {
        Initialize();

        RegisterAllGrids();
    }

    // 全てのPanelGridを登録
    private void RegisterAllGrids()
    {
        PanelGrid[] allGrids = FindObjectsOfType<PanelGrid>();

        foreach (PanelGrid grid in allGrids)
        {
            RegisterGrid(grid);
        }

        Debug.Log($"PanelManager: {allGrids.Length}個のグリッドを登録しました");
        SetDebugText($"GridSet({allGrids.Length})");
    }

    // 個別のグリッドを登録
    public void RegisterGrid(PanelGrid grid)
    {
        int x = grid.GetGridX();
        int y = grid.GetGridY();

        if (IsInRange(x, y))
        {
            panelGrids[x, y] = grid;
        }
        else
        {
            Debug.LogWarning($"グリッド座標({x},{y})は範囲外です");
        }
    }

    // グリッド座標からワールド座標を取得
    public Vector3 GetGridWorldPosition(int x, int y)
    {
        if (IsInRange(x, y) && panelGrids[x, y] != null)
        {
            return panelGrids[x, y].transform.position;
        }

        Debug.LogWarning($"グリッド({x},{y})が見つかりません");
        return Vector3.zero;
    }

    public void Initialize()
    {
        for (int x = 0; x < PanelWidth; x++)
        {
            for (int y = 0; y < PanelHeight; y++)
            {
                panelState[x, y] = false;
            }
        }
        placedParts.Clear();
    }

    // パーツが配置可能かチェック
    public bool CanPlaceParts(PartsBase partsData, int originX, int originY)
    {
        if (partsData == null) return false;

        SetDebugText($"PartsData:width " +  partsData.width + " height " + partsData.height);

        for (int y = 0; y < partsData.height; y++)
        {
            for (int x = 0; x < partsData.width; x++)
            {
                if (partsData.GetCell(x, y))
                {
                    int targetX = originX + x;
                    int targetY = originY + y;

                    // 範囲外チェック
                    if (!IsInRange(targetX, targetY))
                    {
                        return false;
                    }

                    // 既に占有されているかチェック
                    if (panelState[targetX, targetY])
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    // パーツを配置
    public bool PlaceParts(Parts parts, PartsBase partsData, PartsType type, int originX, int originY)
    {
        if (!CanPlaceParts(partsData, originX, originY))
        {
            return false;
        }

        // 配置するマスのリスト
        List<Vector2Int> occupiedCells = new List<Vector2Int>();

        for (int y = 0; y < partsData.height; y++)
        {
            for (int x = 0; x < partsData.width; x++)
            {
                if (partsData.GetCell(x, y))
                {
                    int targetX = originX + x;
                    int targetY = originY + y; ;

                    panelState[targetX, targetY] = true;
                    placedPartsTypes[targetX, targetY] = type;
                    occupiedCells.Add(new Vector2Int(targetX, targetY));
                }
            }
        }

        // 配置したパーツを記録
        placedParts[parts] = occupiedCells;

        // 配置したパーツ情報を送信
        engineer.SendItem(parts.GetPartsID());

        Debug.Log($"パーツを配置しました: 原点({originX},{originY})");
        Debug.Log($"配置パーツID:" + parts.GetPartsID());
        PrintState();

        return true;
    }

    // パーツを取り外し
    public bool RemoveParts(Parts parts)
    {
        if (!placedParts.ContainsKey(parts))
        {
            return false;
        }

        List<Vector2Int> occupiedCells = placedParts[parts];

        foreach (Vector2Int cell in occupiedCells)
        {
            panelState[cell.x, cell.y] = false;
        }

        placedParts.Remove(parts);

        Debug.Log("パーツを取り外しました");
        PrintState();

        return true;
    }

    // パーツが既に配置されているかチェック
    public bool IsPartsPlaced(Parts parts)
    {
        return placedParts.ContainsKey(parts);
    }

    // 現在のカーソル位置を取得
    public bool GetCurrentGridPos(out int x, out int y)
    {
        if (currentX.HasValue && currentY.HasValue)
        {
            x = currentX.Value;
            y = currentY.Value;
            return true;
        }
        x = 0;
        y = 0;
        return false;
    }

    public void SetOccupied(int x, int y)
    {
        if (IsInRange(x, y))
            panelState[x, y] = true;
        else
            Debug.LogWarning($"座標({x},{y})は範囲外です。");
    }

    public void SetEmpty(int x, int y)
    {
        if (IsInRange(x, y))
            panelState[x, y] = false;
        else
            Debug.LogWarning($"座標({x},{y})は範囲外です。");
    }

    public bool IsEmpty(int x, int y)
    {
        if (IsInRange(x, y))
            return !panelState[x, y];
        else
        {
            Debug.LogWarning($"座標({x},{y})は範囲外です。");
            return false;
        }
    }

    private bool IsInRange(int x, int y)
    {
        return x >= 0 && x < PanelWidth && y >= 0 && y < PanelHeight;
    }

    public void PrintState()
    {
        string log = "Panel States:\n";
        for (int y = 0; y < PanelHeight - 1; y++)
        {
            for (int x = 0; x < PanelWidth; x++)
            {
                log += panelState[x, y] ? "■ " : "□ ";
            }
            log += "\n";
        }
        Debug.Log(log);
    }

    // プレビューを設定
    public void SetPreview(PartsBase partsData, int originX, int originY, PartsType type)
    {
        previewPartsData = partsData;
        previewOriginX = originX;
        previewOriginY = originY;
        previewPartsType = type;
    }

    // プレビューをクリア
    public void ClearPreview()
    {
        previewPartsData = null;
        previewOriginX = -1;
        previewOriginY = -1;
    }

    // 指定座標がプレビュー範囲内かチェック
    public bool IsInPreview(int x, int y)
    {
        if (previewPartsData == null)
        {
            return false;
        }

        for (int py = 0; py < previewPartsData.height; py++)
        {
            for (int px = 0; px < previewPartsData.width; px++)
            {
                if (previewPartsData.GetCell(px, py))
                {
                    int targetX = previewOriginX + px;
                    int targetY = previewOriginY + py;

                    if (targetX == x && targetY == y)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // プレビューが配置可能かチェック
    public bool IsPreviewPlaceable()
    {
        if (previewPartsData == null)
        {
            SetDebugText("Cant Set");
            return false;
        }
        return CanPlaceParts(previewPartsData, previewOriginX, previewOriginY);
    }

    // パネルのサイズを取得（外部から参照用）
    public int GetPanelWidth() => PanelWidth;
    public int GetPanelHeight() => PanelHeight;

    public PartsType GetPreviewPartsType() => previewPartsType;

    public PartsType GetPlacedPartsType(int x, int y)
    {
        if (x < 0 || y < 0 || x >= PanelWidth || y >= PanelHeight)
            return PartsType.Passive; // デフォルト
        return placedPartsTypes[x, y];
    }
    public void SetDebugText(string s)
    {
        debugText.text = s;
    }
    public void AddDebugText(string s)
    {
        debugText.text = debugText.text + "\n" + s;
    }

    public Vector3 GetPanelPos(int gridX, int gridY)
    {
        Vector3 pos = panelGrids[gridX, gridY].transform.position;
        pos.z = -1;
        return pos;
    }

    public void SetGridPos(int x, int y)
    {
        currentX = x;
        currentY = y;
        ClearPreview();
    }

    public void ResetGridPos()
    {
        currentX = null;
        currentY = null;
    }

    public bool GetGridState(int x, int y) => panelState[x, y];

    // セットされているパーツの数を取得
    public Dictionary<PartsID, int> CountPlacedPartsByID()
    {
        // 集計用辞書を作成
        Dictionary<PartsID, int> countDict = new Dictionary<PartsID, int>();
        
        foreach (var kvp in placedParts)
        {
            Parts part = kvp.Key;
            if (part != null)
            {
                PartsID id = part.GetPartsID(); // Parts クラスで設定されている ID

                if (countDict.ContainsKey(id))
                    countDict[id]++;
                else
                    countDict[id] = 1;
            }
        }

        return countDict;
    }
}