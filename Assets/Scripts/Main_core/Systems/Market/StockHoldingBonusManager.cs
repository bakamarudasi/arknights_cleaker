using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 株式保有ボーナス管理システム
/// 各株の保有率を計算し、対応するボーナスを適用
/// </summary>
public class StockHoldingBonusManager : MonoBehaviour
{
    public static StockHoldingBonusManager Instance { get; private set; }

    // ========================================
    // イベント
    // ========================================
    public event Action<string, List<StockHoldingBonus>> OnBonusesChanged;
    public event Action OnAllBonusesRecalculated;

    // ========================================
    // キャッシュ
    // ========================================
    // stockId -> アクティブなボーナスリスト
    private Dictionary<string, List<StockHoldingBonus>> activeBonuses = new();

    // stockId -> 保有率
    private Dictionary<string, float> holdingRates = new();

    // 集計されたボーナス効果
    private Dictionary<HoldingBonusType, float> aggregatedBonuses = new();

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // PortfolioManagerのイベントを購読
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnHoldingChanged += OnHoldingChanged;
        }

        // 初期計算
        RecalculateAllBonuses();
    }

    private void OnDestroy()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnHoldingChanged -= OnHoldingChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ========================================
    // ポートフォリオ変更時
    // ========================================

    private void OnHoldingChanged(string stockId)
    {
        RecalculateBonusesForStock(stockId);
        AggregateAllBonuses();
    }

    // ========================================
    // 保有率計算
    // ========================================

    /// <summary>
    /// 特定の株の保有率を取得
    /// </summary>
    public float GetHoldingRate(string stockId)
    {
        if (holdingRates.TryGetValue(stockId, out float rate))
        {
            return rate;
        }
        return 0f;
    }

    /// <summary>
    /// 特定の株の保有率を計算
    /// </summary>
    private float CalculateHoldingRate(string stockId, StockData stockData)
    {
        if (stockData == null || stockData.totalShares <= 0) return 0f;

        int holdings = PortfolioManager.Instance?.GetHoldingQuantity(stockId) ?? 0;

        return (float)holdings / stockData.totalShares;
    }

    // ========================================
    // ボーナス計算
    // ========================================

    /// <summary>
    /// 全ての株のボーナスを再計算
    /// </summary>
    public void RecalculateAllBonuses()
    {
        activeBonuses.Clear();
        holdingRates.Clear();

        if (MarketManager.Instance?.stockDatabase?.stocks == null) return;

        foreach (var stock in MarketManager.Instance.stockDatabase.stocks)
        {
            if (stock == null) continue;
            RecalculateBonusesForStock(stock.stockId);
        }

        AggregateAllBonuses();
        OnAllBonusesRecalculated?.Invoke();
    }

    /// <summary>
    /// 特定の株のボーナスを再計算
    /// </summary>
    private void RecalculateBonusesForStock(string stockId)
    {
        if (string.IsNullOrEmpty(stockId)) return;

        var stockData = MarketManager.Instance?.GetStockData(stockId);
        if (stockData == null) return;

        // 保有率を計算
        float rate = CalculateHoldingRate(stockId, stockData);
        holdingRates[stockId] = rate;

        // アクティブなボーナスをリストアップ
        var bonuses = new List<StockHoldingBonus>();

        if (stockData.holdingBonuses != null)
        {
            foreach (var bonus in stockData.holdingBonuses)
            {
                if (rate >= bonus.requiredHoldingRate)
                {
                    bonuses.Add(bonus);
                }
            }
        }

        activeBonuses[stockId] = bonuses;
        OnBonusesChanged?.Invoke(stockId, bonuses);
    }

    /// <summary>
    /// 全ボーナスを集計
    /// </summary>
    private void AggregateAllBonuses()
    {
        aggregatedBonuses.Clear();

        foreach (var kvp in activeBonuses)
        {
            foreach (var bonus in kvp.Value)
            {
                if (!aggregatedBonuses.ContainsKey(bonus.bonusType))
                {
                    aggregatedBonuses[bonus.bonusType] = 0f;
                }
                aggregatedBonuses[bonus.bonusType] += bonus.effectValue;
            }
        }
    }

    // ========================================
    // ボーナス取得API
    // ========================================

    /// <summary>
    /// 特定タイプの集計ボーナス値を取得
    /// </summary>
    public float GetAggregatedBonus(HoldingBonusType type)
    {
        if (aggregatedBonuses.TryGetValue(type, out float value))
        {
            return value;
        }
        return 0f;
    }

    /// <summary>
    /// 強化費用の軽減率を取得 (0.1 = 10%オフ)
    /// </summary>
    public float GetUpgradeCostReduction()
    {
        return GetAggregatedBonus(HoldingBonusType.UpgradeCostReduction);
    }

    /// <summary>
    /// クリック効率ボーナスを取得 (0.1 = 10%アップ)
    /// </summary>
    public float GetClickEfficiencyBonus()
    {
        return GetAggregatedBonus(HoldingBonusType.ClickEfficiency);
    }

    /// <summary>
    /// 自動収入ボーナスを取得 (0.1 = 10%アップ)
    /// </summary>
    public float GetAutoIncomeBonus()
    {
        return GetAggregatedBonus(HoldingBonusType.AutoIncomeBoost);
    }

    /// <summary>
    /// クリティカル率ボーナスを取得
    /// </summary>
    public float GetCriticalRateBonus()
    {
        return GetAggregatedBonus(HoldingBonusType.CriticalRate);
    }

    /// <summary>
    /// 取引手数料軽減率を取得
    /// </summary>
    public float GetTransactionFeeReduction()
    {
        return GetAggregatedBonus(HoldingBonusType.TransactionFeeReduction);
    }

    /// <summary>
    /// 配当ボーナス率を取得
    /// </summary>
    public float GetDividendBonus()
    {
        return GetAggregatedBonus(HoldingBonusType.DividendBonus);
    }

    // ========================================
    // アップグレード連動
    // ========================================

    /// <summary>
    /// アップグレードの効果倍率を計算（株式保有率に基づく）
    /// </summary>
    public float GetUpgradeEffectMultiplier(UpgradeData upgrade)
    {
        if (upgrade == null || upgrade.relatedStock == null || !upgrade.scaleWithHolding)
        {
            return 1f;
        }

        float rate = GetHoldingRate(upgrade.relatedStock.stockId);
        // 保有率0% = 1倍, 100% = maxHoldingMultiplier倍
        return 1f + (upgrade.maxHoldingMultiplier - 1f) * rate;
    }

    /// <summary>
    /// アップグレードのコストに株式ボーナスを適用
    /// </summary>
    public double ApplyUpgradeCostBonus(double baseCost, UpgradeData upgrade)
    {
        // 全体の軽減率
        float reduction = GetUpgradeCostReduction();

        // 特定企業の株を持っている場合の追加軽減
        if (upgrade?.relatedStock != null)
        {
            float rate = GetHoldingRate(upgrade.relatedStock.stockId);
            // 保有率10%ごとに1%追加軽減（最大10%）
            reduction += Mathf.Min(rate * 0.1f, 0.1f);
        }

        // 軽減率は最大50%
        reduction = Mathf.Min(reduction, 0.5f);

        return baseCost * (1 - reduction);
    }

    // ========================================
    // 特定株のボーナス取得
    // ========================================

    /// <summary>
    /// 特定の株でアクティブなボーナスを取得
    /// </summary>
    public List<StockHoldingBonus> GetActiveBonuses(string stockId)
    {
        if (activeBonuses.TryGetValue(stockId, out var bonuses))
        {
            return new List<StockHoldingBonus>(bonuses);
        }
        return new List<StockHoldingBonus>();
    }

    /// <summary>
    /// 特定の株の次のボーナス目標を取得
    /// </summary>
    public (StockHoldingBonus nextBonus, float requiredRate)? GetNextBonusTarget(string stockId)
    {
        var stockData = MarketManager.Instance?.GetStockData(stockId);
        if (stockData?.holdingBonuses == null || stockData.holdingBonuses.Count == 0)
        {
            return null;
        }

        float currentRate = GetHoldingRate(stockId);

        foreach (var bonus in stockData.holdingBonuses)
        {
            if (currentRate < bonus.requiredHoldingRate)
            {
                return (bonus, bonus.requiredHoldingRate);
            }
        }

        return null; // 全てのボーナスを達成済み
    }

    // ========================================
    // デバッグ
    // ========================================

    public void LogAllBonuses()
    {
        Debug.Log("=== Stock Holding Bonuses ===");
        foreach (var kvp in holdingRates)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value * 100:F2}% holding");
            if (activeBonuses.TryGetValue(kvp.Key, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    Debug.Log($"  - {bonus.bonusType}: {bonus.effectValue * 100:F1}%");
                }
            }
        }
        Debug.Log("=== Aggregated Bonuses ===");
        foreach (var kvp in aggregatedBonuses)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value * 100:F1}%");
        }
    }
}
