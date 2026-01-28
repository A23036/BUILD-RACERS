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
        SceneManager.LoadScene("tutorial");
    }

    public void PushSingleButton()
    {
        SceneManager.LoadScene("single");
    }

    public void PushMultiButton()
    {
        SceneManager.LoadScene("Robby");
    }
    public void PushOptionButton()
    {
        SceneManager.LoadScene("option");
    }
    public void PushShopButton()
    {
        SceneManager.LoadScene("shop");
    }
    public void PushBackButton()
    {
        SceneManager.LoadScene("tittle");
    }
}
