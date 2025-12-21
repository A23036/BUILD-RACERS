using TMPro;
using UnityEngine;

public class monitorTargetSwitcher : MonoBehaviour
{
    [SerializeField] private int addIdx;
    [SerializeField] private GameObject monitorCamera;

    //UI
    

    private CameraController cc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        cc = monitorCamera.GetComponent<CameraController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //’Ç]‘ÎÛ‚ÌØ‚è‘Ö‚¦
    public void PushSwitcher()
    {
        if(cc != null)
        {
            //ŠÏí‘ÎÛ‚ÌØ‚è‘Ö‚¦
            cc.SetNextTarget(addIdx);
        }
        else
        {
            cc = monitorCamera.GetComponent<CameraController>();
        }
    }
}
