using UnityEngine;
using UnityEngine.SceneManagement;

public class tutorialScene : baseScene
{
    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private Fade fade2;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    void Start()
    {
        preSceneName = "menu";

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
        // ガイド表示を有効化
        PlayerPrefs.SetInt(OptionPrefs.GUIDE_ENABLED, 1);
        fade2.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("driver tutorial");
        });
    }

    public void PushEngineerTestButton()
    {
        // ガイド表示を有効化
        PlayerPrefs.SetInt(OptionPrefs.GUIDE_ENABLED, 1);
        fade2.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("engineer tutorial");
        });
    }

    public void PushBackButton()
    {
        fade2.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("menu");
        });
    }
}
