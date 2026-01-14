using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ItemManager : MonoBehaviour
{
    private LinkedList<int> itemQueue = new LinkedList<int>();

    // 同じIDのノードをリストで管理
    private Dictionary<int, List<LinkedListNode<int>>> nodeMap = new Dictionary<int, List<LinkedListNode<int>>>();

    CarController carController;

    public int GetItemNum() => itemQueue.Count;

    private void Start()
    {
        carController = GetComponent<CarController>();
    }

    // アイテム追加（同じIDも追加可能）
    public void Enqueue(int itemId)
    {
        var node = itemQueue.AddLast(itemId);

        if (!nodeMap.ContainsKey(itemId))
            nodeMap[itemId] = new List<LinkedListNode<int>>();

        nodeMap[itemId].Add(node);
        PrintItemQueue();
    }

    // 最も古いアイテムを取り出す
    public int? Dequeue(bool isUse)
    {
        if (itemQueue.Count == 0)
            return null;
        var firstNode = itemQueue.First;

        int id = firstNode.Value;
        
        PrintItemQueue();
        
        // 使用フラグが立っていたらアイテム生成
        if(isUse) SpawnItem((PartsID)id);

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

        PrintItemQueue();
        return true;
    }

    public void PrintItemQueue()
    {
        string s = "ItemQueue: ";
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
                    return PartsID.AntiStun;
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
                int r3 = Random.Range(0, 4);
                Debug.Log("RandomItem:" + r3);
                if (r3 == 0)
                {
                    return PartsID.Mud;
                }
                else if (r3 == 1)
                {
                    return PartsID.Balloon;
                }
                else if (r3 == 2)
                {
                    return PartsID.Wall;
                }
                else
                {
                    return PartsID.Slope;
                }
            default:
                return 0;
        }
    }

    public PartsType GetPartsType(PartsID id)
    {
        PartsType type = 0;

        switch ((PartsID)id)
        {
            case PartsID.Energy:
            case PartsID.Rocket:
                type = PartsType.Item;
                break;
            case PartsID.Speed:
            case PartsID.Acceleration:
                type = PartsType.Passive;
                break;
            default:
                type = PartsType.Gimmick;
                break;
        }

        return type;
    }

    public void SpawnItem(PartsID id)
    {
        if(id == PartsID.Energy)
        {
            // 加速状態を付与
            carController.SetBoost(BoostType.Short);
        }

        if(id == PartsID.Rocket)
        {
            float forwardOffset = 5.0f;   // 前方距離
            float heightOffset = 1.5f;   // 少し浮かせる（地面埋まり防止）

            Vector3 spawnPos =
                transform.position +
                transform.forward * forwardOffset +
                Vector3.up * heightOffset;

            if(PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(
                    "PetBottle_Rocket_Green",
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );
            }
            else
            {
                GameObject prefab = (GameObject)Resources.Load("PetBottle_Rocket_Green");

                Instantiate(
                    prefab,
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );
            }

            return;
        }
    }
}
