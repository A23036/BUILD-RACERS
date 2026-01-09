using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("遷移先シーン名")]
    [SerializeField] private string sceneName;

    [Header("Visual")]
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    Vector3 defaultScale;
    bool isPressed;

    void Awake()
    {
        defaultScale = visual.localScale;
    }

    // マウス・タッチ共通
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        PlayHover();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPressed)
        {
            LoadScene();
        }
        isPressed = false;
        StopHover();
    }

    void PlayHover()
    {
        animator.Play("Hover", 0, 0f);
        spriteRenderer.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        visual.localScale = defaultScale * 0.95f;
    }

    void StopHover()
    {
        animator.Play("Idle", 0, 0f);
        spriteRenderer.color = Color.white;
        visual.localScale = defaultScale;
    }

    void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        SceneManager.LoadScene(sceneName);
    }
}
