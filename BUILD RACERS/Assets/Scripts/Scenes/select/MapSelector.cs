using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelector : MonoBehaviour
{
    private string playSceneName;

    //Dropdownを格納する変数
    [SerializeField] private TMP_Dropdown dropdown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playSceneName = "gamePlay";
    }

    // Update is called once per frame
    void Update()
    {
        if (dropdown.value == 0)
        {
            playSceneName = "gamePlay";
        }
        else if (dropdown.value == 1)
        {
            playSceneName = "Map2";
        }
        else if (dropdown.value == 2)
        {
            playSceneName = "Map3";
        }

        SetSceneNameOnProp();
    }

    //ドロップダウンで選択したシーン名を返す
    public void SetSceneNameOnProp()
    {
        //シーンマネージャーからカスタムプロパティに登録
        var sceneManager = GameObject.Find("SceneManager").GetComponent<selectScene>();
        sceneManager.SetPlaySceneNameOnProp(playSceneName);
    }
}
