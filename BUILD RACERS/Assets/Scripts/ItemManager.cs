using UnityEngine;
using System.Collections.Generic;

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
        //Debug.Log(s);
    }

    public int GetRandomItem(PartsType type)
    {
        switch (type)
        {
            case PartsType.Passive:
                return Random.Range(0, 3);
            case PartsType.Item:
                return Random.Range(4, 6);
            case PartsType.Gimmick:
                return Random.Range(7, 10);
            default:
                return 0;
        }
    }
}
