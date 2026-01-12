using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
    [SerializeField] private SpriteRenderer panelSprite;
    [SerializeField] private SpriteRenderer buildSprite;
    [SerializeField] private SpriteRenderer flagSprite;
    [SerializeField] private SpriteRenderer RacersSprite;
    [SerializeField] private SpriteRenderer RogoSprite;
    [SerializeField] private SpriteRenderer tittleRogo;

    [SerializeField] private Animator panelAnimator;
    [SerializeField] private Animator buildAnimator;
    [SerializeField] private Animator flagAnimator;
    [SerializeField] private Animator racersAnimator;
    [SerializeField] private Animator rogoAnimator;

    AnimationState state;
    Animator currentAnime;

    void Start()
    {
        preSceneName = "";

        buildSprite.enabled = false;
        flagSprite.enabled = false;
        RacersSprite.enabled = false;
        RogoSprite.enabled = false;
        tittleRogo.enabled = false;

        state = AnimationState.Panel;

        StartCoroutine(AnimationSequence());
    }

    void Update()
    {
        // 新InputSystem：マウス左クリックまたはタッチ開始でメニュー画面に遷移
        bool clicked = false;

        if (Mouse.current != null)
        {
            clicked |= Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (Touchscreen.current != null)
        {
            clicked |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        if (clicked)
        {
            if (state != AnimationState.Finish) // アニメーション中にクリックでスキップ
            {
                state = AnimationState.Finish;
            }
            else
            {
                SceneManager.LoadScene("menu");
            }
        }

        base.Update();
    }

    private System.Collections.IEnumerator AnimationSequence()
    {
        yield return Play(panelSprite, panelAnimator, AnimationState.Panel);
        yield return Play(buildSprite, buildAnimator, AnimationState.Build);
        yield return Play(flagSprite, flagAnimator, AnimationState.Flag);
        yield return Play(RacersSprite, racersAnimator, AnimationState.Racers);
        yield return Play(RogoSprite, rogoAnimator, AnimationState.Rogo);

        state = AnimationState.Finish;
        EnterFinishState();
    }

    private System.Collections.IEnumerator Play(SpriteRenderer sprite,
    Animator animator,AnimationState playState)
    {
        // スキップされたら即終了
        if (state == AnimationState.Finish)
        {
            EnterFinishState();
            StopAllCoroutines();
            yield break;
        }

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
        // 既存スプライトをすべて非表示
        panelSprite.enabled = false;
        buildSprite.enabled = false;
        flagSprite.enabled = false;
        RacersSprite.enabled = false;
        RogoSprite.enabled = false;

        // タイトルロゴ表示
        tittleRogo.enabled = true;
    }
}
