using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI; // CanvasGroup用（念のため）

enum AnimationState
{
    Panel,
    Build,
    Flag,
    Racers,
    Rogo,
    Finish,
}

public class tittleScene : baseScene
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer panelSprite;
    [SerializeField] private SpriteRenderer buildSprite;
    [SerializeField] private SpriteRenderer flagSprite;
    [SerializeField] private SpriteRenderer RacersSprite;
    [SerializeField] private SpriteRenderer RogoSprite;
    [SerializeField] private SpriteRenderer tittleRogo;

    [Header("Animators")]
    [SerializeField] private Animator panelAnimator;
    [SerializeField] private Animator buildAnimator;
    [SerializeField] private Animator flagAnimator;
    [SerializeField] private Animator racersAnimator;
    [SerializeField] private Animator rogoAnimator;

    [Header("Press Start UI")]
    [SerializeField] private GameObject toStartImage; // PressStartオブジェクト（UI）
    [SerializeField] private float fadeDuration = 0.6f; // フェード片道の秒数
    [SerializeField] private float minAlpha = 0.15f;    // 最小アルファ
    [SerializeField] private float maxAlpha = 1.0f;     // 最大アルファ

    private CanvasGroup pressStartCanvasGroup;
    private Coroutine animationSequenceCoroutine;
    private Coroutine blinkCoroutine;

    private AnimationState state;
    private bool isFinishEntered = false;

    void Start()
    {
        preSceneName = "";

        buildSprite.enabled = false;
        flagSprite.enabled = false;
        RacersSprite.enabled = false;
        RogoSprite.enabled = false;
        tittleRogo.enabled = false;

        if (toStartImage != null)
        {
            toStartImage.SetActive(false);

            // フェード用の CanvasGroup を確保（無ければ付ける）
            pressStartCanvasGroup = toStartImage.GetComponent<CanvasGroup>();
            if (pressStartCanvasGroup == null)
                pressStartCanvasGroup = toStartImage.AddComponent<CanvasGroup>();

            pressStartCanvasGroup.alpha = 0f;
        }

        state = AnimationState.Panel;
        animationSequenceCoroutine = StartCoroutine(AnimationSequence());
    }

    void Update()
    {
        // 新InputSystem：マウス左クリックまたはタッチ開始でメニュー画面に遷移
        bool clicked = false;

        if (Mouse.current != null)
            clicked |= Mouse.current.leftButton.wasPressedThisFrame;

        if (Touchscreen.current != null)
            clicked |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (clicked)
        {
            if (state != AnimationState.Finish) // アニメーション中にクリックでスキップ
            {
                state = AnimationState.Finish;

                // メインのシーケンスだけ止める（StopAllCoroutinesは使わない）
                if (animationSequenceCoroutine != null)
                {
                    StopCoroutine(animationSequenceCoroutine);
                    animationSequenceCoroutine = null;
                }

                EnterFinishState();
            }
            else
            {
                // 点滅停止（任意）
                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                    blinkCoroutine = null;
                }

                SceneManager.LoadScene("menu");
            }
        }

        base.Update();
    }

    private IEnumerator AnimationSequence()
    {
        yield return Play(panelSprite, panelAnimator, AnimationState.Panel);
        yield return Play(buildSprite, buildAnimator, AnimationState.Build);
        yield return Play(flagSprite, flagAnimator, AnimationState.Flag);
        yield return Play(RacersSprite, racersAnimator, AnimationState.Racers);
        yield return Play(RogoSprite, rogoAnimator, AnimationState.Rogo);

        state = AnimationState.Finish;
        EnterFinishState();
    }

    private IEnumerator Play(SpriteRenderer sprite, Animator animator, AnimationState playState)
    {
        // スキップされたら即終了（ここでStopAllCoroutinesはしない）
        if (state == AnimationState.Finish)
            yield break;

        state = playState;

        sprite.enabled = true;
        animator.Play(0, 0, 0f);

        // Animator更新待ち
        yield return null;

        // 再生終了待ち
        while (true)
        {
            if (state == AnimationState.Finish)
                yield break;

            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.normalizedTime >= 1f && !animator.IsInTransition(0))
                break;

            yield return null;
        }
    }

    private void EnterFinishState()
    {
        if (isFinishEntered) return;
        isFinishEntered = true;

        // 既存スプライトをすべて非表示
        panelSprite.enabled = false;
        buildSprite.enabled = false;
        flagSprite.enabled = false;
        RacersSprite.enabled = false;
        RogoSprite.enabled = false;

        // タイトルロゴ表示
        tittleRogo.enabled = true;

        // PressStart 表示 + フェード点滅開始
        if (toStartImage != null)
        {
            toStartImage.SetActive(true);

            if (pressStartCanvasGroup == null)
            {
                pressStartCanvasGroup = toStartImage.GetComponent<CanvasGroup>();
                if (pressStartCanvasGroup == null)
                    pressStartCanvasGroup = toStartImage.AddComponent<CanvasGroup>();
            }

            // 初期アルファ
            pressStartCanvasGroup.alpha = maxAlpha;

            // すでに点滅中なら止める
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            blinkCoroutine = StartCoroutine(FadeBlinkPressStart());
        }
    }

    private IEnumerator FadeBlinkPressStart()
    {
        // Finish中だけループ
        while (state == AnimationState.Finish)
        {
            // max -> min
            yield return FadeAlpha(maxAlpha, minAlpha, fadeDuration);

            if (state != AnimationState.Finish) break;

            // min -> max
            yield return FadeAlpha(minAlpha, maxAlpha, fadeDuration);
        }

        // 状態が変わったら念のため表示に戻す
        if (pressStartCanvasGroup != null)
            pressStartCanvasGroup.alpha = maxAlpha;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (pressStartCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            pressStartCanvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        pressStartCanvasGroup.alpha = from;

        while (t < duration)
        {
            // 途中でFinishを抜けたら中断
            if (state != AnimationState.Finish)
                yield break;

            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            pressStartCanvasGroup.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        pressStartCanvasGroup.alpha = to;
    }
}
