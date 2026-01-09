using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Parts : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private bool isDragging = false;
    private Vector3 offset;
    //※int?:nullable型(nullを値として許容する型)
    private int? draggingTouchId = null; // 現在ドラッグ中のタッチID
    
    [SerializeField]
    private PartsBase partsData;

    [SerializeField]
    private PartsType partsType;    // パーツの種類:パッシブ、アイテム、ギミック

    [SerializeField]
    private PartsID partsId;        // パーツ番号

    [SerializeField] private string partsResourceName; // Resourcesフォルダからロードする場合の名前

    private PanelManager panelManager;
    private bool isPlaced = false; // 配置済みフラグ
    private Vector3 originalPosition; // ドラッグ開始時の位置

    private Canvas canvas;
    private RectTransform rectTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 2f;

        panelManager = FindAnyObjectByType<PanelManager>();

        canvas = GetComponentInParent<Canvas>();
        rectTransform = GetComponent<RectTransform>();

        if (panelManager == null)
            Debug.LogError("PanelManager が見つかりません！");

        // Resourcesからロードして partsData が null を回避
        if (partsData == null && !string.IsNullOrEmpty(partsResourceName))
        {
            partsData = Resources.Load<PartsBase>("PartsData/" + partsResourceName);
            if (partsData == null)
                Debug.LogError($"PartsBase {partsResourceName} が見つかりません！");
        }

        // grid を初期化
        if (partsData != null)
            partsData.InitializeGrid();

        UpdateColliderState();
    }

    void Update()
    {
        Vector3 pointerWorldPos = Vector3.zero;
        bool pointerUp = false;

        // ------------------------
        // マウス入力（PC）
        // ------------------------
        if (Mouse.current != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mousePos.z = 0f;

            // ドラッグ開始
            if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
            {
                Collider2D hit = Physics2D.OverlapPoint(mousePos);
                if (hit == GetComponent<Collider2D>())
                {
                    StartDragging(mousePos);
                }
            }

            // ドラッグ中
            if (isDragging && draggingTouchId == null) // マウスでドラッグ中
            {
                pointerWorldPos = mousePos;
                pointerUp = Mouse.current.leftButton.wasReleasedThisFrame;
            }
        }

        // ------------------------
        // タッチ入力（スマホ）
        // ------------------------
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                int touchId = touch.touchId.ReadValue();
                Vector3 touchPos = Camera.main.ScreenToWorldPoint(touch.position.ReadValue());
                touchPos.z = 0f;

                // タッチ開始
                if (touch.press.wasPressedThisFrame && !isDragging)
                {
                    Collider2D hit = Physics2D.OverlapPoint(touchPos);
                    if (hit == GetComponent<Collider2D>())
                    {
                        StartDragging(touchPos);
                        draggingTouchId = touchId;
                    }
                }

                // ドラッグ中の座標更新
                if (isDragging && draggingTouchId == touchId)
                {
                    pointerWorldPos = touchPos;
                    pointerUp = touch.press.wasReleasedThisFrame;
                }
            }
        }

        // ドラッグ中に Rigidbody を移動
        if (isDragging)
        {
            rb.MovePosition(pointerWorldPos + offset);

            UpdatePreview();
        }

        // ドラッグ終了
        if (pointerUp && isDragging)
        {
            EndDragging();
        }

        // 画面外に落ちたら削除
        if (IsOutOfScreen())
        {
            panelManager.itemUsed();
            Destroy(gameObject);
        }
    }

    // 画面外判定
    private bool IsOutOfScreen()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPos.y < 0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // ドラッグ開始処理
    private void StartDragging(Vector3 pointerPos)
    {
        isDragging = true;
        originalPosition = transform.position;
        offset = transform.position - pointerPos;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        // 既に配置されている場合は取り外す
        if (isPlaced)
        {
            panelManager.RemoveParts(this);
            isPlaced = false;
        }

        // Collider を掴み判定可能に
        UpdateColliderState();
    }

    // ドラッグ終了処理
    private void EndDragging()
    {
        isDragging = false;
        draggingTouchId = null;
        panelManager.ClearPreview();

        Debug.Log($"[Parts] ドラッグ終了");

        // パネルに設置するパーツの時
        if (partsType == PartsType.Passive || partsType == PartsType.Item)
        {
            // パネル上のグリッド座標を取得
            if (panelManager.GetCurrentGridPos(out int gridX, out int gridY))
            {
                Debug.Log($"[Parts] グリッド座標取得: ({gridX}, {gridY})");

                if (partsData == null)
                {
                    Debug.LogError("[Parts] partsData が null です！Inspector で PartsBase を設定してください。");
                    ReturnToOriginalPosition();
                    return;
                }

                // 右下を掴んでいるので、左上の原点座標を計算

                int originX = gridX - (partsData.width - 1);
                int originY = gridY - (partsData.height - 1);

                // 配置可能かチェック
                if (panelManager.CanPlaceParts(partsData, originX, originY))
                {
                    // 配置成功
                    panelManager.PlaceParts(this, partsData, partsType, originX, originY);
                    isPlaced = true;

                    // パーツをグリッドにスナップ
                    SnapToGrid(gridX, gridY);

                    // 重力を無効化（配置済み）
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    // 配置失敗：元の位置に戻す or 落下
                    Debug.Log("配置できません！");
                    // デバッグテキスト
                    panelManager.SetDebugText("Cant Set");
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                // パネル外で離した：落下させる
                rb.gravityScale = 2f;
            }


            // 配置後 Collider を物理衝突無効化
            UpdateColliderState();
        }
        else // マップに設置するパーツの時
        {
            // CreateGimmickに自身のパーツIDと離した座標を送信
            Vector2 screenPos;

            if (Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();
            else if (Touchscreen.current != null)
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            else
                return;

            CreateGimmic createGimmick = FindAnyObjectByType<CreateGimmic>();

            if (createGimmick == null)
            {
                Debug.LogError("CreateGimmic が見つかりません！");
                ReturnToOriginalPosition();
                return;
            }

            bool placed = createGimmick.TrySpawnAtScreenPosition(screenPos, partsId);

            if (placed)
            {
                Debug.Log("[Parts] Gimmick placed");
                panelManager.itemUsed();
                Destroy(gameObject); // パーツを削除
            }
            else
            {
                Debug.Log("[Parts] Gimmick placement failed");

                // ミニマップ外で離した：落下させる
                rb.gravityScale = 2f;
            }
        }
    }

    private void UpdateColliderState()
    {
        if (isDragging || !isPlaced)
        {
            col.enabled = true;      // 判定有効
            col.isTrigger = false;   // 物理衝突も有効
        }
        else
        {
            col.enabled = true;      // Collider は残す（OverlapPointで掴める）
            col.isTrigger = true;    // 物理衝突のみ無効化
        }
    }

    // グリッド座標にスナップ
    private void SnapToGrid(int gridX, int gridY)
    {
        // コア位置を考慮してワールド座標を計算
        // originX, originYはパーツの左上、コアはパーツ内の相対位置
        int coreGridX = gridX - (partsData.width - partsData.coreX - 1);
        int coreGridY = gridY - (partsData.height - partsData.coreY - 1);
        //int coreGridY = gridY + (partsData.height - 1 - partsData.coreY);

        Debug.Log($"[SnapToGrid] 原点: ({gridX},{gridY}), コア位置: パーツ内({partsData.coreX},{partsData.coreY}), グリッド上({coreGridX},{coreGridY})");

        // デバッグテキスト
        panelManager.SetDebugText($"[SnapToGrid] origin: ({gridX},{gridY}), core:({partsData.coreX},{partsData.coreY}), grid({coreGridX},{coreGridY})");
        
        // コアのグリッド座標をワールド座標に変換
        Vector3 worldPos = GridToWorld(coreGridX, coreGridY);
        transform.position = worldPos;
        rb.linearVelocity = Vector2.zero;
    }

    // グリッド座標→ワールド座標変換
    private Vector3 GridToWorld(int gridX, int gridY)
    {
        Debug.Log("コア位置X:" +  gridX +" Y:" + gridY);
        return panelManager.GetPanelPos(gridX, gridY);
    }

    // 元の位置に戻す
    private void ReturnToOriginalPosition()
    {
        transform.position = originalPosition;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 2f;
    }

    // パーツデータの設定
    public void SetPartsData(PartsBase data)
    {
        partsData = data;
    }

    public PartsBase GetPartsData()
    {
        return partsData;
    }

    public PartsID GetPartsID()
    {
        return partsId;
    }

    public PartsType GetPartsType()
    {
        return partsType;
    }

    // プレビューを更新
    private void UpdatePreview()
    {
        if (partsData == null || panelManager == null) return;

        // 現在のカーソル位置を取得
        if (panelManager.GetCurrentGridPos(out int gridX, out int gridY))
        {
            // 右下を掴んでいるので、左上の原点座標を計算
            int originX = gridX - (partsData.width - 1);
            int originY = gridY - (partsData.height - 1);

            // プレビューを設定
            panelManager.SetPreview(partsData, originX, originY, partsType);
        }
        else
        {
            // パネル外ならプレビューをクリア
            panelManager.ClearPreview();
        }
    }
}
