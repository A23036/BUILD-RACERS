using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
        // 新InputSystem：マウス左クリックまたはタッチ開始でメニュー画面に遷移
        bool clicked = false;

        if (Mouse.current != null)
        {
            clicked |= Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (Touchscreen.current != null)
        {
            clicked |= Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        if (clicked)
        {
            SceneManager.LoadScene("menu");
        }

        base.Update();
    }
}
