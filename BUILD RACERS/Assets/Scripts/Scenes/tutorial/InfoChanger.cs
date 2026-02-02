using UnityEngine;
using UnityEngine.UI;

public class InfoChanger : MonoBehaviour
{
    [SerializeField] private AudioClip clickSe; // クリック音
    [SerializeField] private AudioClip fileSe;  // ファイル音
    private bool isFirst = true;

    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject infoPanel;

        [Header("Button Sprites")]
        public Sprite normalSprite;
        public Sprite selectedSprite;
    }

    [SerializeField] private Tab[] tabs;

    private int currentIndex = -1;

    void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].button.onClick.AddListener(() => OnTabSelected(index));
        }

        // 初期選択（必要なら）
        OnTabSelected(0);
    }

    private void OnTabSelected(int index)
    {
        if (currentIndex == index) return;

        if (!isFirst)
        {
            PlayFileSound();
        }
        else
        {
            isFirst = false;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            bool isSelected = (i == index);

            // Info 表示切り替え
            tabs[i].infoPanel.SetActive(isSelected);

            // Button 画像切り替え
            UpdateButtonSprite(tabs[i], isSelected);
        }

        currentIndex = index;
    }

    private void UpdateButtonSprite(Tab tab, bool selected)
    {
        Image img = tab.button.GetComponent<Image>();
        if (img == null) return;

        img.sprite = selected ? tab.selectedSprite : tab.normalSprite;
    }

    public void PlayClickSound()
    {
        SoundManager.Instance.PlaySE(clickSe);
    }

    public void PlayFileSound()
    {
        SoundManager.Instance.PlaySE(fileSe);
    }
}
