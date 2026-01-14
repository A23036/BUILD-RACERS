using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Item/PartsSpriteTable")]
public class PartsSpriteTable : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public PartsID id;
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries;

    private Dictionary<PartsID, Sprite> dict;

    void OnEnable()
    {
        dict = new Dictionary<PartsID, Sprite>();
        foreach (var e in entries)
            dict[e.id] = e.sprite;
    }

    public Sprite GetSprite(PartsID id)
    {
        return dict.TryGetValue(id, out var s) ? s : null;
    }
}
