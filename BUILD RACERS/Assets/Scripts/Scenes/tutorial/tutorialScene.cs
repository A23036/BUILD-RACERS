using UnityEngine;
using UnityEngine.SceneManagement;

public class tutorialScene : baseScene
{
    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private Fade fade2;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    
    [Header("Sound")]
    [SerializeField] private AudioClip tutorialBgm; // チュートリアルBGM
    [SerializeField] private AudioClip clickSe; // クリック音
    [SerializeField] private AudioClip startSe; // スタート音
    [SerializeField] private AudioClip backSe; // 戻る音
    private bool isClicked = false;

    void Start()
    {
        preSceneName = "menu";

        SoundManager.Instance.PlayBGM(tutorialBgm, 0f);
        if (fade != null)
        {
            fade.SetStartRange();
            fade.FadeOut(fadeOutDuration);
        }
    }

    void Update()
    {
        base.Update();
    }

    public void PushDriverTestButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(startSe);
            isClicked = true;
        }
        SoundManager.Instance.StopBGM(0.8f);

        // ガイド表示を有効化
        PlayerPrefs.SetInt(OptionPrefs.GUIDE_ENABLED, 1);
        fade2.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("driver tutorial");
        });
    }

    public void PushEngineerTestButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(startSe);
            isClicked = true;
        }
        SoundManager.Instance.StopBGM(0.8f);

        // ガイド表示を有効化
        PlayerPrefs.SetInt(OptionPrefs.GUIDE_ENABLED, 1);
        fade2.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("engineer tutorial");
        });
    }

    public void PushBackButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(backSe);
            isClicked = true;
        }
        SoundManager.Instance.StopBGM(0.8f);

        fade2.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("menu");
        });
    }
}
