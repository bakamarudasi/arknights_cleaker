using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// マーケットシステムの中央管理クラス
/// 株価の更新、履歴管理、ニュース生成を担当
/// バックグラウンドで常に動作
/// </summary>
public class MarketManager : MonoBehaviour
{
    // ========================================
    // シングルトン
    // ========================================
    public static MarketManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================
    [Header("市場設定")]
    [SerializeField] private StockDatabase stockDatabase;

    [Tooltip("株価更新間隔（秒）")]
    [SerializeField] private float tickInterval = 1f;

    [Tooltip("保持する価格履歴の長さ")]
    [SerializeField] private int priceHistoryLength = 100;

    [Header("ニュース設定")]
    [Tooltip("ニュース生成間隔（秒）")]
    [SerializeField] private float newsInterval = 30f;

    [SerializeField] private MarketNewsDatabase newsDatabase;

    // ========================================
    // ランタイム状態
    // ========================================
    private Dictionary<string, StockRuntimeData> stockStates = new();
    private float tickTimer;
    private float newsTimer;
    private bool isMarketOpen = true;

    // ========================================
    // プロパティ
    // ========================================
    public bool IsMarketOpen => isMarketOpen;
    public IReadOnlyDictionary<string, StockRuntimeData> StockStates => stockStates;

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

    private void Start()
    {
        InitializeStocks();
    }

    private void Update()
    {
        if (!isMarketOpen) return;

        // 株価更新ティック
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            UpdateAllPrices();
        }

        // ニュース生成
        newsTimer += Time.deltaTime;
        if (newsTimer >= newsInterval)
        {
            newsTimer = 0f;
            GenerateRandomNews();
        }
    }

    // ========================================
    // 初期化
    // ========================================

    private void InitializeStocks()
    {
        if (stockDatabase == null)
        {
            Debug.LogWarning("[MarketManager] StockDatabase not assigned!");
            return;
        }

        foreach (var stock in stockDatabase.stocks)
        {
            var runtimeData = new StockRuntimeData
            {
                stockId = stock.stockId,
                currentPrice = stock.initialPrice,
                previousPrice = stock.initialPrice,
                openPrice = stock.initialPrice,
                highPrice = stock.initialPrice,
                lowPrice = stock.initialPrice,
                priceHistory = new Queue<double>(priceHistoryLength)
            };

            // 初期履歴を生成（チャート表示用）
            var initialHistory = StockPriceEngine.GeneratePriceHistory(
                stock.initialPrice,
                stock.drift,
                stock.volatility,
                tickInterval,
                priceHistoryLength / 2, // 半分だけ事前生成
                stock.jumpProbability,
                stock.jumpIntensity,
                stock.minPrice,
                stock.maxPrice
            );

            foreach (var price in initialHistory)
            {
                runtimeData.priceHistory.Enqueue(price);
            }
            runtimeData.currentPrice = initialHistory.Length > 0
                ? initialHistory[^1]
                : stock.initialPrice;

            stockStates[stock.stockId] = runtimeData;
        }

        Debug.Log($"[MarketManager] Initialized {stockStates.Count} stocks");
    }

    // ========================================
    // 株価更新
    // ========================================

    private void UpdateAllPrices()
    {
        if (stockDatabase == null || stockDatabase.stocks == null) return;

        foreach (var stock in stockDatabase.stocks)
        {
            if (!stockStates.TryGetValue(stock.stockId, out var state)) continue;
            if (!stock.IsUnlocked()) continue; // 未解放の株は更新しない

            UpdateStockPrice(stock, state);
        }
    }

    private void UpdateStockPrice(StockData stock, StockRuntimeData state)
    {
        state.previousPrice = state.currentPrice;

        // 新しい株価を計算
        double newPrice = StockPriceEngine.CalculateNextPrice(
            state.currentPrice,
            stock.drift,
            stock.volatility,
            tickInterval,
            stock.jumpProbability,
            stock.jumpIntensity,
            stock.minPrice,
            stock.maxPrice
        );

        state.currentPrice = newPrice;

        // 履歴に追加
        if (state.priceHistory.Count >= priceHistoryLength)
        {
            state.priceHistory.Dequeue();
        }
        state.priceHistory.Enqueue(newPrice);

        // 高値・安値を更新
        if (newPrice > state.highPrice) state.highPrice = newPrice;
        if (newPrice < state.lowPrice) state.lowPrice = newPrice;

        // イベント発火
        var snapshot = new StockPriceSnapshot(stock.stockId, newPrice, state.previousPrice);
        MarketEventBus.PublishPriceUpdated(snapshot);
    }

    // ========================================
    // 公開API
    // ========================================

    /// <summary>
    /// 指定銘柄の現在価格を取得
    /// </summary>
    public double GetCurrentPrice(string stockId)
    {
        return stockStates.TryGetValue(stockId, out var state) ? state.currentPrice : 0;
    }

    /// <summary>
    /// 指定銘柄の価格履歴を取得
    /// </summary>
    public double[] GetPriceHistory(string stockId)
    {
        return stockStates.TryGetValue(stockId, out var state)
            ? state.priceHistory.ToArray()
            : Array.Empty<double>();
    }

    /// <summary>
    /// 指定銘柄のランタイムデータを取得
    /// </summary>
    public StockRuntimeData GetStockState(string stockId)
    {
        return stockStates.TryGetValue(stockId, out var state) ? state : null;
    }

    /// <summary>
    /// 解放済みの銘柄リストを取得
    /// </summary>
    public List<StockData> GetUnlockedStocks()
    {
        if (stockDatabase == null) return new List<StockData>();
        return stockDatabase.stocks.Where(s => s.IsUnlocked()).ToList();
    }

    /// <summary>
    /// 全銘柄のスナップショットを取得
    /// </summary>
    public List<StockPriceSnapshot> GetAllSnapshots()
    {
        var snapshots = new List<StockPriceSnapshot>();
        foreach (var kvp in stockStates)
        {
            var state = kvp.Value;
            snapshots.Add(new StockPriceSnapshot(state.stockId, state.currentPrice, state.previousPrice));
        }
        return snapshots;
    }

    /// <summary>
    /// 外部イベントによる株価変動を適用
    /// </summary>
    public void ApplyExternalEvent(string stockId, float impactStrength, bool isPositive)
    {
        if (!stockStates.TryGetValue(stockId, out var state)) return;
        if (stockDatabase == null) return;

        var stock = stockDatabase.GetByStockId(stockId);
        if (stock == null) return;

        state.previousPrice = state.currentPrice;
        state.currentPrice = StockPriceEngine.ApplyEventImpact(
            state.currentPrice,
            impactStrength,
            isPositive,
            stock.minPrice,
            stock.maxPrice
        );

        var snapshot = new StockPriceSnapshot(stockId, state.currentPrice, state.previousPrice);
        MarketEventBus.PublishPriceUpdated(snapshot);
    }

    /// <summary>
    /// 物理買い支えを適用（クリック連打）
    /// </summary>
    public void ApplyBuySupport(string stockId, int clickCount)
    {
        if (!stockStates.TryGetValue(stockId, out var state)) return;

        state.currentPrice = StockPriceEngine.ApplyBuySupport(
            state.currentPrice,
            clickCount
        );
    }

    /// <summary>
    /// 市場を開く/閉じる
    /// </summary>
    public void SetMarketOpen(bool open)
    {
        isMarketOpen = open;
        Debug.Log($"[MarketManager] Market is now {(open ? "OPEN" : "CLOSED")}");
    }

    // ========================================
    // ニュース生成
    // ========================================

    private void GenerateRandomNews()
    {
        if (newsDatabase == null) return;

        var news = newsDatabase.GetRandomNews();
        if (string.IsNullOrEmpty(news.text)) return;

        // ニュースに関連する銘柄があれば株価に影響
        if (!string.IsNullOrEmpty(news.relatedStockId) && Mathf.Abs(news.priceImpact) > 0.01f)
        {
            ApplyExternalEvent(news.relatedStockId, Mathf.Abs(news.priceImpact), news.priceImpact > 0);
        }

        MarketEventBus.PublishNewsGenerated(news);
    }

    /// <summary>
    /// 手動でニュースを発行
    /// </summary>
    public void PublishNews(MarketNews news)
    {
        MarketEventBus.PublishNewsGenerated(news);
    }
}

/// <summary>
/// 株のランタイム状態データ
/// </summary>
[Serializable]
public class StockRuntimeData
{
    public string stockId;
    public double currentPrice;
    public double previousPrice;
    public double openPrice;   // 始値
    public double highPrice;   // 高値
    public double lowPrice;    // 安値
    public Queue<double> priceHistory;

    public double ChangeRate => StockPriceEngine.CalculateChangeRate(currentPrice, previousPrice);
    public double DayChangeRate => StockPriceEngine.CalculateChangeRate(currentPrice, openPrice);
}
