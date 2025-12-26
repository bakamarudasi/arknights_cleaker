using System;
using System.Collections.Generic;

/// <summary>
/// マーケットPVE UI用ファサード実装
/// 各シングルトンPVE Managerへのアクセスを集約
/// </summary>
public class MarketPVEFacade : IMarketPVEFacade
{
    // シングルトンインスタンス（遅延取得）
    private static MarketPVEFacade _instance;
    public static MarketPVEFacade Instance => _instance ??= new MarketPVEFacade();

    // ========================================
    // 敵対的買収（Takeover Battle）
    // ========================================

    public bool IsBattleActive => TakeoverBattleManager.Instance?.IsBattleActive ?? false;

    public ActiveTakeoverBattle CurrentBattle => TakeoverBattleManager.Instance?.CurrentBattle;

    public bool TryDefend(double amount)
    {
        return TakeoverBattleManager.Instance?.TryDefend(amount) ?? false;
    }

    public bool QuickDefend(float percentOfMoney)
    {
        return TakeoverBattleManager.Instance?.QuickDefend(percentOfMoney) ?? false;
    }

    // ========================================
    // 経済イベント（Defense Event）
    // ========================================

    public bool HasActiveEvent => EconomicEventManager.Instance?.HasActiveEvent ?? false;

    public IReadOnlyList<ActiveEconomicEvent> ActiveEvents =>
        EconomicEventManager.Instance?.ActiveEvents ?? Array.Empty<ActiveEconomicEvent>();

    public ActiveEconomicEvent GetActiveEvent(string stockId)
    {
        return EconomicEventManager.Instance?.GetActiveEvent(stockId);
    }

    public void OnSupportClick(string stockId)
    {
        EconomicEventManager.Instance?.OnSupportClick(stockId);
    }

    public bool IsTradingHalted(string stockId)
    {
        return EconomicEventManager.Instance?.IsTradingHalted(stockId) ?? false;
    }

    public float GetHaltRemainingTime(string stockId)
    {
        return EconomicEventManager.Instance?.GetHaltRemainingTime(stockId) ?? 0;
    }

    // ========================================
    // インサイダー情報（Insider Tip）
    // ========================================

    public int ActiveTipCount => InsiderTipManager.Instance?.ActiveTipCount ?? 0;

    public List<ActiveInsiderTip> GetAllActiveTips()
    {
        return InsiderTipManager.Instance?.GetAllActiveTips() ?? new List<ActiveInsiderTip>();
    }

    public ActiveInsiderTip GetTipForStock(string stockId)
    {
        return InsiderTipManager.Instance?.GetTipForStock(stockId);
    }

    public bool HasTipForStock(string stockId)
    {
        return InsiderTipManager.Instance?.HasTipForStock(stockId) ?? false;
    }

    // ========================================
    // 資産情報（共通）
    // ========================================

    public double Money => WalletManager.Instance?.Money ?? 0;

    public bool CanAfford(double amount)
    {
        return WalletManager.Instance?.CanAffordMoney(amount) ?? false;
    }
}

/// <summary>
/// マーケットPVE UI用イベントハブ実装
/// 個別PVE Managerのイベントを統合
/// </summary>
public class MarketPVEEventHub : IMarketPVEEventHub
{
    private bool isSubscribed = false;

    // ========================================
    // イベント（外部公開用）
    // ========================================

    // 敵対的買収
    public event Action<ActiveTakeoverBattle> OnBattleStarted;
    public event Action<ActiveTakeoverBattle, bool> OnBattleEnded;

    // 経済イベント
    public event Action<ActiveEconomicEvent> OnDefenseEventStarted;
    public event Action<ActiveEconomicEvent, bool> OnDefenseEventEnded;

    // インサイダー情報
    public event Action<ActiveInsiderTip> OnTipReceived;
    public event Action<ActiveInsiderTip> OnTipExpired;
    public event Action<ActiveInsiderTip> OnTipTriggered;

    // ========================================
    // 購読管理
    // ========================================

    public void Subscribe()
    {
        if (isSubscribed) return;

        // 敵対的買収
        if (TakeoverBattleManager.Instance != null)
        {
            TakeoverBattleManager.Instance.OnBattleStarted += HandleBattleStarted;
            TakeoverBattleManager.Instance.OnBattleEnded += HandleBattleEnded;
        }

        // 経済イベント
        if (EconomicEventManager.Instance != null)
        {
            EconomicEventManager.Instance.OnEventStarted += HandleDefenseEventStarted;
            EconomicEventManager.Instance.OnEventEnded += HandleDefenseEventEnded;
        }

        // インサイダー情報
        if (InsiderTipManager.Instance != null)
        {
            InsiderTipManager.Instance.OnTipReceived += HandleTipReceived;
            InsiderTipManager.Instance.OnTipExpired += HandleTipExpired;
            InsiderTipManager.Instance.OnTipTriggered += HandleTipTriggered;
        }

        isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (!isSubscribed) return;

        // 敵対的買収
        if (TakeoverBattleManager.Instance != null)
        {
            TakeoverBattleManager.Instance.OnBattleStarted -= HandleBattleStarted;
            TakeoverBattleManager.Instance.OnBattleEnded -= HandleBattleEnded;
        }

        // 経済イベント
        if (EconomicEventManager.Instance != null)
        {
            EconomicEventManager.Instance.OnEventStarted -= HandleDefenseEventStarted;
            EconomicEventManager.Instance.OnEventEnded -= HandleDefenseEventEnded;
        }

        // インサイダー情報
        if (InsiderTipManager.Instance != null)
        {
            InsiderTipManager.Instance.OnTipReceived -= HandleTipReceived;
            InsiderTipManager.Instance.OnTipExpired -= HandleTipExpired;
            InsiderTipManager.Instance.OnTipTriggered -= HandleTipTriggered;
        }

        isSubscribed = false;
    }

    // ========================================
    // イベントハンドラ（転送）
    // ========================================

    private void HandleBattleStarted(ActiveTakeoverBattle battle) => OnBattleStarted?.Invoke(battle);
    private void HandleBattleEnded(ActiveTakeoverBattle battle, bool playerWon) => OnBattleEnded?.Invoke(battle, playerWon);

    private void HandleDefenseEventStarted(ActiveEconomicEvent evt) => OnDefenseEventStarted?.Invoke(evt);
    private void HandleDefenseEventEnded(ActiveEconomicEvent evt, bool success) => OnDefenseEventEnded?.Invoke(evt, success);

    private void HandleTipReceived(ActiveInsiderTip tip) => OnTipReceived?.Invoke(tip);
    private void HandleTipExpired(ActiveInsiderTip tip) => OnTipExpired?.Invoke(tip);
    private void HandleTipTriggered(ActiveInsiderTip tip) => OnTipTriggered?.Invoke(tip);
}
