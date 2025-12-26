using System;
using System.Collections.Generic;

/// <summary>
/// マーケットPVE UI用ファサードインターフェース
/// 複数のPVE Managerへの依存を隠蔽し、疎結合を実現
///
/// 統合するManager:
/// - TakeoverBattleManager (敵対的買収)
/// - EconomicEventManager (経済イベント/防衛戦)
/// - InsiderTipManager (インサイダー情報)
/// </summary>
public interface IMarketPVEFacade
{
    // ========================================
    // 敵対的買収（Takeover Battle）
    // ========================================

    /// <summary>買収バトル中かどうか</summary>
    bool IsBattleActive { get; }

    /// <summary>現在のバトル状態を取得</summary>
    ActiveTakeoverBattle CurrentBattle { get; }

    /// <summary>対抗買い（LMDを使って防衛）</summary>
    bool TryDefend(double amount);

    /// <summary>クイック防衛（所持金の一定割合を投入）</summary>
    bool QuickDefend(float percentOfMoney);

    // ========================================
    // 経済イベント（Defense Event）
    // ========================================

    /// <summary>アクティブな経済イベントがあるか</summary>
    bool HasActiveEvent { get; }

    /// <summary>アクティブな経済イベント一覧</summary>
    IReadOnlyList<ActiveEconomicEvent> ActiveEvents { get; }

    /// <summary>指定銘柄のアクティブイベントを取得</summary>
    ActiveEconomicEvent GetActiveEvent(string stockId);

    /// <summary>買い支えクリック</summary>
    void OnSupportClick(string stockId);

    /// <summary>銘柄が取引停止中かチェック</summary>
    bool IsTradingHalted(string stockId);

    /// <summary>取引停止の残り時間を取得</summary>
    float GetHaltRemainingTime(string stockId);

    // ========================================
    // インサイダー情報（Insider Tip）
    // ========================================

    /// <summary>アクティブなヒント数</summary>
    int ActiveTipCount { get; }

    /// <summary>全アクティブヒントを取得</summary>
    List<ActiveInsiderTip> GetAllActiveTips();

    /// <summary>特定銘柄のヒントを取得</summary>
    ActiveInsiderTip GetTipForStock(string stockId);

    /// <summary>特定銘柄にヒントがあるか</summary>
    bool HasTipForStock(string stockId);

    // ========================================
    // 資産情報（共通）
    // ========================================

    /// <summary>現在の所持金（LMD）</summary>
    double Money { get; }

    /// <summary>指定金額を支払えるか</summary>
    bool CanAfford(double amount);
}

/// <summary>
/// マーケットPVE UI用イベントハブ
/// 個別PVE Managerのイベントを統合して購読しやすくする
/// </summary>
public interface IMarketPVEEventHub
{
    // ========================================
    // 敵対的買収イベント
    // ========================================
    event Action<ActiveTakeoverBattle> OnBattleStarted;
    event Action<ActiveTakeoverBattle, bool> OnBattleEnded; // battle, playerWon

    // ========================================
    // 経済イベント
    // ========================================
    event Action<ActiveEconomicEvent> OnDefenseEventStarted;
    event Action<ActiveEconomicEvent, bool> OnDefenseEventEnded; // event, success

    // ========================================
    // インサイダー情報イベント
    // ========================================
    event Action<ActiveInsiderTip> OnTipReceived;
    event Action<ActiveInsiderTip> OnTipExpired;
    event Action<ActiveInsiderTip> OnTipTriggered;

    // ========================================
    // 購読管理
    // ========================================
    void Subscribe();
    void Unsubscribe();
}
