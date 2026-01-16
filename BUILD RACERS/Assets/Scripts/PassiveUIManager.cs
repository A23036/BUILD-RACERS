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
    [SerializeField] private float rowSpacing = 80f;

    [Header("Animation")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float floatInOffsetX = -200f;

    [Header("Sprites")]
    [SerializeField] private PartsSpriteTable spriteTable;

    private readonly List<PassiveSlotUI> slots = new();
    private readonly Dictionary<Transform, Coroutine> moveRoutines = new();
    private readonly List<SlotEntry> currentEntries = new();

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
        currentEntries.Clear();
        currentEntries.AddRange(BuildEntries(accelerationCount, speedCount, antiStunCount));
        EnsureSlots(currentEntries.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            PassiveSlotUI slot = slots[i];
            bool shouldShow = i < currentEntries.Count;

            if (!shouldShow)
            {
                slot.Clear();
                slot.gameObject.SetActive(false);
                continue;
            }

            SlotEntry entry = currentEntries[i];
            PartsID id = entry.Id;
            Sprite sprite = spriteTable != null ? spriteTable.GetSprite(id) : null;

            if (!slot.gameObject.activeSelf)
            {
                ActivateSlot(i, entry, sprite);
            }
            else if (slot.ItemId != id)
            {
                slot.SetItem(id, sprite);
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.gameObject.SetActive(true);
            }
            StartMove(slot.transform, GetSlotPos(entry));
        }

        AnimateReorder();
    }

    private List<SlotEntry> BuildEntries(int accelerationCount, int speedCount, int antiStunCount)
    {
        List<SlotEntry> entries = new();
        AddParts(entries, PartsID.Acceleration, accelerationCount, 0);
        AddParts(entries, PartsID.Speed, speedCount, 1);
        AddParts(entries, PartsID.AntiStun, antiStunCount, 2);
        return entries;
    }

    private static void AddParts(List<SlotEntry> entries, PartsID id, int count, int row)
    {
        for (int i = 0; i < count; i++)
        {
            entries.Add(new SlotEntry(id, row, i));
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
        slot.transform.localPosition = Vector3.zero;
        slot.Clear();
        slot.gameObject.SetActive(false);
        slots.Add(slot);
        return slot;
    }

    private void ActivateSlot(int index, SlotEntry entry, Sprite sprite)
    {
        PassiveSlotUI slot = slots[index];
        slot.gameObject.SetActive(true);
        slot.SetItem(entry.Id, sprite);

        Vector3 target = GetSlotPos(entry);
        Vector3 start = target + Vector3.right * floatInOffsetX;
        slot.transform.localPosition = start;
        StartMove(slot.transform, target);
    }

    private Vector3 GetSlotPos(SlotEntry entry)
    {
        float x = slotSpacing * entry.Column;
        float y = -rowSpacing * entry.Row;
        return new Vector3(x, y, 0f);
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
            PassiveSlotUI slot = slots[i];
            if (!slot.gameObject.activeSelf)
            {
                continue;
            }
            if (i < currentEntries.Count)
            {
                StartMove(slot.transform, GetSlotPos(currentEntries[i]));
            }
        }
    }

    private readonly struct SlotEntry
    {
        public PartsID Id { get; }
        public int Row { get; }
        public int Column { get; }

        public SlotEntry(PartsID id, int row, int column)
        {
            Id = id;
            Row = row;
            Column = column;
        }
    }
}
