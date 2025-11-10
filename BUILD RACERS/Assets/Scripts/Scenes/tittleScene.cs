using UnityEngine;
using UnityEngine.SceneManagement;

public class tittleScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "";
    }

    // Update is called once per frame
    void Update()
    {
        //クリックでメニュー画面に遷移
        if (Input.GetMouseButton(0))
        {
            SceneManager.LoadScene("menu");
        }

        base.Update();
    }
}
