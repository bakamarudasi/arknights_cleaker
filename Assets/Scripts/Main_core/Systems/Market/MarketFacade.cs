using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// マーケットUI用ファサード実装
/// 各シングルトンManagerへのアクセスを集約
/// </summary>
public class MarketFacade : IMarketFacade
{
    // シングルトンインスタンス（遅延取得）
    private static MarketFacade _instance;
    public static MarketFacade Instance => _instance ??= new MarketFacade();

    // ========================================
    // 資産情報
    // ========================================

    public double Money => WalletManager.Instance?.Money ?? 0;

    public double TotalPortfolioValue => PortfolioManager.Instance?.TotalValue ?? 0;

    public double TotalUnrealizedPnL => PortfolioManager.Instance?.TotalUnrealizedProfitLoss ?? 0;

    // ========================================
    // 株価情報
    // ========================================

    public List<StockData> GetUnlockedStocks()
    {
        return MarketManager.Instance?.GetUnlockedStocks() ?? new List<StockData>();
    }

    public double GetCurrentPrice(string stockId)
    {
        return MarketManager.Instance?.GetCurrentPrice(stockId) ?? 0;
    }

    public StockState GetStockState(string stockId)
    {
        return MarketManager.Instance?.GetStockState(stockId);
    }

    public double[] GetPriceHistory(string stockId)
    {
        return MarketManager.Instance?.GetPriceHistory(stockId);
    }

    public StockData GetStockData(string stockId)
    {
        return MarketManager.Instance?.stockDatabase?.GetByStockId(stockId);
    }

    // ========================================
    // ポートフォリオ情報
    // ========================================

    public List<HoldingSummary> GetHoldingSummaries()
    {
        return PortfolioManager.Instance?.GetHoldingSummaries() ?? new List<HoldingSummary>();
    }

    public int GetHoldingQuantity(string stockId)
    {
        return PortfolioManager.Instance?.GetHoldingQuantity(stockId) ?? 0;
    }

    public int GetMaxBuyableQuantity(string stockId)
    {
        return PortfolioManager.Instance?.GetMaxBuyableQuantity(stockId) ?? 0;
    }

    public bool HasProfit(string stockId)
    {
        return PortfolioManager.Instance?.HasProfit(stockId) ?? false;
    }

    public bool HasLoss(string stockId)
    {
        return PortfolioManager.Instance?.HasLoss(stockId) ?? false;
    }

    public double GetAverageCost(string stockId)
    {
        return PortfolioManager.Instance?.GetAverageCost(stockId) ?? 0;
    }

    // ========================================
    // 売買操作
    // ========================================

    public bool TryBuyStock(string stockId, int quantity)
    {
        return PortfolioManager.Instance?.TryBuyStock(stockId, quantity) ?? false;
    }

    public bool TrySellStock(string stockId, int quantity)
    {
        return PortfolioManager.Instance?.TrySellStock(stockId, quantity) ?? false;
    }

    // ========================================
    // スキル操作
    // ========================================

    public void ApplyBuySupport(string stockId, int clickCount)
    {
        MarketManager.Instance?.ApplyBuySupport(stockId, clickCount);
    }

    // ========================================
    // ロドス株情報
    // ========================================

    public string GetRhodosPriceText()
    {
        return RhodosStockManager.Instance?.GetPriceText() ?? "---";
    }

    public RhodosStockRank GetRhodosRank()
    {
        return RhodosStockManager.Instance?.CurrentRank ?? RhodosStockRank.Normal;
    }

    public string GetRhodosDividendTimerText()
    {
        return RhodosStockManager.Instance?.GetDividendTimerText() ?? "--:--";
    }

    // ========================================
    // チュートリアル
    // ========================================

    public bool TryStartTutorial(string sequenceId, VisualElement root)
    {
        return TutorialManager.Instance?.TryStartTutorial(sequenceId, root) ?? false;
    }

    // ========================================
    // フォーマッタ
    // ========================================

    public string FormatPrice(double price)
    {
        return StockPriceEngine.FormatPrice(price);
    }

    public string FormatChangeRate(double rate)
    {
        return StockPriceEngine.FormatChangeRate(rate);
    }
}

/// <summary>
/// マーケットUI用イベントハブ実装
/// 個別ManagerのイベントをMarketEventBus経由で統合
/// </summary>
public class MarketEventHub : IMarketEventHub
{
    private bool isSubscribed = false;

    // ========================================
    // イベント（外部公開用）
    // ========================================

    public event Action<StockPriceSnapshot> OnPriceUpdated;
    public event Action<string, double> OnPriceCrash;
    public event Action<string, int, double> OnStockBought;
    public event Action<string, int, double, double> OnStockSold;
    public event Action<MarketNews> OnNewsGenerated;
    public event Action<double> OnMoneyChanged;
    public event Action OnPortfolioUpdated;
    public event Action<DividendPayment> OnDividendPaid;

    // ========================================
    // 購読管理
    // ========================================

    public void Subscribe()
    {
        if (isSubscribed) return;

        // MarketEventBusからの転送
        MarketEventBus.OnPriceUpdated += HandlePriceUpdated;
        MarketEventBus.OnPriceCrash += HandlePriceCrash;
        MarketEventBus.OnStockBought += HandleStockBought;
        MarketEventBus.OnStockSold += HandleStockSold;
        MarketEventBus.OnNewsGenerated += HandleNewsGenerated;
        MarketEventBus.OnDividendPaid += HandleDividendPaid;

        // 個別Managerからの転送
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        }

        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += HandlePortfolioUpdated;
        }

        isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (!isSubscribed) return;

        // MarketEventBusからの解除
        MarketEventBus.OnPriceUpdated -= HandlePriceUpdated;
        MarketEventBus.OnPriceCrash -= HandlePriceCrash;
        MarketEventBus.OnStockBought -= HandleStockBought;
        MarketEventBus.OnStockSold -= HandleStockSold;
        MarketEventBus.OnNewsGenerated -= HandleNewsGenerated;
        MarketEventBus.OnDividendPaid -= HandleDividendPaid;

        // 個別Managerからの解除
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }

        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated -= HandlePortfolioUpdated;
        }

        isSubscribed = false;
    }

    // ========================================
    // イベントハンドラ（転送）
    // ========================================

    private void HandlePriceUpdated(StockPriceSnapshot snapshot) => OnPriceUpdated?.Invoke(snapshot);
    private void HandlePriceCrash(string stockId, double rate) => OnPriceCrash?.Invoke(stockId, rate);
    private void HandleStockBought(string stockId, int qty, double cost) => OnStockBought?.Invoke(stockId, qty, cost);
    private void HandleStockSold(string stockId, int qty, double ret, double pnl) => OnStockSold?.Invoke(stockId, qty, ret, pnl);
    private void HandleNewsGenerated(MarketNews news) => OnNewsGenerated?.Invoke(news);
    private void HandleMoneyChanged(double amount) => OnMoneyChanged?.Invoke(amount);
    private void HandlePortfolioUpdated() => OnPortfolioUpdated?.Invoke();
    private void HandleDividendPaid(DividendPayment payment) => OnDividendPaid?.Invoke(payment);
}
