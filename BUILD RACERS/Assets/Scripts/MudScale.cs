using UnityEngine;
using System.Collections;

public class MudScale : MonoBehaviour
{
    [Header("アニメーション設定")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float overshootScale = 1.2f;
    [SerializeField] private float finalScale = 1.0f;

    [Header("開始設定")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float startDelay = 0f;

    private void Start()
    {
        if (playOnStart)
        {
            PlayScaleAnimation();
        }
    }

    /// <summary>
    /// スケールアニメーションを再生
    /// </summary>
    public void PlayScaleAnimation()
    {
        StartCoroutine(ScaleAnimation(startDelay));
    }

    /// <summary>
    /// スケールアニメーションのコルーチン
    /// </summary>
    private IEnumerator ScaleAnimation(float delay)
    {
        // 初期スケールを0に
        transform.localScale = Vector3.zero;

        // 遅延がある場合は待機
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsed = 0f;
        float phase1Duration = animationDuration * 0.66f; // 前半66%
        float phase2Duration = animationDuration * 0.34f; // 後半34%

        // === フェーズ1: 0 → オーバーシュート (1.2) ===
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;

            // EaseOutBackで弾むような動き
            float scale = EaseOutBack(t) * overshootScale;
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // === フェーズ2: オーバーシュート → 最終サイズ (1.0) ===
        elapsed = 0f;
        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase2Duration;

            // スムーズに元のサイズへ
            float scale = Mathf.Lerp(overshootScale, finalScale, t);
            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // 最終的に正確なサイズにセット
        transform.localScale = Vector3.one * finalScale;
    }

    /// <summary>
    /// EaseOutBackイージング関数
    /// 少しオーバーシュートして戻る動き
    /// </summary>
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}