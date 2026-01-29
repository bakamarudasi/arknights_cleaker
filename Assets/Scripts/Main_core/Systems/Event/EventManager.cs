using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲームイベントの監視・発動を管理するマネージャー
/// GameControllerと連携し、条件達成時にイベントを発火する
/// </summary>
public class EventManager : BaseSingleton<EventManager>
{
    protected override bool Persistent => false;

    // ========================================
    // 設定
    // ========================================

    [Header("イベントデータ")]
    [SerializeField] private List<GameEventData> allEvents = new List<GameEventData>();

    [Header("表示設定")]
    [SerializeField] private Transform eventLayer;  // イベントUI表示先

    // ========================================
    // 状態管理
    // ========================================

    /// <summary>発動済みイベントIDのセット</summary>
    private HashSet<string> triggeredEventIds = new HashSet<string>();

    /// <summary>現在表示中のイベントキュー</summary>
    private Queue<GameEventData> pendingEvents = new Queue<GameEventData>();

    /// <summary>現在表示中のイベントインスタンス</summary>
    private GameObject currentEventInstance;

    /// <summary>イベント処理中かどうか</summary>
    public bool IsEventActive => currentEventInstance != null;

    /// <summary>初回起動かどうか（セーブデータがない状態）</summary>
    public bool IsFirstLaunch { get; private set; } = true;

    /// <summary>セッション開始時刻</summary>
    private float sessionStartTime;

    // ========================================
    // イベント（外部通知用）
    // ========================================

    /// <summary>イベントが発動した時</summary>
    public event Action<GameEventData> OnEventTriggered;

    /// <summary>メニューが解放された時</summary>
    public event Action<MenuType> OnMenuUnlocked;

    /// <summary>イベント表示が完了した時</summary>
    public event Action<GameEventData> OnEventCompleted;

    // ========================================
    // キャッシュ（イベント解除用）
    // ========================================

    private Action<double> _onMoneyChangedCallback;
    private Action<double> _onMoneyEarnedCallback;
    private Action<UpgradeData, int> _onUpgradePurchasedCallback;

    /// <summary>定期条件チェック用Coroutine</summary>
    private Coroutine _periodicCheckCoroutine;
    private readonly WaitForSeconds _oneSecondWait = new WaitForSeconds(1f);

    /// <summary>未発動イベントキャッシュ（GC圧力軽減）</summary>
    private List<GameEventData> _pendingEventsCache = new List<GameEventData>();
    private bool _pendingEventsCacheDirty = true;

    // ========================================
    // 初期化
    // ========================================

    void Start()
    {
        // 少し遅延してGameControllerの初期化を待つ
        Invoke(nameof(Initialize), 0.1f);
    }

    private void Initialize()
    {
        if (GameController.Instance == null)
        {
            Debug.LogError("[EventManager] GameController not found!");
            return;
        }

        sessionStartTime = Time.realtimeSinceStartup;
        SubscribeToGameEvents();

        // 起動時イベントをチェック（少し遅延させてUIの準備を待つ）
        Invoke(nameof(CheckStartupEvents), 0.5f);

    }

    /// <summary>
    /// 起動時のイベントをチェック
    /// </summary>
    private void CheckStartupEvents()
    {
        foreach (var evt in GetPendingEvents())
        {
            bool shouldTrigger = evt.triggerType switch
            {
                EventTriggerType.FirstLaunch => IsFirstLaunch,
                EventTriggerType.OnGameStart => true,
                _ => false
            };

            if (shouldTrigger) TriggerEvent(evt);
        }
    }

    // ========================================
    // イベント購読
    // ========================================

    private void SubscribeToGameEvents()
    {
        var gc = GameController.Instance;

        // 通貨関連
        _onMoneyChangedCallback = _ => CheckMoneyConditions();
        _onMoneyEarnedCallback = _ => CheckMoneyConditions();
        gc.Wallet.OnMoneyChanged += _onMoneyChangedCallback;
        gc.Wallet.OnMoneyEarned += _onMoneyEarnedCallback;

        // 強化関連
        _onUpgradePurchasedCallback = (data, level) => CheckUpgradeConditions(data, level);
        gc.Upgrade.OnUpgradePurchased += _onUpgradePurchasedCallback;

        // 統計の定期チェック（クリック数、プレイ時間など）- Coroutine版
        _periodicCheckCoroutine = StartCoroutine(PeriodicConditionCheckCoroutine());
    }

    /// <summary>
    /// 定期条件チェックCoroutine（GC圧力軽減のためWaitForSecondsを再利用）
    /// </summary>
    private IEnumerator PeriodicConditionCheckCoroutine()
    {
        while (true)
        {
            yield return _oneSecondWait;
            CheckPeriodicConditions();
        }
    }

    // ========================================
    // 条件チェック
    // ========================================

    private void CheckMoneyConditions()
    {
        var gc = GameController.Instance;
        var stats = gc.GetStatistics();

        foreach (var evt in GetPendingEvents())
        {
            bool shouldTrigger = evt.triggerType switch
            {
                EventTriggerType.CurrentMoneyReached => gc.Wallet.Money >= evt.triggerValue,
                EventTriggerType.TotalMoneyEarned => stats.totalMoneyEarned >= evt.triggerValue,
                EventTriggerType.CertificateReached => gc.Wallet.Certificates >= evt.triggerValue,
                _ => false
            };

            if (shouldTrigger) TriggerEvent(evt);
        }
    }

    private void CheckUpgradeConditions(UpgradeData upgradeData, int newLevel)
    {
        foreach (var evt in GetPendingEvents())
        {
            bool shouldTrigger = evt.triggerType switch
            {
                EventTriggerType.SpecificUpgradePurchased =>
                    upgradeData.id == evt.requireId,
                EventTriggerType.UpgradeLevelReached =>
                    upgradeData.id == evt.requireId && newLevel >= evt.triggerValue,
                EventTriggerType.TotalUpgradesPurchased =>
                    GameController.Instance.GetStatistics().totalUpgradesPurchased >= evt.triggerValue,
                _ => false
            };

            if (shouldTrigger) TriggerEvent(evt);
        }
    }

    private void CheckPeriodicConditions()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        var stats = gc.GetStatistics();
        float sessionTime = Time.realtimeSinceStartup - sessionStartTime;

        foreach (var evt in GetPendingEvents())
        {
            bool shouldTrigger = evt.triggerType switch
            {
                EventTriggerType.TotalClicks => stats.totalClicks >= (long)evt.triggerValue,
                EventTriggerType.TotalCriticalHits => stats.totalCriticalHits >= evt.triggerValue,
                EventTriggerType.HighestClickDamage => stats.highestClickDamage >= evt.triggerValue,
                EventTriggerType.PlayTimeReached => stats.totalPlayTimeSeconds >= evt.triggerValue,
                EventTriggerType.SessionTimeReached => sessionTime >= evt.triggerValue,
                _ => false
            };

            if (shouldTrigger) TriggerEvent(evt);
        }
    }

    // ========================================
    // イベント発動
    // ========================================

    /// <summary>
    /// イベントを発動する
    /// </summary>
    private void TriggerEvent(GameEventData evt)
    {
        if (evt == null) return;

        // 既に発動済みなら（oneTimeOnlyの場合）スキップ
        if (evt.oneTimeOnly && triggeredEventIds.Contains(evt.eventId))
            return;

        // 前提イベントチェック
        if (evt.prerequisiteEvent != null &&
            !triggeredEventIds.Contains(evt.prerequisiteEvent.eventId))
            return;

        // 発動済みに追加
        triggeredEventIds.Add(evt.eventId);

        // キャッシュ無効化
        InvalidatePendingEventsCache();

        // イベント発火
        evt.Raise();
        OnEventTriggered?.Invoke(evt);

        // 報酬付与
        GrantRewards(evt);

        // メニュー解放
        if (evt.unlockMenu.HasValue)
        {
            OnMenuUnlocked?.Invoke(evt.unlockMenu.Value);
        }

        // UI表示
        if (evt.eventPrefab != null)
        {
            pendingEvents.Enqueue(evt);
            TryShowNextEvent();
        }
        else if (!string.IsNullOrEmpty(evt.notificationText))
        {
            ShowNotification(evt);
        }

    }

    /// <summary>
    /// 手動でイベントを発火（デバッグ/スクリプト用）
    /// </summary>
    public void TriggerEventById(string eventId)
    {
        var evt = allEvents.Find(e => e.eventId == eventId);
        if (evt != null) TriggerEvent(evt);
    }

    // ========================================
    // 報酬付与
    // ========================================

    private void GrantRewards(GameEventData evt)
    {
        var gc = GameController.Instance;

        if (evt.rewardMoney > 0)
            gc.Wallet.AddMoney(evt.rewardMoney);

        if (evt.rewardCertificates > 0)
            gc.Wallet.AddCertificates(evt.rewardCertificates);

        foreach (var item in evt.rewardItems)
        {
            gc.Inventory.Add(item.itemId, item.amount);
        }
    }

    // ========================================
    // UI表示
    // ========================================

    private void TryShowNextEvent()
    {
        if (IsEventActive || pendingEvents.Count == 0) return;

        var evt = pendingEvents.Dequeue();

        if (evt.pauseGame)
            Time.timeScale = 0f;

        // イベントプレハブをインスタンス化
        Transform parent = eventLayer != null ? eventLayer : transform;
        currentEventInstance = Instantiate(evt.eventPrefab, parent);

        // イベントUIにデータを渡す（IEventDisplayを実装している場合）
        var display = currentEventInstance.GetComponent<IEventDisplay>();
        display?.Setup(evt, OnEventDisplayClosed);
    }

    private void OnEventDisplayClosed()
    {
        if (currentEventInstance != null)
        {
            Destroy(currentEventInstance);
            currentEventInstance = null;
        }

        Time.timeScale = 1f;

        // 次のイベントがあれば表示
        TryShowNextEvent();
    }

    private void ShowNotification(GameEventData evt)
    {
        // TODO: ログシステムや通知UIと連携
    }

    // ========================================
    // ヘルパー
    // ========================================

    /// <summary>
    /// まだ発動していない（チェック対象の）イベントを取得
    /// キャッシュを使用してGC圧力を軽減
    /// </summary>
    private List<GameEventData> GetPendingEvents()
    {
        if (!_pendingEventsCacheDirty)
        {
            return _pendingEventsCache;
        }

        _pendingEventsCache.Clear();

        // 未発動イベントを収集
        for (int i = 0; i < allEvents.Count; i++)
        {
            var evt = allEvents[i];
            if (!evt.oneTimeOnly || !triggeredEventIds.Contains(evt.eventId))
            {
                _pendingEventsCache.Add(evt);
            }
        }

        // 優先度でソート（降順）
        _pendingEventsCache.Sort((a, b) => b.priority.CompareTo(a.priority));

        _pendingEventsCacheDirty = false;
        return _pendingEventsCache;
    }

    /// <summary>
    /// 未発動イベントキャッシュを無効化
    /// </summary>
    private void InvalidatePendingEventsCache()
    {
        _pendingEventsCacheDirty = true;
    }

    /// <summary>
    /// 特定のイベントが発動済みかどうか
    /// </summary>
    public bool IsEventTriggered(string eventId)
    {
        return triggeredEventIds.Contains(eventId);
    }

    /// <summary>
    /// 特定のメニューが解放済みかどうか（LINQ不使用版）
    /// </summary>
    public bool IsMenuUnlocked(MenuType menuType)
    {
        for (int i = 0; i < allEvents.Count; i++)
        {
            var evt = allEvents[i];
            if (evt.unlockMenu == menuType && triggeredEventIds.Contains(evt.eventId))
            {
                return true;
            }
        }
        return false;
    }

    // ========================================
    // セーブ/ロード
    // ========================================

    /// <summary>
    /// 発動済みイベントIDのリストを取得（セーブ用）
    /// </summary>
    public List<string> GetTriggeredEventIds()
    {
        return triggeredEventIds.ToList();
    }

    /// <summary>
    /// 発動済みイベントを復元（ロード用）
    /// </summary>
    public void RestoreTriggeredEvents(List<string> eventIds)
    {
        triggeredEventIds = new HashSet<string>(eventIds ?? new List<string>());

        // キャッシュ無効化
        InvalidatePendingEventsCache();

        // セーブデータがあれば初回起動ではない
        if (triggeredEventIds.Count > 0)
        {
            IsFirstLaunch = false;
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    private void OnDestroy()
    {
        // Coroutine停止
        if (_periodicCheckCoroutine != null)
        {
            StopCoroutine(_periodicCheckCoroutine);
            _periodicCheckCoroutine = null;
        }

        var gc = GameController.Instance;
        if (gc != null)
        {
            if (gc.Wallet != null)
            {
                gc.Wallet.OnMoneyChanged -= _onMoneyChangedCallback;
                gc.Wallet.OnMoneyEarned -= _onMoneyEarnedCallback;
            }
            if (gc.Upgrade != null)
            {
                gc.Upgrade.OnUpgradePurchased -= _onUpgradePurchasedCallback;
            }
        }
    }
}

/// <summary>
/// イベント表示UIが実装するインターフェース
/// </summary>
public interface IEventDisplay
{
    /// <summary>
    /// イベントデータを設定し、完了時にコールバックを呼ぶ
    /// </summary>
    void Setup(GameEventData eventData, Action onComplete);
}
