using System;
using System.Collections.Generic;

/// <summary>
/// マーケットUI用ファサードインターフェース
/// 複数のManagerへの依存を隠蔽し、疎結合を実現
/// </summary>
public interface IMarketFacade
{
    // ========================================
    // 資産情報
    // ========================================

    /// <summary>現在の所持金（LMD）</summary>
    double Money { get; }

    /// <summary>ポートフォリオの総評価額</summary>
    double TotalPortfolioValue { get; }

    /// <summary>ポートフォリオの総含み損益</summary>
    double TotalUnrealizedPnL { get; }

    // ========================================
    // 株価情報
    // ========================================

    /// <summary>解放済み銘柄リストを取得</summary>
    List<StockData> GetUnlockedStocks();

    /// <summary>現在価格を取得</summary>
    double GetCurrentPrice(string stockId);

    /// <summary>株価状態を取得</summary>
    StockRuntimeData GetStockState(string stockId);

    /// <summary>価格履歴を取得</summary>
    double[] GetPriceHistory(string stockId);

    /// <summary>銘柄データを取得</summary>
    StockData GetStockData(string stockId);

    // ========================================
    // ポートフォリオ情報
    // ========================================

    /// <summary>保有株サマリーを取得</summary>
    List<HoldingSummary> GetHoldingSummaries();

    /// <summary>指定銘柄の保有数を取得</summary>
    int GetHoldingQuantity(string stockId);

    /// <summary>購入可能な最大数量を取得</summary>
    int GetMaxBuyableQuantity(string stockId);

    /// <summary>含み益があるか</summary>
    bool HasProfit(string stockId);

    /// <summary>含み損があるか</summary>
    bool HasLoss(string stockId);

    /// <summary>平均取得単価を取得</summary>
    double GetAverageCost(string stockId);

    // ========================================
    // 売買操作
    // ========================================

    /// <summary>株を購入</summary>
    bool TryBuyStock(string stockId, int quantity);

    /// <summary>株を売却</summary>
    bool TrySellStock(string stockId, int quantity);

    // ========================================
    // スキル操作
    // ========================================

    /// <summary>物理買い支えを実行</summary>
    void ApplyBuySupport(string stockId, int clickCount);

    // ========================================
    // ロドス株情報
    // ========================================

    /// <summary>ロドス株の価格テキスト</summary>
    string GetRhodosPriceText();

    /// <summary>ロドス株のランク</summary>
    RhodosStockRank GetRhodosRank();

    /// <summary>ロドス株の配当タイマーテキスト</summary>
    string GetRhodosDividendTimerText();

    // ========================================
    // チュートリアル
    // ========================================

    /// <summary>チュートリアルを開始（未完了時のみ）</summary>
    bool TryStartTutorial(string sequenceId, UnityEngine.UIElements.VisualElement root);

    // ========================================
    // フォーマッタ
    // ========================================

    /// <summary>価格をフォーマット</summary>
    string FormatPrice(double price);

    /// <summary>変化率をフォーマット</summary>
    string FormatChangeRate(double rate);
}

/// <summary>
/// マーケットUI用イベントハブ
/// 個別Managerのイベントを統合して購読しやすくする
/// </summary>
public interface IMarketEventHub
{
    // 株価
    event Action<StockPriceSnapshot> OnPriceUpdated;
    event Action<string, double> OnPriceCrash;

    // 取引
    event Action<string, int, double> OnStockBought;
    event Action<string, int, double, double> OnStockSold;

    // ニュース
    event Action<MarketNews> OnNewsGenerated;

    // 資産
    event Action<double> OnMoneyChanged;
    event Action OnPortfolioUpdated;

    // ロドス株
    event Action<DividendPayment> OnDividendPaid;

    // 登録/解除
    void Subscribe();
    void Unsubscribe();
}
