using UnityEngine;
using UnityEngine.SceneManagement;

public class robbyScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "multi"; 
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }

    public void PushSelectButton()
    {
        SceneManager.LoadScene("select");
    }
}
