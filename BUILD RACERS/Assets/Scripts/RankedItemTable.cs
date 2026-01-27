using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Ranked Item Table", fileName = "RankedItemTable")]
public class RankedItemTable : ScriptableObject
{
    public enum RankMode
    {
        ExactRank,     // rank=1 は1位設定、rank=2 は2位設定…
        RankRange      // 1-2位, 3-4位…のような範囲で設定
    }

    [SerializeField] private RankMode rankMode = RankMode.ExactRank;

    [SerializeField] private List<RankEntry> entries = new();

    [Serializable]
    public class RankEntry
    {
        [Header("Rank")]
        public int rankMin = 1;
        public int rankMax = 1;

        [Header("Tables")]
        public WeightedList passive = new();
        public WeightedList item = new();
        public WeightedList gimmick = new();
    }

    [Serializable]
    public class WeightedList
    {
        public List<WeightedItem> items = new();
    }

    [Serializable]
    public class WeightedItem
    {
        public PartsID id;
        [Min(0f)] public float weight = 1f; // 確率 = 重み / 合計重み
    }

    /// <summary>
    /// rank(1位=1) と type に応じて抽選して返す。候補が無ければ default(0) を返す。
    /// </summary>
    public PartsID GetRandom(int rank, PartsType type)
    {
        var entry = FindEntry(rank);
        if (entry == null) return 0;

        List<WeightedItem> list = type switch
        {
            PartsType.Passive => entry.passive.items,
            PartsType.Item => entry.item.items,
            _ => entry.gimmick.items,
        };

        return Draw(list);
    }

    private RankEntry FindEntry(int rank)
    {
        if (entries == null || entries.Count == 0) return null;

        if (rankMode == RankMode.ExactRank)
        {
            // Exact: rankMin==rankMax==rank のものを優先
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].rankMin == rank && entries[i].rankMax == rank)
                    return entries[i];
            }
        }

        // Range fallback: rankMin <= rank <= rankMax
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].rankMin <= rank && rank <= entries[i].rankMax)
                return entries[i];
        }

        return null;
    }

    private static PartsID Draw(List<WeightedItem> list)
    {
        if (list == null || list.Count == 0) return 0;

        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].weight > 0f) total += list[i].weight;
        }
        if (total <= 0f) return 0;

        float r = UnityEngine.Random.value * total;
        float acc = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            float w = Mathf.Max(0f, list[i].weight);
            acc += w;
            if (r <= acc)
                return list[i].id;
        }

        // 浮動小数誤差対策
        return list[list.Count - 1].id;
    }
}
