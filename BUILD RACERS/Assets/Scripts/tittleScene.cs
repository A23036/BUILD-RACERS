using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class tittleManager : MonoBehaviour
{
    [SerializeField]
    private string PlaySceneName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
            SceneManager.LoadScene(PlaySceneName);
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
