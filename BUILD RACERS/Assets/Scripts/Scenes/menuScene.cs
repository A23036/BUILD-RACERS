using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class menu : baseScene
{
    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private Fade fadeToTutorial;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    void Start()
    {
        preSceneName = "tittle";

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

    public void PushTutorialButton()
    {
        fadeToTutorial.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("tutorial");
        });
    }

    public void PushSingleButton()
    {
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("single");
        });
    }

    public void PushMultiButton()
    {
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("Robby");
        });
    }

    public void PushOptionButton()
    {
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("option");
        });
    }
    public void PushShopButton()
    {
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("shop");
        });
    }
    public void PushBackButton()
    {
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("tittle");
        });
    }
}
