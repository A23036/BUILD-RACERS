using UnityEngine;

public class BackImageScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 200f; // UI‚È‚Ì‚Å px/sec
    private float width;

    private RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        width = rect.rect.width;
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.left * scrollSpeed * Time.deltaTime;

        if (rect.anchoredPosition.x <= -width)
        {
            rect.anchoredPosition += new Vector2(width * 2f, 0f);
        }
    }
}
