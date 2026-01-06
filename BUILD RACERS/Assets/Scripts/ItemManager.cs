using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UIElements;

public class ItemManager : MonoBehaviour
{
    private LinkedList<int> itemQueue = new LinkedList<int>();

    // 同じIDのノードをリストで管理
    private Dictionary<int, List<LinkedListNode<int>>> nodeMap = new Dictionary<int, List<LinkedListNode<int>>>();

    public int GetItemNum() => itemQueue.Count;

    // アイテム追加（同じIDも追加可能）
    public void Enqueue(int itemId)
    {
        var node = itemQueue.AddLast(itemId);

        if (!nodeMap.ContainsKey(itemId))
            nodeMap[itemId] = new List<LinkedListNode<int>>();

        nodeMap[itemId].Add(node);
        PrintQueue();
    }

    // 最も古いアイテムを取り出す
    public int? Dequeue()
    {
        if (itemQueue.Count == 0)
            return null;

        var firstNode = itemQueue.First;
        itemQueue.RemoveFirst();

        int id = firstNode.Value;
        nodeMap[id].Remove(firstNode);
        if (nodeMap[id].Count == 0)
            nodeMap.Remove(id);

        PrintQueue();
        return id;
    }

    // 任意のIDの最初の要素を削除（すべて削除も可能）
    public bool Remove(int itemId, bool removeAll = false)
    {
        if (!nodeMap.TryGetValue(itemId, out var nodes))
            return false;

        if (removeAll)
        {
            foreach (var node in nodes)
                itemQueue.Remove(node);
            nodeMap.Remove(itemId);
        }
        else
        {
            var node = nodes[0];
            itemQueue.Remove(node);
            nodes.RemoveAt(0);
            if (nodes.Count == 0)
                nodeMap.Remove(itemId);
        }

        PrintQueue();
        return true;
    }

    public void PrintQueue()
    {
        string s = "Queue: ";
        foreach (var id in itemQueue)
            s += id + " ";
        Debug.Log(s);
    }

    public PartsID GetRandomItem(PartsType type)
    {
        switch (type)
        {
            case PartsType.Passive:
                int r = Random.Range(0, 3);
                Debug.Log("RandomItem:" + r);
                if (r == 0)
                {
                    return PartsID.Acceleration;
                }
                else if (r == 1)
                {
                    return PartsID.Speed;
                }
                else
                {
                    return PartsID.AntiStan;
                }

            case PartsType.Item:
                int r2 = Random.Range(0, 2);
                Debug.Log("RandomItem:" + r2);
                if (r2 == 0)
                {
                    return PartsID.Energy;
                }
                else
                {
                    return PartsID.Rocket;
                }
            case PartsType.Gimmick:
                int r3 = Random.Range(0, 2);
                Debug.Log("RandomItem:" + r3);
                if (r3 == 0)
                {
                    return PartsID.Mud;
                }
                else
                {
                    return PartsID.Mud;
                }
            default:
                return 0;
        }
    }

    public void SpawnItem(PartsID id)
    {
        if(id == PartsID.Rocket)
        {
            float forwardOffset = 5.0f;   // 前方距離
            float heightOffset = 1.5f;   // 少し浮かせる（地面埋まり防止）

            Vector3 spawnPos =
                transform.position +
                transform.forward * forwardOffset +
                Vector3.up * heightOffset;

            PhotonNetwork.Instantiate(
                "PetBottle_Rocket_Green",
                spawnPos,
                transform.rotation   // 向きも自身に合わせる
            );
        }
    }
}
