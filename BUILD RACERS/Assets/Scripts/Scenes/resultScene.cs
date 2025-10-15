using UnityEngine;
using UnityEngine.SceneManagement;

public class resultScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "gamePlay";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PushMenuButton()
    {
        SceneManager.LoadScene("menu");
    }
}
