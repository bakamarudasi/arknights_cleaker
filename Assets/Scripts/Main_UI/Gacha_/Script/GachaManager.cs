using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ガチャのロジック（排出計算、天井管理）を担当するマネージャー
/// </summary>
public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    private const string LOG_TAG = "[GachaManager]";

    // ========================================
    // データベース
    // ========================================

    [Header("データベース")]
    [SerializeField] private GachaDatabase _database;

    /// <summary>ガチャデータベース（外部参照用）</summary>
    public GachaDatabase Database => _database;

    // ========================================
    // 天井カウント（バナーID → カウント）
    // ========================================

    [Header("天井データ (Debug)")]
    [SerializeField] private SerializableDictionary<string, int> _pityCounters = new();

    // ========================================
    // 封入数在庫（バナーID → アイテムID → 残り在庫）
    // ========================================

    [Header("封入在庫データ (Debug)")]
    [SerializeField] private SerializableDictionary<string, SerializableDictionary<string, int>> _stockData = new();

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
    // 安全なイベント発火ヘルパー
    // ========================================

    private void SafeInvoke<T>(Action<T> action, T arg, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke(arg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LOG_TAG} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void SafeInvoke<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke(arg1, arg2);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LOG_TAG} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
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

        SafeInvoke(OnGachaPulled, banner, results, nameof(OnGachaPulled));
        SafeInvoke(OnGachaCountIncremented, count, nameof(OnGachaCountIncremented));

        return results;
    }

    /// <summary>
    /// 単発ガチャ（内部処理）
    /// </summary>
    private GachaResultItem PullSingle(GachaBannerData banner)
    {
        // 在庫チェック（全て在庫切れなら引けない）
        if (banner.HasLimitedStock() && !HasStock(banner))
        {
            Debug.LogWarning("GachaManager: Banner is sold out");
            return null;
        }

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
            SafeInvoke(OnPityReached, banner, nameof(OnPityReached));
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

        // 在庫消費
        if (selectedEntry.HasStockLimit && selectedEntry.item != null)
        {
            ConsumeStock(banner, selectedEntry.item.id);
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
            SafeInvoke(OnHighRarityPulled, result, selectedEntry.Rarity, nameof(OnHighRarityPulled));
        }

        return result;
    }

    // ========================================
    // 抽選ロジック
    // ========================================

    /// <summary>
    /// プールから抽選（ソフト天井・在庫考慮）
    /// </summary>
    private GachaPoolEntry SelectFromPool(GachaBannerData banner, int currentPity)
    {
        var pool = banner.pool;
        float totalWeight = 0f;

        // 在庫初期化
        InitializeStock(banner);

        // 重みを計算（ソフト天井補正・在庫考慮）
        var adjustedWeights = new List<float>();

        foreach (var entry in pool)
        {
            // weightが0以下の場合はデフォルト値1を使用（設定ミス対策）
            float weight = entry.weight > 0 ? entry.weight : 1f;

            // 在庫チェック：在庫切れは重み0
            if (entry.HasStockLimit && entry.item != null)
            {
                int remaining = GetRemainingStock(banner, entry.item.id);
                if (remaining == 0)
                {
                    weight = 0f;
                }
            }

            // ソフト天井：高レアの確率を徐々に上昇
            if (weight > 0 && banner.hasPity && currentPity >= banner.softPityStart && entry.Rarity >= 5)
            {
                float pityProgress = (float)(currentPity - banner.softPityStart) / (banner.pityCount - banner.softPityStart);
                weight *= (1f + pityProgress * 5f); // 最大6倍
            }

            // ピックアップ補正
            if (weight > 0 && entry.isPickup && banner.pickupItems.Contains(entry.item))
            {
                weight *= (1f + banner.pickupRateBoost);
            }

            adjustedWeights.Add(weight);
            totalWeight += weight;
        }

        // 全て在庫切れの場合はnull
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("GachaManager: All items are out of stock");
            return null;
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

        // フォールバック（最後の在庫ありエントリを返す）
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (adjustedWeights[i] > 0)
                return pool[i];
        }
        return null;
    }

    /// <summary>
    /// 最高レアリティのエントリを取得（天井用・在庫考慮）
    /// </summary>
    private GachaPoolEntry GetHighestRarityEntry(GachaBannerData banner)
    {
        GachaPoolEntry highest = null;
        int maxRarity = 0;

        // ピックアップ優先で最高レアを探す（在庫考慮）
        foreach (var entry in banner.pool)
        {
            // 在庫チェック
            if (entry.HasStockLimit && entry.item != null)
            {
                int remaining = GetRemainingStock(banner, entry.item.id);
                if (remaining == 0) continue; // 在庫切れはスキップ
            }

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

        var inventory = GameController.Instance?.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning($"{LOG_TAG} CheckIsNew: InventoryManager not available, assuming item is new");
            return true;
        }

        return !inventory.Has(item.id);
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
    // 封入数（在庫）管理
    // ========================================

    /// <summary>
    /// バナーの在庫を初期化（初回アクセス時、または新しいアイテム追加時）
    /// </summary>
    private void InitializeStock(GachaBannerData banner)
    {
        if (banner == null || string.IsNullOrEmpty(banner.bannerId)) return;

        // バナーの在庫データがなければ作成
        if (!_stockData.ContainsKey(banner.bannerId))
        {
            _stockData[banner.bannerId] = new SerializableDictionary<string, int>();
        }

        var bannerStock = _stockData[banner.bannerId];

        // 各エントリについて、在庫データがなければ初期化（新しいアイテム対応）
        foreach (var entry in banner.pool)
        {
            if (entry.item != null && entry.HasStockLimit)
            {
                // 既に在庫データがあればそのまま維持、なければ初期化
                if (!bannerStock.ContainsKey(entry.item.id))
                {
                    bannerStock[entry.item.id] = entry.stockCount;
                }
            }
        }
    }

    /// <summary>
    /// アイテムの残り在庫を取得
    /// </summary>
    public int GetRemainingStock(GachaBannerData banner, string itemId)
    {
        if (banner == null || string.IsNullOrEmpty(itemId)) return -1;

        InitializeStock(banner);

        if (_stockData.TryGetValue(banner.bannerId, out var bannerStock))
        {
            if (bannerStock.TryGetValue(itemId, out int remaining))
            {
                return remaining;
            }
        }
        return -1; // 無制限
    }

    /// <summary>
    /// アイテムの在庫を消費
    /// </summary>
    private bool ConsumeStock(GachaBannerData banner, string itemId)
    {
        if (banner == null || string.IsNullOrEmpty(itemId)) return false;

        if (_stockData.TryGetValue(banner.bannerId, out var bannerStock))
        {
            if (bannerStock.TryGetValue(itemId, out int remaining))
            {
                if (remaining > 0)
                {
                    bannerStock[itemId] = remaining - 1;
                    return true;
                }
                return false; // 在庫切れ
            }
        }
        return true; // 無制限アイテム
    }

    /// <summary>
    /// バナー全体の残り在庫を取得
    /// </summary>
    public (int remaining, int total) GetBannerStockProgress(GachaBannerData banner)
    {
        if (banner == null || !banner.HasLimitedStock()) return (-1, -1);

        InitializeStock(banner);

        int remaining = 0;
        int total = banner.GetTotalStock();

        if (_stockData.TryGetValue(banner.bannerId, out var bannerStock))
        {
            foreach (var kvp in bannerStock)
            {
                remaining += kvp.Value;
            }
        }

        return (remaining, total);
    }

    /// <summary>
    /// 在庫が残っているかチェック
    /// </summary>
    public bool HasStock(GachaBannerData banner)
    {
        if (banner == null || !banner.HasLimitedStock()) return true;

        var (remaining, _) = GetBannerStockProgress(banner);
        return remaining > 0;
    }

    /// <summary>
    /// 在庫データをリセット（バナー単位）
    /// </summary>
    public void ResetStock(GachaBannerData banner)
    {
        if (banner == null || string.IsNullOrEmpty(banner.bannerId)) return;
        _stockData.Remove(banner.bannerId);
        InitializeStock(banner);
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

        var inventory = GameController.Instance?.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning($"{LOG_TAG} IsBannerUnlocked: InventoryManager not available, treating banner as locked");
            return false;
        }

        // アイテムで解放
        if (banner.requiredUnlockItem != null)
        {
            if (inventory.Has(banner.requiredUnlockItem.id))
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

        var inventory = GameController.Instance?.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning($"{LOG_TAG} IsAllPoolItemsOwned: InventoryManager not available");
            return false;
        }

        foreach (var entry in banner.pool)
        {
            if (entry.item == null) continue;
            if (!inventory.Has(entry.item.id))
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

        var inventory = GameController.Instance?.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning($"{LOG_TAG} GetPoolProgress: InventoryManager not available");
            return (0, 0);
        }

        int owned = 0;
        int total = 0;

        foreach (var entry in banner.pool)
        {
            if (entry.item == null) continue;
            total++;
            if (inventory.Has(entry.item.id))
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
        if (banner == null)
        {
            Debug.LogWarning($"{LOG_TAG} PullGacha: banner is null");
            return new List<GachaResultItem>();
        }

        var gc = GameController.Instance;
        if (gc == null)
        {
            Debug.LogError($"{LOG_TAG} PullGacha: GameController.Instance is null");
            return new List<GachaResultItem>();
        }

        var wallet = gc.Wallet;
        var inventory = gc.Inventory;

        if (wallet == null)
        {
            Debug.LogError($"{LOG_TAG} PullGacha: WalletManager is null");
            return new List<GachaResultItem>();
        }

        if (inventory == null)
        {
            Debug.LogError($"{LOG_TAG} PullGacha: InventoryManager is null");
            return new List<GachaResultItem>();
        }

        double cost = banner.GetCost(count);

        // 通貨チェック
        if (!wallet.CanAfford(cost, banner.currencyType))
        {
            Debug.Log($"{LOG_TAG} PullGacha: Insufficient funds for {count} pulls (cost: {cost})");
            return new List<GachaResultItem>();
        }

        // 通貨消費
        if (!wallet.Spend(cost, banner.currencyType))
        {
            Debug.LogWarning($"{LOG_TAG} PullGacha: Failed to spend currency");
            return new List<GachaResultItem>();
        }

        // ガチャ実行
        var results = Pull(banner, count);

        // 結果をインベントリに追加
        foreach (var item in results)
        {
            if (!string.IsNullOrEmpty(item.itemId))
            {
                inventory.Add(item.itemId, 1);
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

    public Dictionary<string, Dictionary<string, int>> GetStockData()
    {
        var result = new Dictionary<string, Dictionary<string, int>>();
        foreach (var banner in _stockData)
        {
            result[banner.Key] = new Dictionary<string, int>(banner.Value);
        }
        return result;
    }

    public void SetStockData(Dictionary<string, Dictionary<string, int>> data)
    {
        _stockData.Clear();
        if (data != null)
        {
            foreach (var banner in data)
            {
                var bannerStock = new SerializableDictionary<string, int>();
                foreach (var item in banner.Value)
                {
                    bannerStock[item.Key] = item.Value;
                }
                _stockData[banner.Key] = bannerStock;
            }
        }
    }
}