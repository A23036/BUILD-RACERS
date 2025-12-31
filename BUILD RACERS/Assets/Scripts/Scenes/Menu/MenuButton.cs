using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    [Header("遷移先シーン名")]
    [SerializeField] private string sceneName;

    [Header("Visual (子オブジェクト)")]
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    Vector3 defaultScale;
    bool isHover;

    void Awake()
    {
        defaultScale = visual.localScale;
    }

    // ===== マウス用 =====
    void OnMouseEnter()
    {
        PlayHover();
    }

    void OnMouseExit()
    {
        StopHover();
    }

    void OnMouseDown()
    {
        // Hover 中のみクリック有効
        if (isHover)
        {
            LoadScene();
        }
    }

    // ===== タッチ用 =====
    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        Vector2 worldPos =
            Camera.main.ScreenToWorldPoint(touch.position);

        RaycastHit2D hit =
            Physics2D.Raycast(worldPos, Vector2.zero);

        bool isTouchingThis =
            hit.collider != null &&
            hit.collider.gameObject == gameObject;

        if (!isTouchingThis)
        {
            StopHover();
            return;
        }

        // ホールド中
        if (touch.phase == TouchPhase.Began ||
            touch.phase == TouchPhase.Moved ||
            touch.phase == TouchPhase.Stationary)
        {
            PlayHover();
        }

        // 離した瞬間に決定
        if (touch.phase == TouchPhase.Ended)
        {
            LoadScene();
            StopHover();
        }

        if (touch.phase == TouchPhase.Canceled)
        {
            StopHover();
        }
    }

    void PlayHover()
    {
        if (isHover) return;

        animator.Play("Hover");
        isHover = true;

        spriteRenderer.color = Color.white * 0.95f;
        visual.localScale = defaultScale * 0.95f;
    }

    void StopHover()
    {
        if (!isHover) return;

        animator.Play("Idle");
        isHover = false;

        spriteRenderer.color = Color.white;
        visual.localScale = defaultScale;
    }

    void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneManager.LoadScene(sceneName);
    }
}
