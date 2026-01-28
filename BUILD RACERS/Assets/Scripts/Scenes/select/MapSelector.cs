using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class MapSelector : MonoBehaviour
{
    private string playSceneName;

    private string[] sceneNames ={ 
        "Map1",
        "Map2",
        "Map3"
    };


    //Dropdownを格納する変数
    [SerializeField] private TMP_Dropdown dropdown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playSceneName = "Map1";
    }

    // Update is called once per frame
    void Update()
    {
        int idx = Mathf.Clamp(dropdown.value, 0, sceneNames.Length - 1);
        playSceneName = sceneNames[idx];

        SetSceneNameOnProp();
    }

    //ドロップダウンで選択したシーン名を返す
    public void SetSceneNameOnProp()
    {
        //セレクトシーンなら
        if(SceneManager.GetActiveScene().name == "select")
        {
            //シーンマネージャーからカスタムプロパティに登録
            var sceneManager = GameObject.Find("SceneManager").GetComponent<selectScene>();
            sceneManager.SetPlaySceneNameOnProp(playSceneName);
        }
    }

    public string GetSceneName()
    {
        return playSceneName;
    }
}
