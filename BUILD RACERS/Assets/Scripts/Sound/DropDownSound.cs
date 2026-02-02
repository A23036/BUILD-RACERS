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
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isOpen) return;

        isOpen = true;
        Debug.Log("DropDownSound: Dropdown opened.");
        SoundManager.Instance?.PlaySE(openSe);
        StartCoroutine(WatchClose());
    }

    private IEnumerator WatchClose()
    {
        while (true)
        {
            if (GameObject.Find("Dropdown List") == null)
            {
                isOpen = false;
                Debug.Log("DropDownSound: Dropdown closed.");
                SoundManager.Instance?.PlaySE(closeSe);
                yield break;
            }
            yield return null;
        }
    }
}
