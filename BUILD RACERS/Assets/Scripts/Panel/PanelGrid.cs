using UnityEngine;
using UnityEngine.UI;

public class PanelGrid : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private string passiveSpriteName;
    [SerializeField] private string itemSpriteName;
    [SerializeField] private string previewEnableSpriteName;

    private Sprite passiveSprite;
    private Sprite itemSprite;
    private Sprite previewEnableSprite;

    [SerializeField] private int gridX;
    [SerializeField] private int gridY;

    private PanelManager panelManager;

    // グリッド座標を取得（外部参照用）
    public int GetGridX() => gridX;
    public int GetGridY() => gridY;

    void Awake()
    {
        panelManager = FindAnyObjectByType<PanelManager>();
    }

    void Start()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // Resources からロード
        passiveSprite = Resources.Load<Sprite>("Sprite/" + passiveSpriteName);
        itemSprite = Resources.Load<Sprite>("Sprite/" + itemSpriteName);
        previewEnableSprite = Resources.Load<Sprite>("Sprite/" + previewEnableSpriteName);

        // PanelManagerに自分を登録
        if (panelManager != null)
        {
            panelManager.RegisterGrid(this);
        }

        if (targetImage != null)
        {
            // 初期状態を明示的に設定
            targetImage.sprite = passiveSprite;
            SetAlpha(1f);
        }
    }
    
    private void Update()
    {
        if (panelManager == null) return;

        // 既に配置されているマス
        if (panelManager.GetGridState(gridX, gridY))
        {
            PartsType placedType = panelManager.GetPlacedPartsType(gridX, gridY);

            Sprite placedSprite;

            if (placedType == PartsType.Passive)
            {
                placedSprite = passiveSprite;
            }
            else if (placedType == PartsType.Item)
            {
                placedSprite = itemSprite;
            }
            else
            {
                placedSprite = null;
            }

            targetImage.sprite = placedSprite;
            SetAlpha(1f);
            return;
        }

        // プレビュー表示
        if (panelManager.IsInPreview(gridX, gridY))
        {
            bool canPlace = panelManager.IsPreviewPlaceable();
            PartsType previewType = panelManager.GetPreviewPartsType();

            Sprite activeSprite;

            if(previewType == PartsType.Passive)
            {
                activeSprite = passiveSprite;
            }
            else if(previewType == PartsType.Item)
            {
                activeSprite = itemSprite;
            }
            else
            {
                activeSprite = null;
            }

            targetImage.sprite = canPlace ? activeSprite : previewEnableSprite;
            SetAlpha(0.5f);
        }
        else
        {
            // 何も表示しない
            targetImage.sprite = null;
            SetAlpha(0f);
        }
    }
    
    public void OnPointerEnter()
    {
        if (targetImage != null)
        {
            panelManager.SetGridPos(gridX, gridY);
        }
    }

    public void OnPointerExit()
    {
        if (targetImage != null)
        {
            panelManager.ResetGridPos();
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = targetImage.color;
        c.a = alpha;
        targetImage.color = c;
    }

}