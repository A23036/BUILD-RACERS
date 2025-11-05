using UnityEngine;

[CreateAssetMenu(fileName = "Parts", menuName = "PanelObject/Parts (Grid Editable)")]
public class PartsBase : ScriptableObject
{
    public int width = 3;
    public int height = 3;

    // コア位置（パーツ内の座標）
    public int coreX = 0;
    public int coreY = 0;

    // ピース形状（true=ブロックあり）
    public bool[,] grid = new bool[5,5];

    // UnityのSerializeが2次元配列を扱えないため、保存用に1次元配列を使用
    [SerializeField] private bool[] serializedGrid;

    // ビルド時にも呼べる初期化
    public void InitializeGrid()
    {
        if (serializedGrid == null || serializedGrid.Length != width * height)
            serializedGrid = new bool[width * height];

        grid = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                grid[x, y] = serializedGrid[index];
            }
        }
    }

    // エディタ用
    private void OnValidate()
    {
        InitializeGrid();
    }

    // エディタから呼ばれる更新メソッド
    public void SetCell(int x, int y, bool value)
    {
        grid[x, y] = value;
        serializedGrid[y * width + x] = value;
    }

    public bool GetCell(int x, int y)
    {
        return grid[x, y];
    }

    // コアの設定
    public void SetCore(int x, int y)
    {
        coreX = Mathf.Clamp(x, 0, width - 1);
        coreY = Mathf.Clamp(y, 0, height - 1);
    }
    
    // コアが有効なセル上にあるかチェック
    public bool IsCoreValid()
    {
        return GetCell(coreX, coreY);
    }
}
