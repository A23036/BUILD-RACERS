using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform uiRoot;
    [SerializeField] private PassiveSlotUI slotPrefab;

    [Header("Layout")]
    [SerializeField] private int initialSlots = 6;
    [SerializeField] private float slotSpacing = 80f;

    [Header("Animation")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float floatInOffsetX = -200f;

    [Header("Sprites")]
    [SerializeField] private PartsSpriteTable spriteTable;

    private readonly List<PassiveSlotUI> slots = new();
    private readonly Dictionary<Transform, Coroutine> moveRoutines = new();

    private void Awake()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("[UI] Passive slotPrefab is NULL");
            return;
        }

        if (uiRoot == null)
        {
            Debug.LogError("[UI] Passive uiRoot is NULL");
            return;
        }

        for (int i = 0; i < initialSlots; i++)
        {
            CreateSlot();
        }
    }

    public void RefreshFromCounts(int accelerationCount, int speedCount, int antiStunCount)
    {
        List<PartsID> order = BuildOrder(accelerationCount, speedCount, antiStunCount);
        EnsureSlots(order.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            PassiveSlotUI slot = slots[i];
            bool shouldShow = i < order.Count;

            if (!shouldShow)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
                continue;
            }

            PartsID id = order[i];
            Sprite sprite = spriteTable != null ? spriteTable.GetSprite(id) : null;

            if (!slot.gameObject.activeSelf)
            {
                ActivateSlot(i, id, sprite);
            }
            else if (slot.ItemId != id)
            {
                slot.SetItem(id, sprite);
                slot.gameObject.SetActive(true);
            }
        }

        AnimateReorder();
    }

    private List<PartsID> BuildOrder(int accelerationCount, int speedCount, int antiStunCount)
    {
        List<PartsID> order = new();
        AddParts(order, PartsID.Acceleration, accelerationCount);
        AddParts(order, PartsID.Speed, speedCount);
        AddParts(order, PartsID.AntiStun, antiStunCount);
        return order;
    }

    private static void AddParts(List<PartsID> order, PartsID id, int count)
    {
        for (int i = 0; i < count; i++)
        {
            order.Add(id);
        }
    }

    private void EnsureSlots(int required)
    {
        while (slots.Count < required)
        {
            CreateSlot();
        }
    }

    private PassiveSlotUI CreateSlot()
    {
        PassiveSlotUI slot = Instantiate(slotPrefab, uiRoot);
        slot.transform.localPosition = GetSlotPos(slots.Count);
        slot.Clear();
        slot.gameObject.SetActive(false);
        slots.Add(slot);
        return slot;
    }

    private void ActivateSlot(int index, PartsID id, Sprite sprite)
    {
        PassiveSlotUI slot = slots[index];
        slot.gameObject.SetActive(true);
        slot.SetItem(id, sprite);

        Vector3 target = GetSlotPos(index);
        Vector3 start = target + Vector3.right * floatInOffsetX;
        slot.transform.localPosition = start;
        StartMove(slot.transform, target);
    }

    private Vector3 GetSlotPos(int index)
    {
        float x = slotSpacing * index;
        return new Vector3(x, 0f, 0f);
    }

    private void StartMove(Transform target, Vector3 destination)
    {
        if (moveRoutines.TryGetValue(target, out Coroutine routine) && routine != null)
        {
            StopCoroutine(routine);
        }

        moveRoutines[target] = StartCoroutine(Move(target, destination));
    }

    private IEnumerator Move(Transform target, Vector3 destination)
    {
        while (Vector3.Distance(target.localPosition, destination) > 0.1f)
        {
            target.localPosition = Vector3.Lerp(target.localPosition, destination, Time.deltaTime * moveSpeed);
            yield return null;
        }

        target.localPosition = destination;
    }

    private void AnimateReorder()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            StartMove(slots[i].transform, GetSlotPos(i));
        }
    }
}
