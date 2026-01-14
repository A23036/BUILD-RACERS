using System.Collections.Generic;
using UnityEngine;

public class ItemUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private ItemSlotUI slotPrefab;

    [Header("Layout")]
    [SerializeField] private int maxSlots = 3;
    [SerializeField] private float slotSpacing = 80f;

    [Header("Animation")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float floatInOffsetX = -200f;

    [Header("Sprites")]
    [SerializeField] private PartsSpriteTable spriteTable;

    [Header("Scale")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 activeScale = Vector3.one * 1.2f;
    [SerializeField] private float activeExtraSpacing = 20f;


    private readonly List<ItemSlotUI> slots = new();

    void Awake()
    {
        Debug.Log("[UI] ItemUIManager Awake");

        if (slotPrefab == null)
            Debug.LogError("[UI] slotPrefab is NULL");

        if (uiRoot == null)
            Debug.LogError("[UI] uiRoot is NULL");

        for (int i = 0; i < maxSlots; i++)
        {
            ItemSlotUI slot = Instantiate(slotPrefab, uiRoot);

            if (slot == null)
            {
                Debug.LogError($"[UI] Slot Instantiate failed at {i}");
                continue;
            }

            Debug.Log($"[UI] Slot created: {slot.name}");

            slot.transform.localPosition = GetSlotPos(i);

            slot.Clear();
            slot.gameObject.SetActive(false); // 明示的に非表示

            slots.Add(slot);
        }
    }

    public void RefreshFromQueue(IList<int> queue)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            ItemSlotUI slot = slots[i];

            PartsID? newId =
                i < queue.Count ? (PartsID)queue[i] : null;

            if (slot.ItemId == newId)
                continue;

            if (newId.HasValue)
            {
                Sprite sprite = spriteTable.GetSprite(newId.Value);
                slot.SetItem(newId.Value, sprite);
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
            }
        }

        AnimateReorder();
        UpdateSlotScales();
    }

    private void UpdateSlotScales()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            ItemSlotUI slot = slots[i];

            if (!slot.gameObject.activeSelf)
                continue;

            // 一番上（使用可能アイテム）だけ大きく
            slot.transform.localScale =
                (i == 0) ? activeScale : normalScale;
        }
    }

    // ----------------------------
    // 内部処理
    // ----------------------------
    private void ActivateSlot(int index, PartsID id, Sprite sprite)
    {
        ItemSlotUI slot = slots[index];

        if (slot == null)
        {
            Debug.LogError($"[UI] Slot[{index}] is NULL");
            return;
        }

        Debug.Log($"[UI] ActivateSlot index={index}, id={id}, sprite={sprite.name}");

        slot.gameObject.SetActive(true);

        slot.SetItem(id, sprite);

        Vector3 target = GetSlotPos(index);
        Vector3 start = target + Vector3.right * floatInOffsetX;

        slot.transform.localPosition = start;
        StartCoroutine(Move(slot.transform, target));
    }


    private void ReorderSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            StartCoroutine(Move(slots[i].transform, GetSlotPos(i)));
        }
    }

    private Vector3 GetSlotPos(int index)
    {
        float y = -slotSpacing * index;

        if (index > 0)
        {
            float scaleOffset =
                (activeScale.y - 1f) * slotSpacing * 0.5f;

            y -= scaleOffset;
        }

        return new Vector3(0, y, 0);
    }



    private System.Collections.IEnumerator Move(Transform t, Vector3 target)
    {
        while (Vector3.Distance(t.localPosition, target) > 0.1f)
        {
            t.localPosition =
                Vector3.Lerp(t.localPosition, target, Time.deltaTime * moveSpeed);
            yield return null;
        }
        t.localPosition = target;
    }

    private void AnimateReorder()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            StartCoroutine(
                Move(slots[i].transform, GetSlotPos(i))
            );
        }
    }

}
