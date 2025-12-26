using TMPro;
using UnityEngine;

public class monitorTargetSwitcher : MonoBehaviour
{
    [SerializeField] private int addIdx;
    [SerializeField] private GameObject monitorCamera;

    //UI
    

    private MonitorCameraController mcc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    private void Awake()
    {
        mcc = monitorCamera.GetComponent<MonitorCameraController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //’Ç]‘ÎÛ‚ÌØ‚è‘Ö‚¦
    public void PushSwitcher()
    {
        if(mcc != null)
        {
            //ŠÏí‘ÎÛ‚ÌØ‚è‘Ö‚¦
            mcc.SetNextTarget(addIdx);
        }
        else
        {
            mcc = monitorCamera.GetComponent<MonitorCameraController>();
        }
    }
}
