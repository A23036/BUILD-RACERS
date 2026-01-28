using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class map1 : playScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== MAP1 START ===");
        
        preSceneName = "select";

        GenerateKarts();

        //ロード完了後にメッセージ処理を再開
        if(PhotonNetwork.IsConnected) PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void Awake()
    {
        //シングルプレイなら処理なし
        if (!PhotonNetwork.IsConnected) return;

        base.Awake();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }
}
