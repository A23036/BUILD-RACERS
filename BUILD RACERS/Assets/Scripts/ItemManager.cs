using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    //シングルで使うアイテムの重み
    private Dictionary<PartsID, int> itemWeightMap;

    //シングルで使うパッシブの重み
    private Dictionary<PartsID, int> passiveWeightMap;

    //各アイテムの重み
    [SerializeField] private int energyWeight;
    [SerializeField] private int rocketWeight;
    [SerializeField] private int rocketHomingWeight;
    [SerializeField] private int balloonTrapWeight;
    [SerializeField] private int killerWeight;

    [SerializeField] private int speedWeight;
    [SerializeField] private int accelerationWeight;
    [SerializeField] private int antiStunWeight;

    [SerializeField] private int ItemCapacity;
    [SerializeField] private int PassiveCapacity;

    [SerializeField] private RankedItemTable rankedTable;
    private int nowPassiveCapacity;
    private int nowItemCapacity;

    private LinkedList<int> itemQueue = new LinkedList<int>();

    // シングルのみで使うパッシブ用キュー
    private LinkedList<PartsID> passiveQueue = new LinkedList<PartsID>();


    // 同じIDのノードをリストで管理
    private Dictionary<int, List<LinkedListNode<int>>> nodeMap = new Dictionary<int, List<LinkedListNode<int>>>();

    CarController carController;
    private ItemUIManager itemUI;

    // チュートリアル用アイテム取得通知
    public event Action OnFirstItemAcquired;
    private bool firstItemNotified = false;

    // チュートリアル用アイテム使用通知
    public event Action OnFirstItemUsed;
    private bool firstItemUsedNotified = false;

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
        passiveWeightMap = new Dictionary<PartsID, int>();
        SetItemWeight();
        SetPassiveWeight();

        nowItemCapacity = 0;
        nowPassiveCapacity = 0;
    }

    // アイテム追加（同じIDも追加可能）
    public void ItemEnqueue(int itemId)
    {
        //シングルプレイなら重みチェック
        if (!PhotonNetwork.IsConnected && carController.isMine)
        {
            //キャパオーバーなら処理なし
            if (nowItemCapacity + itemWeightMap[(PartsID)itemId] > ItemCapacity)
            {
                Debug.Log("parts capacity over");
                Debug.Log("ItemCapacity : " + nowItemCapacity);
                return;
            }

            nowItemCapacity += itemWeightMap[(PartsID)itemId];
            Debug.Log("ItemCapacity : " + nowItemCapacity);
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

        // 初回のみの通知
        NotifyFirstItemIfNeeded();
    }

    // パッシブ追加(シングルプレイ)
    public void PassiveEnqueue(int itemId)
    {
        if(!carController.isMine) return;

        PartsID id = (PartsID)itemId;

        // 重みが無い/未設定なら事故るのでガード
        if (passiveWeightMap == null || !passiveWeightMap.ContainsKey(id))
        {
            Debug.LogError("passiveWeightMapに重みがありません : " + id);
            return;
        }

        int addW = passiveWeightMap[id];

        //キャパオーバーなら古い順に新しいものが入るまで削除
        if (nowPassiveCapacity + addW > PassiveCapacity)
        {
            // 入るまで最古を消す
            while (nowPassiveCapacity + addW > PassiveCapacity)
            {
                // 最古を削除
                PartsID oldest = passiveQueue.First.Value;
                passiveQueue.RemoveFirst();

                if (passiveWeightMap.ContainsKey(oldest))
                {
                    nowPassiveCapacity -= passiveWeightMap[oldest];
                    carController.RPC_SetPassiveState(oldest, false);
                }
            }
        }

        // 追加
        passiveQueue.AddLast(id);
        nowPassiveCapacity += addW;

        carController.RPC_SetPassiveState(id, true);

        Debug.Log("PassiveCapacity : " + nowPassiveCapacity);
    }

    // 最も古いアイテムを取り出す
    public int? ItemDequeue(bool isUse)
    {
        if (itemQueue.Count == 0)
            return null;
        var firstNode = itemQueue.First;

        int id = firstNode.Value;
        
        PrintItemQueue();

        //シングルプレイなら重み計算
        if(!PhotonNetwork.IsConnected && carController.isMine && isUse)
        {
            nowItemCapacity -= itemWeightMap[(PartsID)id];
            Debug.Log("ItemCapacity : " + nowItemCapacity);
        }

        // 使用フラグが立っていたらアイテム生成
        if (isUse)
        {
            SpawnItem((PartsID)id);

            // 初回通知
            NotifyFirstItemUsedIfNeeded();
        }

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
                var obj = PhotonNetwork.Instantiate(
                    "PetBottle_Rocket_Green",
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );

                //所有者名のセット
                RocketGreen rocket = obj.GetComponent<RocketGreen>();
                rocket.SetParentName(obj.GetComponent<PhotonView>().Owner.NickName);
            }
            else
            {
                GameObject prefab = (GameObject)Resources.Load("PetBottle_Rocket_Green");

                var obj = Instantiate(
                    prefab,
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );

                //所有者名のセット
                RocketGreen rocket = obj.GetComponent<RocketGreen>();
                CarController carController = GetComponent<CarController>();
                if(carController.isMine) rocket.SetParentName(PlayerPrefs.GetString("PlayerName"));
                else rocket.SetParentName(carController.GetName());
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

                //所有者名のセット
                RocketRed rocketRed = rocket.GetComponent<RocketRed>();
                rocketRed.SetParentName(rocket.GetComponent<PhotonView>().Owner.NickName);
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

                //所有者名のセット
                RocketRed rocketRed = rocket.GetComponent<RocketRed>();
                CarController carController = GetComponent<CarController>();
                if (carController.isMine) rocketRed.SetParentName(PlayerPrefs.GetString("PlayerName"));
                else rocketRed.SetParentName(carController.GetName());
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
                var balloon = PhotonNetwork.Instantiate(
                    "BalloonTrap",
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );

                //所有者名のセット
                WaterBalloonTrap balloonTrap = balloon.GetComponentInChildren<WaterBalloonTrap>();
                balloonTrap.SetParentName(balloon.GetComponentInChildren<PhotonView>().Owner.NickName);
            }
            else
            {
                GameObject prefab = (GameObject)Resources.Load("BalloonTrap");

                var balloon = Instantiate(
                    prefab,
                    spawnPos,
                    transform.rotation   // 向きも自身に合わせる
                );

                //所有者名のセット
                WaterBalloonTrap balloonTrap = balloon.GetComponentInChildren<WaterBalloonTrap>();
                CarController carController = GetComponent<CarController>();
                if (carController.isMine) balloonTrap.SetParentName(PlayerPrefs.GetString("PlayerName"));
                else balloonTrap.SetParentName(carController.GetName());
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
    }

    public void SetPassiveWeight()
    {
        passiveWeightMap[PartsID.Speed] = speedWeight;
        passiveWeightMap[PartsID.Acceleration] = accelerationWeight;
        passiveWeightMap[PartsID.AntiStun] = antiStunWeight;
    }

    private void NotifyFirstItemIfNeeded()
    {
        if (firstItemNotified) return;
        firstItemNotified = true;
        OnFirstItemAcquired?.Invoke();
    }
    private void NotifyFirstItemUsedIfNeeded()
    {
        if (firstItemUsedNotified) return;
        firstItemUsedNotified = true;
        OnFirstItemUsed?.Invoke();
    }
}
