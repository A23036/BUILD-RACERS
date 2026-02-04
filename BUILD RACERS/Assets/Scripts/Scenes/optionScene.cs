using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class optionScene : baseScene
{
    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Audio Note")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private AudioClip adjustSe; // 調整音
    [SerializeField] private AudioClip backSe;
    private bool isClicked = false;

    private const int STEP_MAX = 20;

    void Start()
    {
        preSceneName = "menu";

        if (fade != null)
        {
            fade.SetStartRange();
            fade.FadeOut(fadeOutDuration);
        }

        SetupSliders();
    }

    void Update()
    {
        base.Update();
    }

    private void SetupSliders()
    {
        if (bgmSlider == null || seSlider == null)
        {
            Debug.LogWarning("OptionScene: sliders are not assigned.");
            return;
        }

        // Slider設定
        bgmSlider.minValue = 0;
        bgmSlider.maxValue = STEP_MAX;
        bgmSlider.wholeNumbers = true;

        seSlider.minValue = 0;
        seSlider.maxValue = STEP_MAX;
        seSlider.wholeNumbers = true;

        // SoundManagerが既にロードしている想定なので、まずそこから取得
        int bgmStep = STEP_MAX;
        int seStep = STEP_MAX;

        if (SoundManager.Instance != null)
        {
            bgmStep = Mathf.RoundToInt(SoundManager.Instance.GetBGMVolume01() * STEP_MAX);
            seStep = Mathf.RoundToInt(SoundManager.Instance.GetSEVolume01() * STEP_MAX);
        }

        bgmStep = Mathf.Clamp(bgmStep, 0, STEP_MAX);
        seStep = Mathf.Clamp(seStep, 0, STEP_MAX);

        // 初期化時の二重呼び出し防止
        bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
        seSlider.onValueChanged.RemoveListener(OnSeSliderChanged);

        bgmSlider.value = bgmStep;
        seSlider.value = seStep;

        // 念のため反映（※SoundManager側がPrefs→Mixer反映済みなら不要。残しても害は少ない）
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBgmStep(bgmStep, save: false);
            SoundManager.Instance.SetSeStep(seStep, save: false);
        }

        // リスナー登録
        bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        seSlider.onValueChanged.AddListener(OnSeSliderChanged);

        HookPointerUpSE(seSlider);
    }

    private void HookPointerUpSE(Slider slider)
    {
        if (slider == null) return;

        var hook = slider.GetComponent<SliderPointUpSE>();
        if (hook == null) hook = slider.gameObject.AddComponent<SliderPointUpSE>();

        hook.OnPointerUpAction = PlayAdjustSE;
    }

    private void PlayAdjustSE()
    {
        if (adjustSe == null) return;
        SoundManager.Instance?.PlaySE(adjustSe);
    }

    private void OnBgmSliderChanged(float value)
    {
        int step = Mathf.Clamp(Mathf.RoundToInt(value), 0, STEP_MAX);
        SoundManager.Instance?.SetBgmStep(step, save: true); // 保存もSoundManager側
    }

    private void OnSeSliderChanged(float value)
    {
        int step = Mathf.Clamp(Mathf.RoundToInt(value), 0, STEP_MAX);
        SoundManager.Instance?.SetSeStep(step, save: true); // 保存もSoundManager側
    }

    public void PushBackButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance?.PlaySE(backSe);
            isClicked = true;
        }

        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("menu");
        });
    }
}
