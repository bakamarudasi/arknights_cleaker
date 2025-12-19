using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 取引履歴マネージャー
/// 売買の記録を保持・統計を計算
/// </summary>
public class TradeHistoryManager : MonoBehaviour
{
    public static TradeHistoryManager Instance { get; private set; }

    // ========================================
    // 取引記録データ
    // ========================================

    [Serializable]
    public class TradeRecord
    {
        public string id;
        public string stockId;
        public string stockName;
        public TradeType type;
        public int quantity;
        public double pricePerShare;
        public double totalAmount;      // 総額（手数料込み）
        public double profitLoss;       // 損益（売却時のみ）
        public DateTime timestamp;

        public TradeRecord(string stockId, string stockName, TradeType type,
                          int quantity, double pricePerShare, double totalAmount, double profitLoss = 0)
        {
            this.id = Guid.NewGuid().ToString();
            this.stockId = stockId;
            this.stockName = stockName;
            this.type = type;
            this.quantity = quantity;
            this.pricePerShare = pricePerShare;
            this.totalAmount = totalAmount;
            this.profitLoss = profitLoss;
            this.timestamp = DateTime.Now;
        }
    }

    public enum TradeType
    {
        Buy,
        Sell
    }

    // ========================================
    // 状態
    // ========================================

    [Header("設定")]
    [Tooltip("保持する最大履歴数")]
    [SerializeField] private int maxHistoryCount = 100;

    [SerializeField] private List<TradeRecord> history = new();

    // 統計キャッシュ
    private TradeStatistics _cachedStats;
    private bool _statsDirty = true;

    // イベント
    public event Action<TradeRecord> OnTradeRecorded;

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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 売買イベントを購読
        MarketEventBus.OnStockBought += OnStockBought;
        MarketEventBus.OnStockSold += OnStockSold;
    }

    private void OnDestroy()
    {
        MarketEventBus.OnStockBought -= OnStockBought;
        MarketEventBus.OnStockSold -= OnStockSold;
    }

    // ========================================
    // イベントハンドラ
    // ========================================

    private void OnStockBought(string stockId, int quantity, double totalCost)
    {
        string stockName = GetStockName(stockId);
        double pricePerShare = totalCost / quantity;

        var record = new TradeRecord(stockId, stockName, TradeType.Buy,
                                     quantity, pricePerShare, totalCost);
        AddRecord(record);
    }

    private void OnStockSold(string stockId, int quantity, double totalReturn, double profitLoss)
    {
        string stockName = GetStockName(stockId);
        double pricePerShare = totalReturn / quantity;

        var record = new TradeRecord(stockId, stockName, TradeType.Sell,
                                     quantity, pricePerShare, totalReturn, profitLoss);
        AddRecord(record);
    }

    // ========================================
    // 履歴管理
    // ========================================

    private void AddRecord(TradeRecord record)
    {
        history.Insert(0, record); // 新しい順

        // 最大数を超えたら古いものを削除
        while (history.Count > maxHistoryCount)
        {
            history.RemoveAt(history.Count - 1);
        }

        _statsDirty = true;
        OnTradeRecorded?.Invoke(record);

        Debug.Log($"[TradeHistory] Recorded: {record.type} {record.stockId} x{record.quantity} @ {record.pricePerShare:N0}");
    }

    /// <summary>
    /// すべての履歴を取得
    /// </summary>
    public List<TradeRecord> GetAllHistory()
    {
        return new List<TradeRecord>(history);
    }

    /// <summary>
    /// 最新N件を取得
    /// </summary>
    public List<TradeRecord> GetRecentHistory(int count)
    {
        return history.Take(count).ToList();
    }

    /// <summary>
    /// 銘柄の履歴を取得
    /// </summary>
    public List<TradeRecord> GetHistoryForStock(string stockId)
    {
        return history.FindAll(r => r.stockId == stockId);
    }

    /// <summary>
    /// 今日の履歴を取得
    /// </summary>
    public List<TradeRecord> GetTodayHistory()
    {
        var today = DateTime.Today;
        return history.FindAll(r => r.timestamp.Date == today);
    }

    /// <summary>
    /// 履歴をクリア
    /// </summary>
    public void ClearHistory()
    {
        history.Clear();
        _statsDirty = true;
    }

    // ========================================
    // 統計
    // ========================================

    public TradeStatistics GetStatistics()
    {
        if (_statsDirty)
        {
            _cachedStats = CalculateStatistics();
            _statsDirty = false;
        }
        return _cachedStats;
    }

    private TradeStatistics CalculateStatistics()
    {
        var stats = new TradeStatistics();

        var sells = history.FindAll(r => r.type == TradeType.Sell);
        var buys = history.FindAll(r => r.type == TradeType.Buy);

        stats.totalTrades = history.Count;
        stats.totalBuys = buys.Count;
        stats.totalSells = sells.Count;

        stats.totalBuyAmount = buys.Sum(r => r.totalAmount);
        stats.totalSellAmount = sells.Sum(r => r.totalAmount);

        // 損益計算
        stats.totalProfitLoss = sells.Sum(r => r.profitLoss);

        var winTrades = sells.FindAll(r => r.profitLoss > 0);
        var lossTrades = sells.FindAll(r => r.profitLoss < 0);

        stats.winCount = winTrades.Count;
        stats.lossCount = lossTrades.Count;
        stats.winRate = sells.Count > 0 ? (double)stats.winCount / sells.Count : 0;

        stats.totalProfit = winTrades.Sum(r => r.profitLoss);
        stats.totalLoss = Math.Abs(lossTrades.Sum(r => r.profitLoss));

        stats.largestWin = winTrades.Count > 0 ? winTrades.Max(r => r.profitLoss) : 0;
        stats.largestLoss = lossTrades.Count > 0 ? Math.Abs(lossTrades.Min(r => r.profitLoss)) : 0;

        stats.averageProfit = stats.winCount > 0 ? stats.totalProfit / stats.winCount : 0;
        stats.averageLoss = stats.lossCount > 0 ? stats.totalLoss / stats.lossCount : 0;

        // プロフィットファクター
        stats.profitFactor = stats.totalLoss > 0 ? stats.totalProfit / stats.totalLoss : stats.totalProfit;

        return stats;
    }

    // ========================================
    // フォーマット
    // ========================================

    /// <summary>
    /// 履歴をテキスト形式で取得
    /// </summary>
    public string FormatRecord(TradeRecord record)
    {
        string time = record.timestamp.ToString("HH:mm");
        string typeText = record.type == TradeType.Buy ? "購入" : "売却";
        string typeIcon = record.type == TradeType.Buy ? "📈" : "📉";

        string result = $"{time} | {typeIcon} {record.stockName} {typeText} {record.quantity}株 @ {record.pricePerShare:N0}";

        if (record.type == TradeType.Sell)
        {
            string plText = record.profitLoss >= 0
                ? $"<color=#4ade80>+{record.profitLoss:N0}</color>"
                : $"<color=#ef4444>{record.profitLoss:N0}</color>";
            result += $" ({plText})";
        }
        else
        {
            result += $" (-{record.totalAmount:N0} LMD)";
        }

        return result;
    }

    /// <summary>
    /// 統計サマリーをテキスト形式で取得
    /// </summary>
    public string FormatStatisticsSummary()
    {
        var stats = GetStatistics();

        string plColor = stats.totalProfitLoss >= 0 ? "#4ade80" : "#ef4444";
        string plSign = stats.totalProfitLoss >= 0 ? "+" : "";

        return $"取引回数: {stats.totalTrades} (買{stats.totalBuys}/売{stats.totalSells})\n" +
               $"勝率: {stats.winRate:P1} ({stats.winCount}勝 {stats.lossCount}敗)\n" +
               $"<color={plColor}>総損益: {plSign}{stats.totalProfitLoss:N0} LMD</color>\n" +
               $"PF: {stats.profitFactor:F2}";
    }

    // ========================================
    // 追加フォーマッタ（UI表示用）
    // ========================================

    /// <summary>
    /// 日付別にグループ化した履歴を取得
    /// </summary>
    public Dictionary<DateTime, List<TradeRecord>> GetHistoryGroupedByDate()
    {
        return history
            .GroupBy(r => r.timestamp.Date)
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// 日付別履歴をフォーマット
    /// </summary>
    public string FormatDailyHistory(DateTime date)
    {
        var records = history.FindAll(r => r.timestamp.Date == date);
        if (records.Count == 0) return "取引なし";

        var lines = new List<string> { $"【{date:yyyy/MM/dd}】" };

        double dailyPnL = 0;
        foreach (var record in records.OrderBy(r => r.timestamp))
        {
            lines.Add(FormatRecord(record));
            if (record.type == TradeType.Sell)
            {
                dailyPnL += record.profitLoss;
            }
        }

        string pnlColor = dailyPnL >= 0 ? "#4ade80" : "#ef4444";
        string pnlSign = dailyPnL >= 0 ? "+" : "";
        lines.Add($"<color={pnlColor}>日計: {pnlSign}{dailyPnL:N0} LMD</color>");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 銘柄別サマリーを取得
    /// </summary>
    public List<StockTradeSummary> GetStockSummaries()
    {
        var summaries = new List<StockTradeSummary>();

        var grouped = history.GroupBy(r => r.stockId);
        foreach (var group in grouped)
        {
            var stockRecords = group.ToList();
            var sells = stockRecords.FindAll(r => r.type == TradeType.Sell);

            var summary = new StockTradeSummary
            {
                stockId = group.Key,
                stockName = stockRecords.First().stockName,
                buyCount = stockRecords.Count(r => r.type == TradeType.Buy),
                sellCount = sells.Count,
                totalBuyAmount = stockRecords.Where(r => r.type == TradeType.Buy).Sum(r => r.totalAmount),
                totalSellAmount = sells.Sum(r => r.totalAmount),
                totalProfitLoss = sells.Sum(r => r.profitLoss),
                winCount = sells.Count(r => r.profitLoss > 0),
                lossCount = sells.Count(r => r.profitLoss < 0)
            };
            summary.winRate = sells.Count > 0 ? (double)summary.winCount / sells.Count : 0;

            summaries.Add(summary);
        }

        return summaries.OrderByDescending(s => Math.Abs(s.totalProfitLoss)).ToList();
    }

    /// <summary>
    /// 銘柄別サマリーをフォーマット
    /// </summary>
    public string FormatStockSummary(StockTradeSummary summary)
    {
        string pnlColor = summary.totalProfitLoss >= 0 ? "#4ade80" : "#ef4444";
        string pnlSign = summary.totalProfitLoss >= 0 ? "+" : "";

        return $"{summary.stockName}\n" +
               $"  取引: {summary.buyCount + summary.sellCount}回 (買{summary.buyCount}/売{summary.sellCount})\n" +
               $"  勝率: {summary.winRate:P0} ({summary.winCount}勝{summary.lossCount}敗)\n" +
               $"  <color={pnlColor}>損益: {pnlSign}{summary.totalProfitLoss:N0}</color>";
    }

    /// <summary>
    /// 期間フィルターで履歴を取得
    /// </summary>
    public List<TradeRecord> GetHistoryByPeriod(TradePeriod period)
    {
        DateTime startDate = period switch
        {
            TradePeriod.Today => DateTime.Today,
            TradePeriod.Week => DateTime.Today.AddDays(-7),
            TradePeriod.Month => DateTime.Today.AddMonths(-1),
            TradePeriod.All => DateTime.MinValue,
            _ => DateTime.MinValue
        };

        return history.FindAll(r => r.timestamp >= startDate);
    }

    /// <summary>
    /// 期間別統計をフォーマット
    /// </summary>
    public string FormatPeriodStats(TradePeriod period)
    {
        var periodRecords = GetHistoryByPeriod(period);
        var sells = periodRecords.FindAll(r => r.type == TradeType.Sell);

        string periodName = period switch
        {
            TradePeriod.Today => "本日",
            TradePeriod.Week => "今週",
            TradePeriod.Month => "今月",
            TradePeriod.All => "全期間",
            _ => ""
        };

        double pnl = sells.Sum(r => r.profitLoss);
        int wins = sells.Count(r => r.profitLoss > 0);
        int losses = sells.Count(r => r.profitLoss < 0);
        double winRate = sells.Count > 0 ? (double)wins / sells.Count : 0;

        string pnlColor = pnl >= 0 ? "#4ade80" : "#ef4444";
        string pnlSign = pnl >= 0 ? "+" : "";

        return $"【{periodName}の成績】\n" +
               $"取引: {periodRecords.Count}回\n" +
               $"勝率: {winRate:P0} ({wins}勝{losses}敗)\n" +
               $"<color={pnlColor}>損益: {pnlSign}{pnl:N0} LMD</color>";
    }

    /// <summary>
    /// コンパクト表示用フォーマット（リスト項目用）
    /// </summary>
    public string FormatRecordCompact(TradeRecord record)
    {
        string icon = record.type == TradeType.Buy ? "▲" : "▼";
        string typeColor = record.type == TradeType.Buy ? "#60a5fa" : "#f472b6";
        string time = record.timestamp.ToString("HH:mm");

        string result = $"<color={typeColor}>{icon}</color> {time} {record.stockName} ×{record.quantity}";

        if (record.type == TradeType.Sell)
        {
            string pnlColor = record.profitLoss >= 0 ? "#4ade80" : "#ef4444";
            string pnlSign = record.profitLoss >= 0 ? "+" : "";
            result += $" <color={pnlColor}>{pnlSign}{record.profitLoss:N0}</color>";
        }

        return result;
    }

    /// <summary>
    /// 詳細表示用フォーマット（モーダル用）
    /// </summary>
    public string FormatRecordDetail(TradeRecord record)
    {
        var lines = new List<string>
        {
            $"取引ID: {record.id[..8]}...",
            $"銘柄: {record.stockName} ({record.stockId})",
            $"種別: {(record.type == TradeType.Buy ? "購入" : "売却")}",
            $"数量: {record.quantity}株",
            $"単価: {record.pricePerShare:N2} LMD",
            $"総額: {record.totalAmount:N0} LMD",
            $"日時: {record.timestamp:yyyy/MM/dd HH:mm:ss}"
        };

        if (record.type == TradeType.Sell)
        {
            string pnlColor = record.profitLoss >= 0 ? "#4ade80" : "#ef4444";
            string pnlSign = record.profitLoss >= 0 ? "+" : "";
            lines.Add($"<color={pnlColor}>損益: {pnlSign}{record.profitLoss:N0} LMD</color>");
        }

        return string.Join("\n", lines);
    }

    // ========================================
    // ヘルパー
    // ========================================

    private string GetStockName(string stockId)
    {
        var stock = MarketManager.Instance?.stockDatabase?.GetByStockId(stockId);
        return stock != null ? stock.companyName : stockId;
    }

    // ========================================
    // セーブ/ロード
    // ========================================

    public List<TradeRecord> GetSaveData()
    {
        return new List<TradeRecord>(history);
    }

    public void LoadSaveData(List<TradeRecord> data)
    {
        history.Clear();
        if (data != null)
        {
            history.AddRange(data);
        }
        _statsDirty = true;
    }
}

/// <summary>
/// 取引統計データ
/// </summary>
[Serializable]
public struct TradeStatistics
{
    public int totalTrades;
    public int totalBuys;
    public int totalSells;

    public double totalBuyAmount;
    public double totalSellAmount;

    public double totalProfitLoss;
    public double totalProfit;
    public double totalLoss;

    public int winCount;
    public int lossCount;
    public double winRate;

    public double largestWin;
    public double largestLoss;

    public double averageProfit;
    public double averageLoss;

    public double profitFactor;
}

/// <summary>
/// 銘柄別取引サマリー
/// </summary>
[Serializable]
public struct StockTradeSummary
{
    public string stockId;
    public string stockName;
    public int buyCount;
    public int sellCount;
    public double totalBuyAmount;
    public double totalSellAmount;
    public double totalProfitLoss;
    public int winCount;
    public int lossCount;
    public double winRate;
}

/// <summary>
/// 期間フィルター
/// </summary>
public enum TradePeriod
{
    Today,
    Week,
    Month,
    All
}
