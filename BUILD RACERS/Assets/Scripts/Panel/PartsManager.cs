using System.Collections.Generic;
using UnityEngine;

public enum PartsType
{
    Passive = 0,
    Item,
    Gimmick,
}

public enum PartsID
{
    Energy = 0,
    Rocket,
    Flask,
}

public class PartsManager : MonoBehaviour
{
    [Header("パーツプレハブリスト")]
    public List<GameObject> partsPrefabs = new List<GameObject>();

    // ID → プレハブ の辞書
    private Dictionary<PartsID, Parts> partsDictionary;

    // 生成位置
    private Vector3 spawnPos = new Vector3(0,0,0);

    // ---------------------------------------------------------
    // 初期化
    // ---------------------------------------------------------
    void Awake()
    {
        // プレハブを辞書化
        partsDictionary = new Dictionary<PartsID, Parts>();

        foreach (var prefab in partsPrefabs)
        {
            Parts parts = prefab.GetComponent<Parts>();
            if (prefab == null) continue;

            if (!partsDictionary.ContainsKey(parts.GetPartsID()))
                partsDictionary.Add(parts.GetPartsID(), parts);
            else
                Debug.LogWarning($"PartsID {parts.GetPartsID()} が重複しています！");
        }
    }

    // ---------------------------------------------------------
    // ID を指定してパーツを生成する関数
    // ---------------------------------------------------------
    public Parts SpawnParts(PartsID id)
    {
        if (!partsDictionary.TryGetValue(id, out Parts prefab))
        {
            Debug.LogError($"PartsID {id} のプレハブが見つかりません！");
            return null;
        }

        // プレハブ生成
        Parts newParts = Instantiate(prefab, spawnPos, Quaternion.identity);

        Debug.Log($"[PartsManager] {id} を生成しました");

        return newParts;
    }
}
