using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class menu : baseScene
{
    [Header("Fade")]
    [SerializeField] private Fade fade;

    void Start()
    {
        preSceneName = "tittle";

        if (fade != null)
        {
            fade.SetStartRange();
            fade.FadeOut(0.8f);
        }
    }

    void Update()
    {
        base.Update();
    }

    public void PushTutorialButton()
    {
        fade.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("tutorial");
        });
    }

    public void PushSingleButton()
    {
        fade.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("single");
        });
    }

    public void PushMultiButton()
    {
        fade.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("Robby");
        });
    }

    public void PushOptionButton()
    {
        fade.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("option");
        });
    }
    public void PushShopButton()
    {
        fade.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("shop");
        });
    }
    public void PushBackButton()
    {
        fade.FadeIn(0.8f, () =>
        {
            SceneManager.LoadScene("tittle");
        });
    }
}
