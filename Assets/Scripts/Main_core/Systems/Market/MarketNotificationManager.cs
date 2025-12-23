using UnityEngine;

/// <summary>
/// マーケット通知マネージャー（常駐）
/// Market画面を開いていなくてもイベントを受信してログに表示
/// </summary>
public class MarketNotificationManager : MonoBehaviour
{
    public static MarketNotificationManager Instance { get; private set; }

    [Header("通知設定")]
    [Tooltip("ニュース通知を表示")]
    [SerializeField] private bool showNewsNotification = true;

    [Tooltip("暴落/急騰通知を表示")]
    [SerializeField] private bool showPriceAlertNotification = true;

    [Tooltip("売買完了通知を表示")]
    [SerializeField] private bool showTradeNotification = true;

    [Tooltip("配当通知を表示")]
    [SerializeField] private bool showDividendNotification = true;

    [Tooltip("イベント通知を表示")]
    [SerializeField] private bool showEventNotification = true;

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
        SubscribeToEvents();
        Debug.Log("[MarketNotification] 常駐通知マネージャー起動");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ========================================
    // イベント購読
    // ========================================

    private void SubscribeToEvents()
    {
        // ニュース
        MarketEventBus.OnNewsGenerated += OnNewsGenerated;

        // 価格アラート
        MarketEventBus.OnPriceSurge += OnPriceSurge;
        MarketEventBus.OnPriceCrash += OnPriceCrash;

        // 取引
        MarketEventBus.OnStockBought += OnStockBought;
        MarketEventBus.OnStockSold += OnStockSold;

        // 配当
        MarketEventBus.OnDividendPaid += OnDividendPaid;

        // イベント
        MarketEventBus.OnMarketEventStarted += OnMarketEventStarted;
        MarketEventBus.OnMarketEventEnded += OnMarketEventEnded;
        MarketEventBus.OnTakeoverStarted += OnTakeoverStarted;
        MarketEventBus.OnTakeoverEnded += OnTakeoverEnded;

        // インサイダー
        MarketEventBus.OnInsiderTipReceived += OnInsiderTipReceived;

        // 銘柄解放
        MarketEventBus.OnStockUnlocked += OnStockUnlocked;
    }

    private void UnsubscribeFromEvents()
    {
        MarketEventBus.OnNewsGenerated -= OnNewsGenerated;
        MarketEventBus.OnPriceSurge -= OnPriceSurge;
        MarketEventBus.OnPriceCrash -= OnPriceCrash;
        MarketEventBus.OnStockBought -= OnStockBought;
        MarketEventBus.OnStockSold -= OnStockSold;
        MarketEventBus.OnDividendPaid -= OnDividendPaid;
        MarketEventBus.OnMarketEventStarted -= OnMarketEventStarted;
        MarketEventBus.OnMarketEventEnded -= OnMarketEventEnded;
        MarketEventBus.OnTakeoverStarted -= OnTakeoverStarted;
        MarketEventBus.OnTakeoverEnded -= OnTakeoverEnded;
        MarketEventBus.OnInsiderTipReceived -= OnInsiderTipReceived;
        MarketEventBus.OnStockUnlocked -= OnStockUnlocked;
    }

    // ========================================
    // ニュース通知
    // ========================================

    private void OnNewsGenerated(MarketNews news)
    {
        if (!showNewsNotification) return;

        MarketSound.Instance?.PlayNews();

        string prefix = news.type switch
        {
            MarketNewsType.Positive => "📈",
            MarketNewsType.Negative => "📉",
            MarketNewsType.Breaking => "🔴 速報",
            MarketNewsType.Rumor => "👂",
            _ => "📰"
        };

        string color = news.type switch
        {
            MarketNewsType.Positive => "#4ade80",
            MarketNewsType.Negative => "#ef4444",
            MarketNewsType.Breaking => "#ff5000",
            _ => "#fbbf24"
        };

        LogUIController.Msg($"<color={color}>{prefix} {news.text}</color>");
    }

    // ========================================
    // 価格アラート通知
    // ========================================

    private void OnPriceSurge(string stockId, double changeRate)
    {
        if (!showPriceAlertNotification) return;

        MarketSound.Instance?.PlaySurge();

        string stockName = GetStockName(stockId);
        LogUIController.Msg($"<color=#4ade80>🚀 {stockName} が急騰！ +{changeRate:P1}</color>");
    }

    private void OnPriceCrash(string stockId, double changeRate)
    {
        if (!showPriceAlertNotification) return;

        MarketSound.Instance?.PlayCrash();

        string stockName = GetStockName(stockId);
        LogUIController.Msg($"<color=#ef4444>💥 {stockName} が暴落！ {changeRate:P1}</color>");
    }

    // ========================================
    // 取引通知
    // ========================================

    private void OnStockBought(string stockId, int quantity, double totalCost)
    {
        if (!showTradeNotification) return;

        MarketSound.Instance?.PlayBuy();

        string stockName = GetStockName(stockId);
        LogUIController.Msg($"<color=#4ade80>📈 {stockName} を {quantity} 株購入 (-{totalCost:N0} LMD)</color>");
    }

    private void OnStockSold(string stockId, int quantity, double totalReturn, double profitLoss)
    {
        if (!showTradeNotification) return;

        MarketSound.Instance?.PlaySell(profitLoss >= 0);

        string stockName = GetStockName(stockId);
        string resultText = profitLoss >= 0
            ? $"<color=#4ade80>利確 +{profitLoss:N0} 🚀</color>"
            : $"<color=#ef4444>損切り {profitLoss:N0} 💀</color>";

        LogUIController.Msg($"📉 {stockName} を {quantity} 株売却 ({resultText})");
    }

    // ========================================
    // 配当通知
    // ========================================

    private void OnDividendPaid(DividendPayment payment)
    {
        if (!showDividendNotification) return;

        MarketSound.Instance?.PlayDividend();

        string rankName = RhodosStockManager.GetRankDisplayName(payment.rank);
        string rewards = "";

        if (payment.lmdAmount > 0)
            rewards += $" +{payment.lmdAmount:N0} LMD";
        if (payment.expAmount > 0)
            rewards += $" +{payment.expAmount} EXP";
        if (payment.gamaStoneAmount > 0)
            rewards += $" +{payment.gamaStoneAmount} 合成玉";
        if (payment.originiumAmount > 0)
            rewards += $" +{payment.originiumAmount} 純正源石";

        LogUIController.Msg($"<color=#fbbf24>💰 配当支払い [{rankName}]{rewards}</color>");
    }

    // ========================================
    // イベント通知
    // ========================================

    private void OnMarketEventStarted(MarketEventSnapshot data)
    {
        if (!showEventNotification) return;

        MarketSound.Instance?.PlayDefenseStart();

        string stockName = GetStockName(data.targetStockId);
        LogUIController.Msg($"<color=#ef4444>⚠️ 緊急イベント発生！ {data.title} ({stockName})</color>");
    }

    private void OnMarketEventEnded(MarketEventSnapshot data, bool success)
    {
        if (!showEventNotification) return;

        MarketSound.Instance?.PlayDefenseResult(success);

        if (success)
        {
            LogUIController.Msg($"<color=#4ade80>✅ イベント「{data.title}」防衛成功！</color>");
        }
        else
        {
            LogUIController.Msg($"<color=#ef4444>❌ イベント「{data.title}」防衛失敗...</color>");
        }
    }

    private void OnTakeoverStarted(TakeoverEventData data)
    {
        if (!showEventNotification) return;

        MarketSound.Instance?.PlayTakeoverStart();

        string stockName = GetStockName(data.targetStockId);
        LogUIController.Msg($"<color=#a855f7>⚔️ 敵対的買収発生！ {data.attackerName} が {stockName} を狙っています！</color>");
    }

    private void OnTakeoverEnded(TakeoverEventData data, bool playerWon)
    {
        if (!showEventNotification) return;

        MarketSound.Instance?.PlayTakeoverResult(playerWon);

        if (playerWon)
        {
            LogUIController.Msg($"<color=#4ade80>🏆 買収防衛成功！ {data.attackerName} を撃退！</color>");
        }
        else
        {
            LogUIController.Msg($"<color=#ef4444>💀 買収防衛失敗... {data.attackerName} に敗北</color>");
        }
    }

    // ========================================
    // インサイダー通知
    // ========================================

    private void OnInsiderTipReceived(InsiderTip tip)
    {
        string stockName = GetStockName(tip.targetStockId);
        string signal = tip.isPositive ? "📈" : "📉";

        LogUIController.Msg($"<color=#fbbf24>📩 {tip.sourceCharacter}より: {tip.hint} {signal} ({stockName})</color>");
    }

    // ========================================
    // 銘柄解放通知
    // ========================================

    private void OnStockUnlocked(string stockId)
    {
        MarketSound.Instance?.PlayUnlock();

        string stockName = GetStockName(stockId);
        LogUIController.Msg($"<color=#a855f7>🔓 新銘柄「{stockName}」が解放されました！</color>");
    }

    // ========================================
    // ヘルパー
    // ========================================

    private string GetStockName(string stockId)
    {
        if (string.IsNullOrEmpty(stockId)) return "不明";

        var stock = MarketManager.Instance?.stockDatabase?.GetByStockId(stockId);
        return stock != null ? stock.companyName : stockId;
    }
}
