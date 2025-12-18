using System;
using System.Collections.Generic;

/// <summary>
/// マーケットシステム用のイベントバス
/// 疎結合を実現するための静的イベント管理
/// </summary>
public static class MarketEventBus
{
    // ========================================
    // 株価関連イベント
    // ========================================

    /// <summary>
    /// 株価が更新された時（毎ティック）
    /// </summary>
    public static event Action<StockPriceSnapshot> OnPriceUpdated;

    /// <summary>
    /// 株価が急騰した時（ジャンプ発生）
    /// </summary>
    public static event Action<string, double> OnPriceSurge; // stockId, changeRate

    /// <summary>
    /// 株価が暴落した時（ジャンプ発生）
    /// </summary>
    public static event Action<string, double> OnPriceCrash; // stockId, changeRate

    // ========================================
    // 取引関連イベント
    // ========================================

    /// <summary>
    /// 株を購入した時
    /// </summary>
    public static event Action<string, int, double> OnStockBought; // stockId, quantity, totalCost

    /// <summary>
    /// 株を売却した時
    /// </summary>
    public static event Action<string, int, double, double> OnStockSold; // stockId, quantity, totalReturn, profitLoss

    /// <summary>
    /// 利確した時（プラス収支で売却）
    /// </summary>
    public static event Action<string, double> OnProfitTaken; // stockId, profit

    /// <summary>
    /// 損切りした時（マイナス収支で売却）
    /// </summary>
    public static event Action<string, double> OnLossCut; // stockId, loss (positive value)

    // ========================================
    // 市場イベント関連
    // ========================================

    /// <summary>
    /// 経済イベントが開始した時（防衛戦など）
    /// </summary>
    public static event Action<MarketEventSnapshot> OnMarketEventStarted;

    /// <summary>
    /// 経済イベントが終了した時
    /// </summary>
    public static event Action<MarketEventSnapshot, bool> OnMarketEventEnded; // eventData, success

    /// <summary>
    /// 敵対的買収が開始した時
    /// </summary>
    public static event Action<TakeoverEventData> OnTakeoverStarted;

    /// <summary>
    /// 敵対的買収が終了した時
    /// </summary>
    public static event Action<TakeoverEventData, bool> OnTakeoverEnded; // eventData, playerWon

    // ========================================
    // ニュース関連イベント
    // ========================================

    /// <summary>
    /// 新しいニュースが発生した時
    /// </summary>
    public static event Action<MarketNews> OnNewsGenerated;

    /// <summary>
    /// インサイダー情報を受け取った時
    /// </summary>
    public static event Action<InsiderTip> OnInsiderTipReceived;

    // ========================================
    // ロドス株関連イベント
    // ========================================

    /// <summary>
    /// ロドス株価が更新された時
    /// </summary>
    public static event Action<double, RhodosStockRank> OnRhodosStockUpdated; // price, rank

    /// <summary>
    /// 配当が支払われた時
    /// </summary>
    public static event Action<DividendPayment> OnDividendPaid;

    // ========================================
    // 解放関連イベント
    // ========================================

    /// <summary>
    /// 新しい銘柄が解放された時
    /// </summary>
    public static event Action<string> OnStockUnlocked; // stockId

    // ========================================
    // イベント発火メソッド
    // ========================================

    public static void PublishPriceUpdated(StockPriceSnapshot snapshot)
    {
        OnPriceUpdated?.Invoke(snapshot);

        // 急騰・暴落判定（±10%以上）
        if (snapshot.changeRate >= 0.10)
        {
            OnPriceSurge?.Invoke(snapshot.stockId, snapshot.changeRate);
        }
        else if (snapshot.changeRate <= -0.10)
        {
            OnPriceCrash?.Invoke(snapshot.stockId, snapshot.changeRate);
        }
    }

    public static void PublishStockBought(string stockId, int quantity, double totalCost)
    {
        OnStockBought?.Invoke(stockId, quantity, totalCost);
    }

    public static void PublishStockSold(string stockId, int quantity, double totalReturn, double profitLoss)
    {
        OnStockSold?.Invoke(stockId, quantity, totalReturn, profitLoss);

        if (profitLoss >= 0)
        {
            OnProfitTaken?.Invoke(stockId, profitLoss);
        }
        else
        {
            OnLossCut?.Invoke(stockId, -profitLoss);
        }
    }

    public static void PublishMarketEventStarted(MarketEventSnapshot data)
    {
        OnMarketEventStarted?.Invoke(data);
    }

    public static void PublishMarketEventEnded(MarketEventSnapshot data, bool success)
    {
        OnMarketEventEnded?.Invoke(data, success);
    }

    public static void PublishTakeoverStarted(TakeoverEventData data)
    {
        OnTakeoverStarted?.Invoke(data);
    }

    public static void PublishTakeoverEnded(TakeoverEventData data, bool playerWon)
    {
        OnTakeoverEnded?.Invoke(data, playerWon);
    }

    public static void PublishNewsGenerated(MarketNews news)
    {
        OnNewsGenerated?.Invoke(news);
    }

    public static void PublishInsiderTipReceived(InsiderTip tip)
    {
        OnInsiderTipReceived?.Invoke(tip);
    }

    public static void PublishRhodosStockUpdated(double price, RhodosStockRank rank)
    {
        OnRhodosStockUpdated?.Invoke(price, rank);
    }

    public static void PublishDividendPaid(DividendPayment payment)
    {
        OnDividendPaid?.Invoke(payment);
    }

    public static void PublishStockUnlocked(string stockId)
    {
        OnStockUnlocked?.Invoke(stockId);
    }

    // ========================================
    // クリーンアップ（テスト・リセット用）
    // ========================================

    public static void ClearAllListeners()
    {
        OnPriceUpdated = null;
        OnPriceSurge = null;
        OnPriceCrash = null;
        OnStockBought = null;
        OnStockSold = null;
        OnProfitTaken = null;
        OnLossCut = null;
        OnMarketEventStarted = null;
        OnMarketEventEnded = null;
        OnTakeoverStarted = null;
        OnTakeoverEnded = null;
        OnNewsGenerated = null;
        OnInsiderTipReceived = null;
        OnRhodosStockUpdated = null;
        OnDividendPaid = null;
        OnStockUnlocked = null;
    }
}

// ========================================
// イベントデータ構造体
// ========================================

/// <summary>
/// マーケットイベント（防衛戦）スナップショット
/// ※ MarketEventData（ScriptableObject）とは別の実行時データ構造
/// </summary>
[Serializable]
public struct MarketEventSnapshot
{
    public string eventId;
    public string targetStockId;
    public string title;
    public string description;
    public float duration;        // 秒
    public float requiredSupport; // 必要な買い支え量
    public float currentSupport;  // 現在の買い支え量

    public float Progress => requiredSupport > 0 ? currentSupport / requiredSupport : 0;
}

/// <summary>
/// 敵対的買収イベントデータ
/// </summary>
[Serializable]
public struct TakeoverEventData
{
    public string eventId;
    public string targetStockId;
    public string attackerName;    // ボスの名前
    public string attackerTitle;   // ボスの肩書
    public float duration;         // 秒
    public double attackerBudget;  // 敵の資金
    public double playerDefense;   // プレイヤーの防衛額
    public float attackerProgress; // 0-1
    public float playerProgress;   // 0-1
}

/// <summary>
/// マーケットニュース
/// </summary>
[Serializable]
public struct MarketNews
{
    public string text;
    public MarketNewsType type;
    public string relatedStockId; // null = 一般ニュース
    public float priceImpact;     // 株価への影響（-1〜1）

    public MarketNews(string text, MarketNewsType type, string stockId = null, float impact = 0f)
    {
        this.text = text;
        this.type = type;
        relatedStockId = stockId;
        priceImpact = impact;
    }
}

public enum MarketNewsType
{
    Normal,   // 平常
    Positive, // ポジティブ
    Negative, // ネガティブ
    Breaking, // 速報
    Rumor     // 噂（インサイダー用）
}

/// <summary>
/// インサイダー情報
/// </summary>
[Serializable]
public struct InsiderTip
{
    public string sourceCharacter; // 情報源のキャラ名
    public string hint;            // ヒントテキスト
    public string targetStockId;   // 関連銘柄
    public bool isPositive;        // 上がる予測か下がる予測か
    public float triggerTime;      // 何秒後に効果が出るか
}

/// <summary>
/// ロドス株のランク
/// </summary>
public enum RhodosStockRank
{
    Normal,  // 放置中
    High,    // 少し連打
    Super,   // スキル全開
    God      // 瞬間最大風速
}

/// <summary>
/// 配当支払いデータ
/// </summary>
[Serializable]
public struct DividendPayment
{
    public RhodosStockRank rank;
    public double stockPrice;
    public double lmdAmount;
    public int expAmount;
    public int gamaStoneAmount;  // 合成玉
    public int originiumAmount;  // 純正源石
    public List<ItemReward> itemRewards;
}

/// <summary>
/// アイテム報酬
/// </summary>
[Serializable]
public struct ItemReward
{
    public string itemId;
    public int amount;
}
