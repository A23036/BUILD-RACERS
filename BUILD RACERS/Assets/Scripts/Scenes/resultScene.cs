using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class resultScene : baseScene
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        preSceneName = "gamePlay";

        //ƒIƒ“ƒ‰ƒCƒ“‚¾‚Á‚½‚Æ‚«‚ÍÚ‘±‚ğØ‚é
        if(PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PushMenuButton()
    {
        SceneManager.LoadScene("menu");
    }
}
