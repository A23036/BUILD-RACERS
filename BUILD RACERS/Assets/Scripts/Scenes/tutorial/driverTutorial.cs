using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;

public class driverTutorial : baseScene
{
    private CarController carController;

    private void Awake()
    {
        //ドライバーの生成
        var player = Instantiate(Resources.Load("player"), new Vector3(0, 0, 0), Quaternion.identity);
        player.GetComponent<CarController>().SetCamera();
        carController = player.GetComponent<CarController>();
        carController.isMine = true;
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
}
