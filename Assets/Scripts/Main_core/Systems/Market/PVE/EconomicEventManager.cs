using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 経済イベント（防衛戦）の管理クラス
/// 企業が襲撃を受け、プレイヤーが買い支えで防衛する
///
/// 疎結合設計：このクラスを削除しても他システムに影響なし
/// </summary>
public class EconomicEventManager : MonoBehaviour
{
    // ========================================
    // シングルトン
    // ========================================
    public static EconomicEventManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================
    [Header("イベント発生設定")]
    [Tooltip("イベント発生チェック間隔（秒）")]
    [SerializeField] private float checkInterval = 60f;

    [Tooltip("イベント発生確率（0〜1）")]
    [SerializeField] private float eventProbability = 0.1f;

    [Tooltip("同時発生可能な最大イベント数")]
    [SerializeField] private int maxConcurrentEvents = 1;

    [Header("防衛戦設定")]
    [Tooltip("イベント継続時間（秒）")]
    [SerializeField] private float eventDuration = 60f;

    [Tooltip("必要な買い支えクリック数")]
    [SerializeField] private int requiredClicks = 100;

    [Tooltip("イベント中の株価下落速度（秒あたり%）")]
    [SerializeField] private float priceDecayRate = 0.01f;

    [Header("報酬設定")]
    [Tooltip("成功時のLMD報酬倍率（株価の何倍）")]
    [SerializeField] private float successRewardMultiplier = 0.5f;

    [Tooltip("失敗時の取引停止時間（秒）")]
    [SerializeField] private float tradingHaltDuration = 300f;

    [Header("データ")]
    [SerializeField] private List<EconomicEventTemplate> eventTemplates = new();

    // ========================================
    // ランタイム状態
    // ========================================
    private List<ActiveEconomicEvent> activeEvents = new();
    private Dictionary<string, float> tradingHalts = new(); // stockId -> 残り時間
    private float checkTimer;

    // ========================================
    // プロパティ
    // ========================================
    public IReadOnlyList<ActiveEconomicEvent> ActiveEvents => activeEvents;
    public bool HasActiveEvent => activeEvents.Count > 0;

    // ========================================
    // イベント
    // ========================================
    public event Action<ActiveEconomicEvent> OnEventStarted;
    public event Action<ActiveEconomicEvent, bool> OnEventEnded; // event, success

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

    private void Update()
    {
        UpdateCheckTimer();
        UpdateActiveEvents();
        UpdateTradingHalts();
    }

    // ========================================
    // イベント発生チェック
    // ========================================

    private void UpdateCheckTimer()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0;
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        // 最大同時発生数チェック
        if (activeEvents.Count >= maxConcurrentEvents) return;

        // 確率チェック
        if (UnityEngine.Random.value > eventProbability) return;

        // 対象銘柄を選択（解放済み＆取引停止中でない）
        var availableStocks = GetAvailableStocks();
        if (availableStocks.Count == 0) return;

        var targetStock = availableStocks[UnityEngine.Random.Range(0, availableStocks.Count)];

        // テンプレートを選択
        var template = GetRandomTemplate();

        // イベント開始
        StartEvent(targetStock, template);
    }

    private List<StockData> GetAvailableStocks()
    {
        var result = new List<StockData>();
        var unlockedStocks = MarketManager.Instance?.GetUnlockedStocks();

        if (unlockedStocks == null) return result;

        foreach (var stock in unlockedStocks)
        {
            // 取引停止中でない
            if (tradingHalts.ContainsKey(stock.stockId)) continue;

            // 既にイベント中でない
            if (activeEvents.Exists(e => e.stockId == stock.stockId)) continue;

            result.Add(stock);
        }

        return result;
    }

    private EconomicEventTemplate GetRandomTemplate()
    {
        if (eventTemplates.Count == 0)
        {
            // デフォルトテンプレート
            return new EconomicEventTemplate
            {
                eventName = "襲撃",
                description = "レユニオンが攻撃しています！",
                attackerName = "レユニオン"
            };
        }

        return eventTemplates[UnityEngine.Random.Range(0, eventTemplates.Count)];
    }

    // ========================================
    // イベント管理
    // ========================================

    private void StartEvent(StockData stock, EconomicEventTemplate template)
    {
        var activeEvent = new ActiveEconomicEvent
        {
            eventId = Guid.NewGuid().ToString(),
            stockId = stock.stockId,
            stockName = stock.companyName,
            eventName = template.eventName,
            description = template.description,
            attackerName = template.attackerName,
            duration = eventDuration,
            remainingTime = eventDuration,
            requiredClicks = requiredClicks,
            currentClicks = 0,
            originalPrice = MarketManager.Instance?.GetCurrentPrice(stock.stockId) ?? stock.initialPrice
        };

        activeEvents.Add(activeEvent);

        // イベント発火
        var eventData = CreateEventData(activeEvent);
        MarketEventBus.PublishMarketEventStarted(eventData);
        OnEventStarted?.Invoke(activeEvent);

        LogUIController.Msg($"⚠️ 緊急: {stock.companyName}が{template.attackerName}の襲撃を受けています！");
    }

    private void UpdateActiveEvents()
    {
        for (int i = activeEvents.Count - 1; i >= 0; i--)
        {
            var evt = activeEvents[i];
            evt.remainingTime -= Time.deltaTime;

            // 株価を下落させる
            ApplyPriceDecay(evt.stockId);

            // 成功判定
            if (evt.currentClicks >= evt.requiredClicks)
            {
                EndEvent(evt, true);
                activeEvents.RemoveAt(i);
                continue;
            }

            // タイムアウト判定
            if (evt.remainingTime <= 0)
            {
                EndEvent(evt, false);
                activeEvents.RemoveAt(i);
            }
        }
    }

    private void ApplyPriceDecay(string stockId)
    {
        // 株価を徐々に下落
        MarketManager.Instance?.ApplyExternalEvent(stockId, priceDecayRate * Time.deltaTime, false);
    }

    private void EndEvent(ActiveEconomicEvent evt, bool success)
    {
        var eventData = CreateEventData(evt);
        MarketEventBus.PublishMarketEventEnded(eventData, success);
        OnEventEnded?.Invoke(evt, success);

        if (success)
        {
            // 成功報酬
            double reward = evt.originalPrice * successRewardMultiplier;
            WalletManager.Instance?.AddMoney(reward);

            // 株価を回復
            MarketManager.Instance?.ApplyExternalEvent(evt.stockId, 0.2f, true);

            LogUIController.Msg($"🎉 防衛成功！ {evt.stockName} を守り抜いた！ (+{reward:N0} LMD)");
        }
        else
        {
            // 失敗：取引停止
            tradingHalts[evt.stockId] = tradingHaltDuration;

            // 株価大暴落
            MarketManager.Instance?.ApplyExternalEvent(evt.stockId, 0.3f, false);

            LogUIController.Msg($"💀 防衛失敗... {evt.stockName} は一時取引停止になりました");
        }
    }

    private MarketEventSnapshot CreateEventData(ActiveEconomicEvent evt)
    {
        return new MarketEventSnapshot
        {
            eventId = evt.eventId,
            targetStockId = evt.stockId,
            title = $"{evt.stockName} {evt.eventName}",
            description = evt.description,
            duration = evt.duration,
            requiredSupport = evt.requiredClicks,
            currentSupport = evt.currentClicks
        };
    }

    // ========================================
    // 取引停止管理
    // ========================================

    private void UpdateTradingHalts()
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in tradingHalts)
        {
            tradingHalts[kvp.Key] -= Time.deltaTime;
            if (tradingHalts[kvp.Key] <= 0)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            tradingHalts.Remove(key);
            LogUIController.Msg($"📈 {key} の取引が再開されました");
        }
    }

    /// <summary>
    /// 銘柄が取引停止中かチェック
    /// </summary>
    public bool IsTradingHalted(string stockId)
    {
        return tradingHalts.ContainsKey(stockId);
    }

    /// <summary>
    /// 取引停止の残り時間を取得
    /// </summary>
    public float GetHaltRemainingTime(string stockId)
    {
        return tradingHalts.TryGetValue(stockId, out float time) ? time : 0;
    }

    // ========================================
    // プレイヤーアクション
    // ========================================

    /// <summary>
    /// 買い支えクリック（UIから呼び出し）
    /// </summary>
    public void OnSupportClick(string stockId)
    {
        var evt = activeEvents.Find(e => e.stockId == stockId);
        if (evt == null) return;

        evt.currentClicks++;

        // 少し株価を回復
        MarketManager.Instance?.ApplyBuySupport(stockId, 1);
    }

    /// <summary>
    /// アクティブイベントを取得
    /// </summary>
    public ActiveEconomicEvent GetActiveEvent(string stockId)
    {
        return activeEvents.Find(e => e.stockId == stockId);
    }

    // ========================================
    // デバッグ
    // ========================================

#if UNITY_EDITOR
    [ContextMenu("Force Trigger Event")]
    private void DebugForceTriggerEvent()
    {
        eventProbability = 1f;
        checkTimer = checkInterval;
    }

    [ContextMenu("Complete Current Event")]
    private void DebugCompleteEvent()
    {
        if (activeEvents.Count > 0)
        {
            activeEvents[0].currentClicks = activeEvents[0].requiredClicks;
        }
    }
#endif
}

/// <summary>
/// 経済イベントのテンプレート
/// </summary>
[Serializable]
public class EconomicEventTemplate
{
    public string eventName = "襲撃";
    public string description = "企業が攻撃を受けています！";
    public string attackerName = "レユニオン";
    public Sprite attackerIcon;
}

/// <summary>
/// アクティブな経済イベント
/// </summary>
[Serializable]
public class ActiveEconomicEvent
{
    public string eventId;
    public string stockId;
    public string stockName;
    public string eventName;
    public string description;
    public string attackerName;
    public float duration;
    public float remainingTime;
    public int requiredClicks;
    public int currentClicks;
    public double originalPrice;

    public float Progress => requiredClicks > 0 ? (float)currentClicks / requiredClicks : 0;
    public float TimeProgress => duration > 0 ? remainingTime / duration : 0;
}
