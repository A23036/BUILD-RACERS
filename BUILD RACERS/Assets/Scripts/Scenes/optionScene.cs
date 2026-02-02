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

    private const string KEY_BGM = "Volume_BGM_20";
    private const string KEY_SE = "Volume_SE_20";
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
        // スライダー未設定なら何もしない（null事故回避）
        if (bgmSlider == null || seSlider == null)
        {
            Debug.LogWarning("OptionScene: sliders are not assigned.");
            return;
        }

        // Slider設定（念のためコード側でも矯正）
        bgmSlider.minValue = 0;
        bgmSlider.maxValue = STEP_MAX;
        bgmSlider.wholeNumbers = true;

        seSlider.minValue = 0;
        seSlider.maxValue = STEP_MAX;
        seSlider.wholeNumbers = true;

        // 保存値を読み込む（無ければ初期値20）
        int bgmStep = PlayerPrefs.GetInt(KEY_BGM, STEP_MAX);
        int seStep = PlayerPrefs.GetInt(KEY_SE, STEP_MAX);

        // 0〜20に丸め
        bgmStep = Mathf.Clamp(bgmStep, 0, STEP_MAX);
        seStep = Mathf.Clamp(seStep, 0, STEP_MAX);

        // 値を入れる前にリスナーを外す（初期化時の二重呼び出し防止）
        bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
        seSlider.onValueChanged.RemoveListener(OnSeSliderChanged);

        bgmSlider.value = bgmStep;
        seSlider.value = seStep;

        // 反映（起動時にも適用）
        ApplyVolumes(bgmStep, seStep);

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

        // 多重登録防止のため上書き
        hook.OnPointerUpAction = PlayAdjustSE;
    }

    private void PlayAdjustSE()
    {
        if (adjustSe == null) return;
        SoundManager.Instance?.PlaySE(adjustSe);
    }


    private void OnBgmSliderChanged(float value)
    {
        int step = Mathf.RoundToInt(value);
        step = Mathf.Clamp(step, 0, STEP_MAX);

        PlayerPrefs.SetInt(KEY_BGM, step);
        PlayerPrefs.Save();

        // 0〜1に変換して反映
        SoundManager.Instance?.SetBGMVolume(StepTo01(step));
    }

    private void OnSeSliderChanged(float value)
    {
        int step = Mathf.RoundToInt(value);
        step = Mathf.Clamp(step, 0, STEP_MAX);

        PlayerPrefs.SetInt(KEY_SE, step);
        PlayerPrefs.Save();

        SoundManager.Instance?.SetSEVolume(StepTo01(step));
    }

    private void ApplyVolumes(int bgmStep, int seStep)
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.SetBGMVolume(StepTo01(bgmStep));
        SoundManager.Instance.SetSEVolume(StepTo01(seStep));
    }

    private float StepTo01(int step)
    {
        // 20段階 -> 0.0~1.0
        return step / (float)STEP_MAX;
    }

    public void PushBackButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(backSe);
            isClicked = true;
        }

        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("menu");
        });
    }
}
