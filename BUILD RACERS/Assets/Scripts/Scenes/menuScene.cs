using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class menu : baseScene
{
    [Header("Sound")]
    [SerializeField] private AudioClip menuBgm; // メニューBGM
    [SerializeField] private AudioClip clickSe; // クリック音
    [SerializeField] private AudioClip backSe; // 戻る音

    [Header("Fade")]
    [SerializeField] private Fade fade;
    [SerializeField] private Fade fadeToTutorial;
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    private bool isClicked = false;

    void Start()
    {
        preSceneName = "tittle";

        if (fade != null)
        {
            fade.SetStartRange();
            fade.FadeOut(fadeOutDuration);
        }

        SoundManager.Instance.PlayBGM(menuBgm, 0f);
    }

    void Update()
    {
        base.Update();
    }

    public void PushTutorialButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(clickSe);
            isClicked = true;
        }

        SoundManager.Instance.StopBGM(0.8f);
        fadeToTutorial.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("tutorial");
        });
    }

    public void PushSingleButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(clickSe);
            isClicked = true;
        }

        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("single");
        });
    }

    public void PushMultiButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(clickSe);
            isClicked = true;
        }

        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("Robby");
        });
    }

    public void PushOptionButton()
    {
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(clickSe);
            isClicked = true;
        }

        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("option");
        });
    }
    public void PushBackButton()
    {
        SoundManager.Instance.StopBGM(0.8f);
        if (!isClicked)
        {
            SoundManager.Instance.PlaySE(backSe);
            isClicked = true;
        }
        fade.FadeIn(fadeInDuration, () =>
        {
            SceneManager.LoadScene("tittle");
        });
    }
}
