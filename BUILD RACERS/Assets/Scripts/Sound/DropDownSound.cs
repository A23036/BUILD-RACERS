using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropDownSound : MonoBehaviour, IPointerDownHandler
{
    [Header("SE")]
    [SerializeField] private AudioClip openSe;
    [SerializeField] private AudioClip closeSe;

    private bool isOpen = false;
    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null) Debug.LogError("DropDownSound: TMP_Dropdown component not found!");

        // 値変更（選択確定）時に鳴らす
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SoundManager.Instance?.PlaySE(openSe);
    }

    private void OnValueChanged(int _)
    {
        // 値が変わった（= 選択が確定した）時に鳴らす
        SoundManager.Instance?.PlaySE(closeSe);
    }
}
