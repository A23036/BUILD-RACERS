using Unity.VisualScripting;
using UnityEngine;

public class driverTutorial : baseScene
{
    private CarController carController;

    private void Awake()
    {
        //ドライバーの生成
        var player = Instantiate(Resources.Load("tutorial player"), new Vector3(0, 0, -5), Quaternion.identity);
        player.GetComponent<CarController>().SetCamera();
        carController = player.GetComponent<CarController>();
        carController.isMine = true;
        carController.SetIsTutorial();

        // 状態の初期化
        PlayerPrefs.SetInt("driverNum", 1);
        PlayerPrefs.SetInt("engineerNum", -1);
        PlayerPrefs.SetInt("isMonitor", 0);

        // bot生成
        GenerateBotDrivers();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "tutorial";
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }

    public void GenerateBotDrivers()
    {
        var wpContainer = FindObjectOfType<WaypointContainer>();
        
        var bot = Instantiate(Resources.Load("Player"), new Vector3(-80f, 0, -5f), Quaternion.identity);
        var botCc = bot.GetComponent<CarController>();
        botCc.SetAI<AIDriver>(wpContainer);
        botCc.SetName("CPU");
        botCc.SetIsTutorial();
    }
}
