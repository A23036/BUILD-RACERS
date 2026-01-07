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

    //ドライバーBOTの生成数
    [SerializeField] private int botsNum;

    Engineer engineer;
    CarController carController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "single";

        if(PlayerPrefs.GetInt("engineerNum") != -1)
        {
            //エンジニアのカメラ追従設定
            engineer.SetCamera();
        }

        //BOTドライバーの生成
        GenerateBotDrivers();
    }

    private void Awake()
    {
         carController = null;
         engineer = null;

        //プレイヤーの生成
        if (PlayerPrefs.GetInt("driverNum") != -1)
        {
            //ドライバーの生成
            var player = Instantiate(Resources.Load("player"));
            player.GetComponent<CarController>().SetCamera();
            carController = player.GetComponent<CarController>();

            //UIの有効化
            DriverUI.SetActive(true);
            EngineerUI.SetActive(false);
        }
        else if (PlayerPrefs.GetInt("engineerNum") != -1)
        {
            //相方ドライバーの生成（CPU）
            var cpu = Instantiate(Resources.Load("Player"));
            carController = cpu.GetComponent<CarController>();
            //carController.SetCamera();
            carController.SetName(PlayerPrefs.GetString("PlayerName"));
            var cpuCc = cpu.GetComponent<CarController>();
            var wpContainer = FindObjectOfType<WaypointContainer>();
            cpuCc.SetAI<AIDriver>(wpContainer);

            //エンジニアの生成
            var player = Instantiate(Resources.Load("Engineer"));
            engineer = player.GetComponent<Engineer>();

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

    public void GenerateBotDrivers()
    {
        var wpContainer = FindObjectOfType<WaypointContainer>();
        for (int i = 0; i < botsNum; i++)
        {
            var bot = Instantiate(Resources.Load("Player"));
            var botCc = bot.GetComponent<CarController>();
            botCc.SetAI<AIDriver>(wpContainer);
            //0埋め2桁で名前を設定
            botCc.SetName("CPU_" + (i + 1).ToString("00"));
        }
    }
}
