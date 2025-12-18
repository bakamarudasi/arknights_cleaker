using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ガチャのロジック（排出計算、天井管理）を担当するマネージャー
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    // ========================================
    // 天井カウント（バナーID → カウント）
    // ========================================

    [Header("天井データ (Debug)")]
    [SerializeField] private SerializableDictionary<string, int> _pityCounters = new();

    // ========================================
    // イベント
    // ========================================

    /// <summary>ガチャ実行時 (banner, results)</summary>
    public event Action<GachaBannerData, List<GachaResultItem>> OnGachaPulled;

    /// <summary>天井到達時 (banner)</summary>
    public event Action<GachaBannerData> OnPityReached;

    /// <summary>高レア排出時 (result, rarity)</summary>
    public event Action<GachaResultItem, int> OnHighRarityPulled;

    // ========================================
    // 統計用コールバック
    // ========================================

    public Action<int> OnGachaCountIncremented;

    // ========================================
    // 初期化
    // ========================================

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ========================================
    // ガチャ実行
    // ========================================

    /// <summary>
    /// ガチャを引く
    /// </summary>
    /// <param name="banner">対象バナー</param>
    /// <param name="count">回数（1 or 10）</param>
    /// <returns>結果リスト</returns>
    public List<GachaResultItem> Pull(GachaBannerData banner, int count = 1)
    {
        if (banner == null || banner.pool == null || banner.pool.Count == 0)
        {
            Debug.LogWarning("GachaManager: Invalid banner or empty pool");
            return new List<GachaResultItem>();
        }

        var results = new List<GachaResultItem>();

        for (int i = 0; i < count; i++)
        {
            var result = PullSingle(banner);
            if (result != null)
            {
                results.Add(result);
            }
        }

        OnGachaPulled?.Invoke(banner, results);
        OnGachaCountIncremented?.Invoke(count);

        return results;
    }

    /// <summary>
    /// 単発ガチャ（内部処理）
    /// </summary>
    private GachaResultItem PullSingle(GachaBannerData banner)
    {
        // 天井カウント更新
        int currentPity = GetPityCount(banner.bannerId) + 1;
        SetPityCount(banner.bannerId, currentPity);

        GachaPoolEntry selectedEntry = null;

        // 天井チェック
        if (banner.hasPity && currentPity >= banner.pityCount)
        {
            // 天井到達 → 最高レアを確定排出
            selectedEntry = GetHighestRarityEntry(banner);
            ResetPityCount(banner.bannerId);
            OnPityReached?.Invoke(banner);
        }
        else
        {
            // 通常抽選（ソフト天井考慮）
            selectedEntry = SelectFromPool(banner, currentPity);
        }

        if (selectedEntry == null)
        {
            Debug.LogWarning("GachaManager: Failed to select entry");
            return null;
        }

        // 高レア排出時は天井リセット
        if (selectedEntry.Rarity >= 5)
        {
            ResetPityCount(banner.bannerId);
        }

        var result = new GachaResultItem(
            selectedEntry.item,
            isNew: CheckIsNew(selectedEntry.item),
            isPickup: selectedEntry.isPickup
        );

        // 高レア通知
        if (selectedEntry.Rarity >= 5)
        {
            OnHighRarityPulled?.Invoke(result, selectedEntry.Rarity);
        }

        return result;
    }

    // ========================================
    // 抽選ロジック
    // ========================================

    /// <summary>
    /// プールから抽選（ソフト天井考慮）
    /// </summary>
    private GachaPoolEntry SelectFromPool(GachaBannerData banner, int currentPity)
    {
        var pool = banner.pool;
        float totalWeight = 0f;

        // 重みを計算（ソフト天井補正含む）
        var adjustedWeights = new List<float>();

        foreach (var entry in pool)
        {
            float weight = entry.weight;

            // ソフト天井：高レアの確率を徐々に上昇
            if (banner.hasPity && currentPity >= banner.softPityStart && entry.Rarity >= 5)
            {
                float pityProgress = (float)(currentPity - banner.softPityStart) / (banner.pityCount - banner.softPityStart);
                weight *= (1f + pityProgress * 5f); // 最大6倍
            }

            // ピックアップ補正
            if (entry.isPickup && banner.pickupItems.Contains(entry.item))
            {
                weight *= (1f + banner.pickupRateBoost);
            }

            adjustedWeights.Add(weight);
            totalWeight += weight;
        }

        // 乱数で抽選
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += adjustedWeights[i];
            if (roll <= cumulative)
            {
                return pool[i];
            }
        }

        // フォールバック（最後のエントリを返す）
        return pool[pool.Count - 1];
    }

    /// <summary>
    /// 最高レアリティのエントリを取得（天井用）
    /// </summary>
    private GachaPoolEntry GetHighestRarityEntry(GachaBannerData banner)
    {
        GachaPoolEntry highest = null;
        int maxRarity = 0;

        // ピックアップ優先で最高レアを探す
        foreach (var entry in banner.pool)
        {
            if (entry.Rarity > maxRarity)
            {
                maxRarity = entry.Rarity;
                highest = entry;
            }
            // 同レアリティならピックアップを優先
            else if (entry.Rarity == maxRarity && entry.isPickup)
            {
                highest = entry;
            }
        }

        return highest;
    }

    /// <summary>
    /// 新規アイテムかチェック
    /// </summary>
    private bool CheckIsNew(ItemData item)
    {
        if (item == null) return false;
        // InventoryManager経由で所持チェック
        return !GameController.Instance.Inventory.Has(item.id);
    }

    // ========================================
    // 天井管理
    // ========================================

    public int GetPityCount(string bannerId)
    {
        if (string.IsNullOrEmpty(bannerId)) return 0;
        return _pityCounters.TryGetValue(bannerId, out int count) ? count : 0;
    }

    public void SetPityCount(string bannerId, int count)
    {
        if (string.IsNullOrEmpty(bannerId)) return;
        _pityCounters[bannerId] = count;
    }

    public void ResetPityCount(string bannerId)
    {
        if (string.IsNullOrEmpty(bannerId)) return;
        _pityCounters[bannerId] = 0;
    }

    // ========================================
    // 解放条件チェック
    // ========================================

    /// <summary>
    /// バナーが解放されているかチェック
    /// </summary>
    public bool IsBannerUnlocked(GachaBannerData banner)
    {
        if (banner == null) return false;
        if (!banner.startsLocked) return true;

        // アイテムで解放
        if (banner.requiredUnlockItem != null)
        {
            if (GameController.Instance.Inventory.Has(banner.requiredUnlockItem.id))
                return true;
        }

        // 前提バナー全取得で解放
        if (banner.prerequisiteBanner != null)
        {
            if (IsAllPoolItemsOwned(banner.prerequisiteBanner))
                return true;
        }

        return false;
    }

    /// <summary>
    /// バナーのプールを全取得しているかチェック
    /// </summary>
    public bool IsAllPoolItemsOwned(GachaBannerData banner)
    {
        if (banner == null || banner.pool == null || banner.pool.Count == 0)
            return false;

        foreach (var entry in banner.pool)
        {
            if (entry.item == null) continue;
            if (!GameController.Instance.Inventory.Has(entry.item.id))
                return false;
        }
        return true;
    }

    /// <summary>
    /// バナーのプール取得進捗を取得
    /// </summary>
    public (int owned, int total) GetPoolProgress(GachaBannerData banner)
    {
        if (banner == null || banner.pool == null)
            return (0, 0);

        int owned = 0;
        int total = 0;

        foreach (var entry in banner.pool)
        {
            if (entry.item == null) continue;
            total++;
            if (GameController.Instance.Inventory.Has(entry.item.id))
                owned++;
        }

        return (owned, total);
    }

    // ========================================
    // ガチャ実行（外部向け）
    // ========================================

    /// <summary>
    /// ガチャを引く（通貨消費・アイテム追加込み）
    /// </summary>
    public List<GachaResultItem> PullGacha(GachaBannerData banner, int count = 1)
    {
        if (banner == null) return new List<GachaResultItem>();

        double cost = banner.GetCost(count);

        // 通貨チェック
        if (!GameController.Instance.Wallet.CanAfford(cost, banner.currencyType))
            return new List<GachaResultItem>();

        // 通貨消費
        if (!GameController.Instance.Wallet.Spend(cost, banner.currencyType))
            return new List<GachaResultItem>();

        // ガチャ実行
        var results = Pull(banner, count);

        // 結果をインベントリに追加
        foreach (var item in results)
        {
            if (!string.IsNullOrEmpty(item.itemId))
            {
                GameController.Instance.Inventory.Add(item.itemId, 1);
            }
        }

        return results;
    }

    // ========================================
    // 確率表示用（UI向け）
    // ========================================

    /// <summary>
    /// 指定レアリティの排出確率を取得
    /// </summary>
    public float GetRarityRate(GachaBannerData banner, int rarity)
    {
        if (banner == null || banner.pool == null) return 0f;

        float totalWeight = banner.GetTotalWeight();
        if (totalWeight <= 0f) return 0f;

        float rarityWeight = 0f;
        foreach (var entry in banner.pool)
        {
            if (entry.Rarity == rarity)
            {
                rarityWeight += entry.weight;
            }
        }

        return rarityWeight / totalWeight * 100f;
    }

    /// <summary>
    /// 確率表示用テキストを生成
    /// </summary>
    public string GetRateDisplayText(GachaBannerData banner)
    {
        if (banner == null) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("【排出確率】");

        for (int r = 6; r >= 1; r--)
        {
            float rate = GetRarityRate(banner, r);
            if (rate > 0f)
            {
                sb.AppendLine($"★{r}: {rate:F2}%");
            }
        }

        return sb.ToString();
    }

    // ========================================
    // セーブ/ロード用
    // ========================================

    public Dictionary<string, int> GetPityData()
    {
        return new Dictionary<string, int>(_pityCounters);
    }

    public void SetPityData(Dictionary<string, int> data)
    {
        _pityCounters.Clear();
        if (data != null)
        {
            foreach (var kvp in data)
            {
                _pityCounters[kvp.Key] = kvp.Value;
            }
        }
    }
}