using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 株価アラートマネージャー
/// 指定価格に達したら通知を発行
/// </summary>
public class StockPriceAlertManager : BaseSingleton<StockPriceAlertManager>
{

    // ========================================
    // アラートデータ
    // ========================================

    [Serializable]
    public class PriceAlert
    {
        public string id;
        public string stockId;
        public AlertType type;
        public double targetPrice;
        public bool isTriggered;
        public DateTime createdAt;

        public PriceAlert(string stockId, AlertType type, double targetPrice)
        {
            this.id = Guid.NewGuid().ToString();
            this.stockId = stockId;
            this.type = type;
            this.targetPrice = targetPrice;
            this.isTriggered = false;
            this.createdAt = DateTime.Now;
        }
    }

    public enum AlertType
    {
        AbovePrice,  // 指定価格以上になったら
        BelowPrice   // 指定価格以下になったら
    }

    // ========================================
    // Prefab設定（シーン配置用）
    // ========================================

    [Header("通知UI設定")]
    [Tooltip("アラート通知用のポップアップUIプレハブ")]
    [SerializeField] private GameObject alertPopupPrefab;

    [Tooltip("通知を表示するキャンバス（未設定時は自動検索）")]
    [SerializeField] private Canvas notificationCanvas;

    [Header("サウンド設定")]
    [Tooltip("上昇アラート時のSE")]
    [SerializeField] private AudioClip alertUpSound;

    [Tooltip("下落アラート時のSE")]
    [SerializeField] private AudioClip alertDownSound;

#pragma warning disable CS0414 // UI実装時に使用予定の設定フィールド
    [Tooltip("SE音量")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;

    [Header("通知設定")]
    [Tooltip("ポップアップ表示時間（秒）")]
    [SerializeField] private float popupDuration = 3f;

    [Tooltip("同時に表示する最大通知数")]
    [SerializeField] private int maxVisibleNotifications = 3;
#pragma warning restore CS0414

    // ========================================
    // 状態
    // ========================================

    [Header("アラートデータ (Runtime)")]
    [SerializeField] private List<PriceAlert> alerts = new();

    // イベント
    public event Action<PriceAlert, double> OnAlertTriggered;

    // ========================================
    // 初期化
    // ========================================

    private void Start()
    {
        // 株価更新イベントを購読
        MarketEventBus.OnPriceUpdated += OnPriceUpdated;
    }

    private void OnDestroy()
    {
        MarketEventBus.OnPriceUpdated -= OnPriceUpdated;
    }

    // ========================================
    // アラート管理
    // ========================================

    /// <summary>
    /// アラートを追加
    /// </summary>
    public PriceAlert AddAlert(string stockId, AlertType type, double targetPrice)
    {
        var alert = new PriceAlert(stockId, type, targetPrice);
        alerts.Add(alert);

        string stockName = GetStockName(stockId);
        string typeText = type == AlertType.AbovePrice ? "以上" : "以下";
        LogUIController.Msg($"<color=#fbbf24>🔔 アラート設定: {stockName} が {targetPrice:N0} {typeText}</color>");

        Debug.Log($"[PriceAlert] Added: {stockId} {type} {targetPrice}");
        return alert;
    }

    /// <summary>
    /// アラートを削除
    /// </summary>
    public bool RemoveAlert(string alertId)
    {
        var alert = alerts.Find(a => a.id == alertId);
        if (alert != null)
        {
            alerts.Remove(alert);
            Debug.Log($"[PriceAlert] Removed: {alertId}");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 銘柄のアラートをすべて削除
    /// </summary>
    public void RemoveAlertsForStock(string stockId)
    {
        alerts.RemoveAll(a => a.stockId == stockId);
    }

    /// <summary>
    /// すべてのアラートを取得
    /// </summary>
    public List<PriceAlert> GetAllAlerts()
    {
        return new List<PriceAlert>(alerts);
    }

    /// <summary>
    /// 銘柄のアラートを取得
    /// </summary>
    public List<PriceAlert> GetAlertsForStock(string stockId)
    {
        return alerts.FindAll(a => a.stockId == stockId && !a.isTriggered);
    }

    /// <summary>
    /// アクティブなアラート数
    /// </summary>
    public int ActiveAlertCount => alerts.FindAll(a => !a.isTriggered).Count;

    // ========================================
    // 価格監視
    // ========================================

    private void OnPriceUpdated(StockPriceSnapshot snapshot)
    {
        // この銘柄のアラートをチェック
        var stockAlerts = alerts.FindAll(a => a.stockId == snapshot.stockId && !a.isTriggered);

        foreach (var alert in stockAlerts)
        {
            bool triggered = false;

            if (alert.type == AlertType.AbovePrice && snapshot.price >= alert.targetPrice)
            {
                triggered = true;
            }
            else if (alert.type == AlertType.BelowPrice && snapshot.price <= alert.targetPrice)
            {
                triggered = true;
            }

            if (triggered)
            {
                TriggerAlert(alert, snapshot.price);
            }
        }
    }

    private void TriggerAlert(PriceAlert alert, double currentPrice)
    {
        alert.isTriggered = true;

        string stockName = GetStockName(alert.stockId);
        string direction = alert.type == AlertType.AbovePrice ? "📈 上昇" : "📉 下落";
        string color = alert.type == AlertType.AbovePrice ? "#4ade80" : "#ef4444";

        LogUIController.Msg($"<color={color}>🔔 {direction}アラート！ {stockName} が {currentPrice:N0} に到達！</color>");

        // イベント発火
        OnAlertTriggered?.Invoke(alert, currentPrice);

        Debug.Log($"[PriceAlert] Triggered: {alert.stockId} @ {currentPrice}");
    }

    // ========================================
    // クイック設定
    // ========================================

    /// <summary>
    /// 現在価格の±X%でアラート設定
    /// </summary>
    public void SetPercentageAlert(string stockId, double percentage)
    {
        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? 0;
        if (currentPrice <= 0) return;

        double abovePrice = currentPrice * (1 + percentage / 100);
        double belowPrice = currentPrice * (1 - percentage / 100);

        AddAlert(stockId, AlertType.AbovePrice, abovePrice);
        AddAlert(stockId, AlertType.BelowPrice, belowPrice);
    }

    /// <summary>
    /// 利確/損切りラインでアラート設定
    /// </summary>
    public void SetProfitLossAlert(string stockId, double profitPercent, double lossPercent)
    {
        double avgCost = PortfolioManager.Instance?.GetAverageCost(stockId) ?? 0;
        if (avgCost <= 0) return;

        double profitPrice = avgCost * (1 + profitPercent / 100);
        double lossPrice = avgCost * (1 - lossPercent / 100);

        AddAlert(stockId, AlertType.AbovePrice, profitPrice);
        AddAlert(stockId, AlertType.BelowPrice, lossPrice);

        LogUIController.Msg($"<color=#fbbf24>📊 利確ライン: {profitPrice:N0} / 損切りライン: {lossPrice:N0}</color>");
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

    public List<PriceAlert> GetSaveData()
    {
        return alerts.FindAll(a => !a.isTriggered);
    }

    public void LoadSaveData(List<PriceAlert> data)
    {
        alerts.Clear();
        if (data != null)
        {
            alerts.AddRange(data);
        }
    }
}
