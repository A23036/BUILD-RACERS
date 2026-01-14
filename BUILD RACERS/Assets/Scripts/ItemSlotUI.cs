using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;

    public PartsID? ItemId { get; private set; }

    public void SetItem(PartsID id, Sprite sprite)
    {
        ItemId = id;
        itemImage.sprite = sprite;
        itemImage.enabled = true;
        Debug.Log($"[UI] SetItem sprite = {sprite}");
    }

    public void Clear()
    {
        ItemId = null;
        itemImage.sprite = null;
        itemImage.enabled = false;
    }
}
