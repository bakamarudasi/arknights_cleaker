using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテム・素材の在庫管理を担当するマネージャー
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // ========================================
    // 在庫データ（ID → 個数）
    // ========================================

    [Header("在庫 (Debug)")]
    [SerializeField] private SerializableDictionary<string, int> _inventory = new();

    // ========================================
    // イベント
    // ========================================

    /// <summary>アイテム数が変化した時 (id, newCount)</summary>
    public event Action<string, int> OnItemCountChanged;

    // ========================================
    // 統計用コールバック（外部から設定）
    // ========================================

    /// <summary>アイテムを使用した時の統計更新用</summary>
    public Action<int> OnMaterialsUsed;

    // ========================================
    // 初期化
    // ========================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========================================
    // 基本操作
    // ========================================

    /// <summary>
    /// アイテムの所持数を取得
    /// </summary>
    public int GetCount(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _inventory.TryGetValue(id, out int count) ? count : 0;
    }

    /// <summary>
    /// アイテムを追加
    /// </summary>
    public void Add(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return;

        int newCount = GetCount(id) + amount;
        _inventory[id] = newCount;
        OnItemCountChanged?.Invoke(id, newCount);
    }

    /// <summary>
    /// アイテムを使用（消費）
    /// </summary>
    /// <returns>成功したらtrue</returns>
    public bool Use(string id, int amount = 1)
    {
        if (!Has(id, amount)) return false;

        int newCount = GetCount(id) - amount;
        _inventory[id] = newCount;
        OnItemCountChanged?.Invoke(id, newCount);
        OnMaterialsUsed?.Invoke(amount);
        return true;
    }

    /// <summary>
    /// 指定数を持っているかチェック
    /// </summary>
    public bool Has(string id, int amount = 1)
    {
        return GetCount(id) >= amount;
    }

    /// <summary>
    /// アイテムの所持数を直接設定（ロード用）
    /// </summary>
    public void SetCount(string id, int count)
    {
        if (string.IsNullOrEmpty(id)) return;
        _inventory[id] = Mathf.Max(0, count);
        OnItemCountChanged?.Invoke(id, _inventory[id]);
    }

    // ========================================
    // 複数アイテム操作（強化素材用）
    // ========================================

    /// <summary>
    /// 必要素材を全て持っているかチェック
    /// </summary>
    public bool HasAllMaterials(List<ItemCost> costs)
    {
        if (costs == null || costs.Count == 0) return true;

        foreach (var cost in costs)
        {
            if (cost.item == null) continue;
            if (!Has(cost.item.id, cost.amount))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 素材をまとめて消費
    /// </summary>
    /// <returns>成功したらtrue</returns>
    public bool UseAllMaterials(List<ItemCost> costs)
    {
        if (!HasAllMaterials(costs)) return false;

        foreach (var cost in costs)
        {
            if (cost.item == null) continue;
            Use(cost.item.id, cost.amount);
        }
        return true;
    }

    /// <summary>
    /// 解放用アイテムを持っているかチェック
    /// </summary>
    public bool HasUnlockItem(ItemData item)
    {
        if (item == null) return true;
        return Has(item.id);
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// 全アイテムのIDリストを取得
    /// </summary>
    public List<string> GetAllItemIds()
    {
        return new List<string>(_inventory.Keys);
    }

    /// <summary>
    /// 所持アイテム数（種類数）
    /// </summary>
    public int GetUniqueItemCount()
    {
        int count = 0;
        foreach (var kvp in _inventory)
        {
            if (kvp.Value > 0) count++;
        }
        return count;
    }

    /// <summary>
    /// 在庫をクリア（デバッグ・ニューゲーム用）
    /// </summary>
    public void Clear()
    {
        var ids = GetAllItemIds();
        _inventory.Clear();
        foreach (var id in ids)
        {
            OnItemCountChanged?.Invoke(id, 0);
        }
    }

    // ========================================
    // セーブ/ロード用
    // ========================================

    /// <summary>
    /// 在庫データを取得（セーブ用）
    /// </summary>
    public Dictionary<string, int> GetInventoryData()
    {
        return new Dictionary<string, int>(_inventory);
    }

    /// <summary>
    /// 在庫データを設定（ロード用）
    /// </summary>
    public void SetInventoryData(Dictionary<string, int> data)
    {
        _inventory.Clear();
        if (data != null)
        {
            foreach (var kvp in data)
            {
                _inventory[kvp.Key] = kvp.Value;
                OnItemCountChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }
    }
}

// ========================================
// シリアライズ可能なDictionary（Inspectorで見える）
// ========================================

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new();
    [SerializeField] private List<TValue> values = new();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var kvp in this)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
        {
            this[keys[i]] = values[i];
        }
    }
}
