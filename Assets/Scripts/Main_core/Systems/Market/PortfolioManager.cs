using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// プレイヤーのポートフォリオ（保有株）を管理
/// 売買処理、含み損益計算を担当
/// </summary>
public class PortfolioManager : MonoBehaviour
{
    // ========================================
    // シングルトン
    // ========================================
    public static PortfolioManager Instance { get; private set; }

    // ========================================
    // 依存関係（Inspector注入）
    // ========================================
    [Header("依存関係")]
    [SerializeField] private StockDatabase stockDatabase;

    // ========================================
    // ランタイム状態
    // ========================================
    private Dictionary<string, StockHolding> holdings = new();

    // ========================================
    // イベント
    // ========================================
    public event Action<string> OnHoldingChanged; // stockId
    public event Action OnPortfolioUpdated;

    // ========================================
    // プロパティ
    // ========================================

    /// <summary>
    /// 全保有銘柄
    /// </summary>
    public IReadOnlyDictionary<string, StockHolding> Holdings => holdings;

    /// <summary>
    /// ポートフォリオの総評価額
    /// </summary>
    public double TotalValue
    {
        get
        {
            double total = 0;
            foreach (var holding in holdings.Values)
            {
                double currentPrice = MarketManager.Instance?.GetCurrentPrice(holding.stockId) ?? 0;
                total += currentPrice * holding.quantity;
            }
            return total;
        }
    }

    /// <summary>
    /// ポートフォリオの総含み損益
    /// </summary>
    public double TotalUnrealizedProfitLoss
    {
        get
        {
            double total = 0;
            foreach (var holding in holdings.Values)
            {
                total += GetUnrealizedProfitLoss(holding.stockId);
            }
            return total;
        }
    }

    // ========================================
    // Unity ライフサイクル
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========================================
    // 売買API
    // ========================================

    /// <summary>
    /// 株を購入
    /// </summary>
    /// <param name="stockId">銘柄ID</param>
    /// <param name="quantity">購入数量</param>
    /// <returns>成功したか</returns>
    public bool TryBuyStock(string stockId, int quantity)
    {
        if (quantity <= 0) return false;

        var stock = stockDatabase?.GetByStockId(stockId);
        if (stock == null)
        {
            Debug.LogWarning($"[Portfolio] Stock not found: {stockId}");
            return false;
        }

        if (!stock.IsUnlocked())
        {
            Debug.LogWarning($"[Portfolio] Stock is locked: {stockId}");
            return false;
        }

        // 現在価格を取得
        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? stock.initialPrice;
        double totalCost = stock.CalculateBuyCost(currentPrice, quantity);

        // 残高チェック
        if (WalletManager.Instance == null)
        {
            Debug.LogWarning("[Portfolio] WalletManager not found");
            return false;
        }

        if (!WalletManager.Instance.CanAffordMoney(totalCost))
        {
            Debug.Log($"[Portfolio] Insufficient funds: need {totalCost:F0}, have {WalletManager.Instance.Money:F0}");
            return false;
        }

        // 購入実行
        WalletManager.Instance.SpendMoney(totalCost);

        // 保有を更新
        if (!holdings.TryGetValue(stockId, out var holding))
        {
            holding = new StockHolding { stockId = stockId };
            holdings[stockId] = holding;
        }

        // 平均取得単価を更新
        double oldTotalCost = holding.averageCost * holding.quantity;
        double newTotalCost = oldTotalCost + (currentPrice * quantity);
        holding.quantity += quantity;
        holding.averageCost = holding.quantity > 0 ? newTotalCost / holding.quantity : 0;

        // イベント発火
        MarketEventBus.PublishStockBought(stockId, quantity, totalCost);
        OnHoldingChanged?.Invoke(stockId);
        OnPortfolioUpdated?.Invoke();

        Debug.Log($"[Portfolio] Bought {quantity} {stockId} @ {currentPrice:F2} (Total: {totalCost:F0} LMD)");
        return true;
    }

    /// <summary>
    /// 株を売却
    /// </summary>
    /// <param name="stockId">銘柄ID</param>
    /// <param name="quantity">売却数量</param>
    /// <returns>成功したか</returns>
    public bool TrySellStock(string stockId, int quantity)
    {
        if (quantity <= 0) return false;

        if (!holdings.TryGetValue(stockId, out var holding) || holding.quantity < quantity)
        {
            Debug.LogWarning($"[Portfolio] Insufficient holdings: {stockId}");
            return false;
        }

        var stock = stockDatabase?.GetByStockId(stockId);
        if (stock == null) return false;

        // 現在価格を取得
        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? stock.initialPrice;
        double totalReturn = stock.CalculateSellReturn(currentPrice, quantity);

        // 損益計算
        double costBasis = holding.averageCost * quantity;
        double profitLoss = totalReturn - costBasis;

        // 売却実行
        WalletManager.Instance?.AddMoney(totalReturn);

        holding.quantity -= quantity;
        if (holding.quantity <= 0)
        {
            holdings.Remove(stockId);
        }

        // イベント発火
        MarketEventBus.PublishStockSold(stockId, quantity, totalReturn, profitLoss);
        OnHoldingChanged?.Invoke(stockId);
        OnPortfolioUpdated?.Invoke();

        string resultText = profitLoss >= 0 ? $"+{profitLoss:F0} 利確 🚀" : $"{profitLoss:F0} 損切り 💀";
        Debug.Log($"[Portfolio] Sold {quantity} {stockId} @ {currentPrice:F2} ({resultText})");

        return true;
    }

    /// <summary>
    /// 全株を売却
    /// </summary>
    public bool TrySellAll(string stockId)
    {
        if (!holdings.TryGetValue(stockId, out var holding)) return false;
        return TrySellStock(stockId, holding.quantity);
    }

    // ========================================
    // 情報取得API
    // ========================================

    /// <summary>
    /// 指定銘柄の保有数を取得
    /// </summary>
    public int GetHoldingQuantity(string stockId)
    {
        return holdings.TryGetValue(stockId, out var holding) ? holding.quantity : 0;
    }

    /// <summary>
    /// 指定銘柄の平均取得単価を取得
    /// </summary>
    public double GetAverageCost(string stockId)
    {
        return holdings.TryGetValue(stockId, out var holding) ? holding.averageCost : 0;
    }

    /// <summary>
    /// 指定銘柄の含み損益を取得
    /// </summary>
    public double GetUnrealizedProfitLoss(string stockId)
    {
        if (!holdings.TryGetValue(stockId, out var holding)) return 0;

        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? 0;
        double currentValue = currentPrice * holding.quantity;
        double costBasis = holding.averageCost * holding.quantity;

        return currentValue - costBasis;
    }

    /// <summary>
    /// 指定銘柄の含み損益率を取得
    /// </summary>
    public double GetUnrealizedProfitLossRate(string stockId)
    {
        if (!holdings.TryGetValue(stockId, out var holding)) return 0;
        if (holding.averageCost <= 0) return 0;

        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? 0;
        return (currentPrice - holding.averageCost) / holding.averageCost;
    }

    /// <summary>
    /// 含み益があるか（利確ボタン用）
    /// </summary>
    public bool HasProfit(string stockId)
    {
        return GetUnrealizedProfitLoss(stockId) > 0;
    }

    /// <summary>
    /// 含み損があるか（損切りボタン用）
    /// </summary>
    public bool HasLoss(string stockId)
    {
        return GetUnrealizedProfitLoss(stockId) < 0;
    }

    /// <summary>
    /// 購入可能な最大数量を計算
    /// </summary>
    public int GetMaxBuyableQuantity(string stockId)
    {
        var stock = stockDatabase?.GetByStockId(stockId);
        if (stock == null)
        {
            Debug.LogWarning($"[Portfolio] GetMaxBuyableQuantity: stock '{stockId}' not found in database");
            return 0;
        }

        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? stock.initialPrice;
        double money = WalletManager.Instance?.Money ?? 0;

        // 手数料込みの1株あたりコスト
        double costPerShare = currentPrice * (1 + stock.transactionFee);

        int maxQty = (int)(money / costPerShare);
        Debug.Log($"[Portfolio] GetMaxBuyableQuantity: stockId={stockId}, price={currentPrice}, money={money}, fee={stock.transactionFee}, costPerShare={costPerShare}, maxQty={maxQty}");

        return maxQty;
    }

    /// <summary>
    /// 保有銘柄のサマリーを取得
    /// </summary>
    public List<HoldingSummary> GetHoldingSummaries()
    {
        var summaries = new List<HoldingSummary>();

        foreach (var holding in holdings.Values)
        {
            var stock = stockDatabase?.GetByStockId(holding.stockId);
            if (stock == null) continue;

            double currentPrice = MarketManager.Instance?.GetCurrentPrice(holding.stockId) ?? 0;
            double pnl = GetUnrealizedProfitLoss(holding.stockId);
            double pnlRate = GetUnrealizedProfitLossRate(holding.stockId);

            summaries.Add(new HoldingSummary
            {
                stockId = holding.stockId,
                companyName = stock.companyName,
                quantity = holding.quantity,
                averageCost = holding.averageCost,
                currentPrice = currentPrice,
                currentValue = currentPrice * holding.quantity,
                unrealizedPnL = pnl,
                unrealizedPnLRate = pnlRate
            });
        }

        return summaries.OrderByDescending(s => s.currentValue).ToList();
    }

    // ========================================
    // セーブ/ロード
    // ========================================

    public PortfolioSaveData GetSaveData()
    {
        return new PortfolioSaveData
        {
            holdings = holdings.Values.ToList()
        };
    }

    public void LoadSaveData(PortfolioSaveData data)
    {
        holdings.Clear();
        if (data?.holdings != null)
        {
            foreach (var holding in data.holdings)
            {
                holdings[holding.stockId] = holding;
            }
        }
        OnPortfolioUpdated?.Invoke();
    }
}

/// <summary>
/// 保有株データ
/// </summary>
[Serializable]
public class StockHolding
{
    public string stockId;
    public int quantity;
    public double averageCost; // 平均取得単価
}

/// <summary>
/// 保有株のサマリー（UI表示用）
/// </summary>
public struct HoldingSummary
{
    public string stockId;
    public string companyName;
    public int quantity;
    public double averageCost;
    public double currentPrice;
    public double currentValue;
    public double unrealizedPnL;
    public double unrealizedPnLRate;
}

/// <summary>
/// ポートフォリオのセーブデータ
/// </summary>
[Serializable]
public class PortfolioSaveData
{
    public List<StockHolding> holdings;
}
