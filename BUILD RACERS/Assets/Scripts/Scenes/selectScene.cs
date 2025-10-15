using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class selectScene : baseScene
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "menu";

        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();
        input.text = PlayerPrefs.GetString("PlayerName");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PushStartButton()
    {
        //名前が一文字以上でシーン遷移
        if(PlayerPrefs.GetString("PlayerName").Length > 0)
        {
            SceneManager.LoadScene("gamePlay");
        }
    }

    public void InputText()
    {
        GameObject inputField = GameObject.Find("InputFieldLegacy");
        InputField input = inputField.GetComponent<InputField>();

        //プレイヤーの名前を保存
        PlayerPrefs.SetString("PlayerName", input.text);
    }
}
