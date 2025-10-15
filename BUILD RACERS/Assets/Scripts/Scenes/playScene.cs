using UnityEngine;
using UnityEngine.SceneManagement;

public class playScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "select";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            ToResult();
        }
    }

    public void ToResult()
    {
        SceneManager.LoadScene("result");
    }
}
