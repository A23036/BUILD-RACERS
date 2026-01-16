using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PassiveSlotUI : MonoBehaviour
{
    [SerializeField] private Image passiveImage;
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float fadeDuration = 0.12f;

    public PartsID? ItemId { get; private set; }

    private Coroutine animationRoutine;
    private RectTransform imageRectTransform;
    private Vector3 baseScale;
    private Color baseColor;

    private void Awake()
    {
        imageRectTransform = passiveImage.rectTransform;
        baseScale = imageRectTransform.localScale;
        baseColor = passiveImage.color;
    }

    public void SetItem(PartsID id, Sprite sprite)
    {
        ItemId = id;
        passiveImage.sprite = sprite;
        passiveImage.enabled = true;
        passiveImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        imageRectTransform.localScale = baseScale * 0.9f;
        StartAnimation(AnimateIn());
    }

    public void Clear()
    {
        ItemId = null;
        StopAnimation();
        passiveImage.sprite = null;
        passiveImage.enabled = false;
        imageRectTransform.localScale = baseScale;
        passiveImage.color = baseColor;
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
            imageRectTransform.localScale = Vector3.Lerp(startScale, peakScale, eased);
            passiveImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            imageRectTransform.localScale = Vector3.Lerp(peakScale, baseScale, eased);
            passiveImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
            yield return null;
        }

        imageRectTransform.localScale = baseScale;
        passiveImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        animationRoutine = null;
    }
}
