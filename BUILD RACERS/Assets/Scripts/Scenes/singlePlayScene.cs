using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class singlePlayScene : baseScene
{
    [SerializeField] private GameObject DriverUI;
    [SerializeField] private GameObject EngineerUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "single";
    }

    private void Awake()
    {
        //プレイヤーの生成
        if (PlayerPrefs.GetInt("driverNum") != -1)
        {
            //ドライバーの生成
            var player = Instantiate(Resources.Load("player"));
            player.GetComponent<CarController>().SetCamera();

            //UIの有効化
            DriverUI.SetActive(true);
            EngineerUI.SetActive(false);
        }
        else if (PlayerPrefs.GetInt("engineerNum") != -1)
        {
            //エンジニアの生成
            Instantiate(Resources.Load("Engineer"));

            //相方ドライバーの生成（CPU）
            var cpu = Instantiate(Resources.Load("Player"));
            var cpuCc = cpu.GetComponent<CarController>();
            var wpContainer = FindObjectOfType<WaypointContainer>();
            cpuCc.SetAI<AIDriver>(wpContainer);

            //UIの有効化
            DriverUI.SetActive(false);
            EngineerUI.SetActive(true);
        }
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
