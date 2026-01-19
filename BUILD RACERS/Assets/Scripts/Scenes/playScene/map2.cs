using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class map2 : playScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== MAP2 START ===");
        
        preSceneName = "select";

        GenerateKarts();

        //ロード完了後にメッセージ処理を再開
        PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void Awake()
    {
        base.Awake();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }
}
