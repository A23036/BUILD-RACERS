using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class singlePlayScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "single";
    }

    private void Awake()
    {
        //ÉvÉåÉCÉÑÅ[ÇÃê∂ê¨
        if (PlayerPrefs.GetInt("driverNum") != -1)
        {
            var player = Instantiate(Resources.Load("player"));
            player.GetComponent<CarController>().SetCamera();
        }
        else if (PlayerPrefs.GetInt("engineerNum") != -1) Instantiate(Resources.Load("Engineer"));
        else Debug.Log("not select");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            ToResult();
        }

        base.Update();
    }

    public void ToResult()
    {
        SceneManager.LoadScene("result");
    }
}
