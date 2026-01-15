using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float fadeDuration = 0.12f;

    public PartsID? ItemId { get; private set; }

    private Coroutine animationRoutine;
    private RectTransform itemRectTransform;
    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        itemRectTransform = itemImage.rectTransform;
        baseScale = itemRectTransform.localScale;
        baseColor = itemImage.color;
    }

    public void SetItem(PartsID id, Sprite sprite)
    {
        ItemId = id;
        itemImage.sprite = sprite;
        itemImage.enabled = true;
        itemImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        itemRectTransform.localScale = baseScale * 0.9f;
        StartAnimation(AnimateIn());
        Debug.Log($"[UI] SetItem sprite = {sprite}");
    }

    public void Clear()
    {
        ItemId = null;
        StopAnimation();
        itemImage.sprite = null;
        itemImage.enabled = false;
        itemRectTransform.localScale = baseScale;
        itemImage.color = baseColor;
    }

    private void StartAnimation(IEnumerator routine)
    {
        StopAnimation();
        animationRoutine = StartCoroutine(routine);
    }

    private void StopAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private IEnumerator AnimateIn()
    {
        float elapsed = 0f;
        Vector3 startScale = baseScale * 0.9f;
        Vector3 peakScale = baseScale * popScale;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            itemRectTransform.localScale = Vector3.Lerp(startScale, peakScale, eased);
            itemImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            itemRectTransform.localScale = Vector3.Lerp(peakScale, baseScale, eased);
            itemImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            yield return null;
        }

        itemRectTransform.localScale = baseScale;
        itemImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        animationRoutine = null;
    }
}