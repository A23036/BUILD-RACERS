using UnityEngine;
using UnityEngine.UI;

public static class OptionPrefs
{
    public const string GUIDE_ENABLED = "GuideEnabled";
}

public class GuideFlag : MonoBehaviour
{
    [SerializeField] private Toggle guideToggle;
    [SerializeField] private AudioClip toggleSe;

    void Start()
    {
        // デフォルトON（キーが無ければ1）
        bool guideOn = PlayerPrefs.GetInt(OptionPrefs.GUIDE_ENABLED, 1) == 1;

        // Toggleに反映（イベント発火させない）
        guideToggle.SetIsOnWithoutNotify(guideOn);

        // イベント登録
        guideToggle.onValueChanged.AddListener(OnGuideToggleChanged);
    }

    private void OnGuideToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(OptionPrefs.GUIDE_ENABLED, isOn ? 1 : 0);
        PlayerPrefs.Save();

        SoundManager.Instance.PlaySE(toggleSe);

        Debug.Log($"Guide Enabled : {isOn}");
    }
}
