using TMPro;
using UnityEngine;
using UnityEngine.Windows;

public class LapSetter : MonoBehaviour
{
    private int lapCnt = 0;
    [SerializeField] private int minLapCnt = 0;
    [SerializeField] private int maxLapCnt = 9;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InputText()
    {
        var lapInput = GameObject.Find("lapInputField").GetComponent<TMP_InputField>();

        //êîílïœä∑
        if (!int.TryParse(lapInput.text, out int lap))
        {
            lapInput.text = "";
            return;
        }

        lap = Mathf.Clamp(lap, minLapCnt, maxLapCnt);
        lapCnt = lap;
        lapInput.text = lap.ToString();
    }

    public int GetLapCnt()
    {
        return lapCnt;
    }
}
