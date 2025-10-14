using UnityEngine;
using UnityEngine.SceneManagement;

//シーンの親クラス

public class baseScene : MonoBehaviour
{
    /// <summary>
    /// 前のシーンの名前
    /// </summary>
    protected string preSceneName;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PushBackButton()
    {
        if(preSceneName != null) SceneManager.LoadScene(preSceneName);
    }
}