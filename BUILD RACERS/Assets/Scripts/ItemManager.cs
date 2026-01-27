using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ItemManager : MonoBehaviour
{
    //シングルで使うアイテムの重み
    private Dictionary<PartsID, int> itemWeightMap;

    //各アイテムの重み
    [SerializeField] private int energyWeight;
    [SerializeField] private int rocketWeight;
    [SerializeField] private int rocketHomingWeight;
    [SerializeField] private int balloonTrapWeight;
    [SerializeField] private int killerWeight;

    [SerializeField] private int speedWeight;
    [SerializeField] private int accelerationWeight;
    [SerializeField] private int antiStunWeight;

    [SerializeField] private int maxCapacity;

    [SerializeField] private RankedItemTable rankedTable;
    private int nowCapacity;

    private LinkedList<int> itemQueue = new LinkedList<int>();

    // 同じIDのノードをリストで管理
    private Dictionary<int, List<LinkedListNode<int>>> nodeMap = new Dictionary<int, List<LinkedListNode<int>>>();

    CarController carController;
    private ItemUIManager itemUI;


    public int GetItemNum() => itemQueue.Count;

    private void Start()
    {
        var pv = GetComponent<PhotonView>();
        if(PhotonNetwork.IsConnected && pv != null && pv.IsMine == false)
        {
            return;
        }

        carController = GetComponent<CarController>();

        var itemSlot = GameObject.Find("ItemSlotRoot");
        if(itemSlot != null)
        {
            itemUI = itemSlot.GetComponent<ItemUIManager>();
        }

        //重みの設定
        itemWeightMap = new Dictionary<PartsID, int>();
        SetItemWeight();

        nowCapacity = 0;
    }

    // アイテム追加（同じIDも追加可能）
    public void Enqueue(int itemId)
    {
        //シングルプレイなら重みチェック
        if (!PhotonNetwork.IsConnected)
        {
            //キャパオーバーなら処理なし
            if (nowCapacity + itemWeightMap[(PartsID)itemId] > maxCapacity)
            {
                Debug.Log("parts capacity over");
                Debug.Log("Capacity : " + nowCapacity);
                return;
            }

            nowCapacity += itemWeightMap[(PartsID)itemId];
            Debug.Log("Capacity : " + nowCapacity);
        }

        var node = itemQueue.AddLast(itemId);

        if (!nodeMap.ContainsKey(itemId))
            nodeMap[itemId] = new List<LinkedListNode<int>>();

        nodeMap[itemId].Add(node);

        // アイテムUIの更新
        if(carController.isMine && carController.isRaceClear == false && itemUI != null)
        {
            itemUI.RefreshFromQueue(new List<int>(itemQueue));
            PrintItemQueue();
        }
    }

    // 最も古いアイテムを取り出す
    public int? Dequeue(bool isUse)
    {
        if (itemQueue.Count == 0)
            return null;
        var firstNode = itemQueue.First;

        int id = firstNode.Value;
        
        PrintItemQueue();

        //シングルプレイなら重み計算
        if(!PhotonNetwork.IsConnected)
        {
            nowCapacity -= itemWeightMap[(PartsID)id];
            Debug.Log("Capacity : " + nowCapacity);
        }
        
        // 使用フラグが立っていたらアイテム生成
        if(isUse) SpawnItem((PartsID)id);

        // アイテムUIの更新
        if (carController.isMine && carController.isRaceClear == false && itemUI != null)
        {
            itemUI.RefreshFromQueue(new List<int>(itemQueue));
        }

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

        // アイテムUIの更新
        if (carController.isMine && carController.isRaceClear == false && itemUI != null)
        {
            itemUI.RefreshFromQueue(new List<int>(itemQueue));
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

    public PartsID GetRandomItem(int rank,PartsType type)
    {
        if (rankedTable == null)
        {
            Debug.LogError("RankedItemTable が未設定です ItemManagerのinspectorから設定してください");
        }

        PartsID randomId = rankedTable.GetRandom(rank, type);
        /*
        switch (type)
        {
            case PartsType.Passive:
                Debug.Log("Rank:" + rank + " GetPassive:" + randomId);
                break;
            case PartsType.Item:
                Debug.Log("Rank:" + rank + " GetItem:" + randomId);
                break;
            case PartsType.Gimmick:
                Debug.Log("Rank:" + rank + " GetGimmick:" + randomId);
                break;
        }\
        */
        return randomId;
    }

    public PartsType GetPartsType(PartsID id)
    {
        PartsType type = 0;

        switch (id)
        {
            case PartsID.Energy:
            case PartsID.Rocket:
            case PartsID.RocketHoming:
            case PartsID.BalloonTrap:
            case PartsID.Killer:
                type = PartsType.Item;
                break;
            case PartsID.Speed:
            case PartsID.Acceleration:
            case PartsID.AntiStun:
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
            float forwardOffset = 3.0f;   // 前方距離
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
        if (id == PartsID.RocketHoming)
        {
            float forwardOffset = 3.0f;   // 前方距離
            float heightOffset = 1.5f;   // 少し浮かせる（地面埋まり防止）

            Vector3 spawnPos =
                transform.position +
                transform.forward * forwardOffset +
                Vector3.up * heightOffset;

            if (PhotonNetwork.IsConnected)
            {
                var rocket = PhotonNetwork.Instantiate(
                    "PetBottle_Rocket_Red",
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );
                // ロケットの生成者をセット
                rocket.GetComponent<RocketRed>().SetOwner(transform);
            }
            else
            {
                GameObject prefab = (GameObject)Resources.Load("PetBottle_Rocket_Red");

                var rocket = Instantiate(
                    prefab,
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );
                rocket.GetComponent<RocketRed>().SetOwner(transform);
            }

            return;
        }
        if (id == PartsID.BalloonTrap)
        {
            float forwardOffset = -4.0f;   // 後方距離
            float heightOffset = 0f;

            Vector3 spawnPos =
                transform.position +
                transform.forward * forwardOffset +
                Vector3.up * heightOffset;

            if (PhotonNetwork.IsConnected)
            {
                var rocket = PhotonNetwork.Instantiate(
                    "BalloonTrap",
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );
            }
            else
            {
                GameObject prefab = (GameObject)Resources.Load("BalloonTrap");

                var rocket = Instantiate(
                    prefab,
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );
            }

            return;
        }

        if (id == PartsID.Killer)
        {
            // 即座にキラー状態を付与
            carController.SetKiller();
            Debug.Log("KILLER SET");
            return;
        }
    }

    public void SetItemWeight()
    {
        itemWeightMap[PartsID.Energy] = energyWeight;
        itemWeightMap[PartsID.Rocket] = rocketWeight;
        itemWeightMap[PartsID.RocketHoming] = rocketHomingWeight;
        itemWeightMap[PartsID.BalloonTrap] = balloonTrapWeight;
        itemWeightMap[PartsID.Killer] = killerWeight;

        itemWeightMap[PartsID.Acceleration] = accelerationWeight;
        itemWeightMap[PartsID.Speed] = speedWeight;
        itemWeightMap[PartsID.AntiStun] = antiStunWeight;
    }
}
