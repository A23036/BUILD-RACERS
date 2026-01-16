using UnityEngine;
using UnityEngine.SceneManagement;

public class menu : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "tittle";
    }

    // Update is called once per frame
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
