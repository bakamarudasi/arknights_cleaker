using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// マーケットPVE UIの管理クラス
/// 防衛戦・敵対的買収パネルの表示/更新を担当
/// MarketUIControllerから呼び出される
/// </summary>
public class MarketPVEUIController
{
    // ========================================
    // UI要素：防衛戦
    // ========================================
    private VisualElement defenseEventPanel;
    private Label defenseTitle;
    private Label defenseSubtitle;
    private VisualElement defenseProgressFill;
    private Label defenseTimer;
    private VisualElement defenseClickArea;

    // ========================================
    // UI要素：敵対的買収
    // ========================================
    private VisualElement takeoverBattlePanel;
    private VisualElement bossIcon;
    private Label bossName;
    private Label bossTitle;
    private Label bossSpeech;
    private Label battleTarget;
    private VisualElement bossProgressFill;
    private Label bossProgressValue;
    private VisualElement playerProgressFill;
    private Label playerProgressValue;
    private Label battleTimer;
    private Button defendSmallBtn;
    private Button defendMediumBtn;
    private Button defendLargeBtn;

    // ========================================
    // UI要素：インサイダー情報
    // ========================================
    private VisualElement insiderTipPanel;
    private Label insiderSource;
    private Label insiderHintText;
    private Label insiderTarget;
    private VisualElement insiderSignal;
    private Label insiderTriggerTime;
    private Label insiderExpireTime;
    private Button insiderDismissBtn;
    private ActiveInsiderTip currentDisplayedTip;

    // ========================================
    // 参照
    // ========================================
    private VisualElement root;
    private IVisualElementScheduledItem updateTimer;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        this.root = root;

        QueryElements();
        BindUIEvents();
        BindPVEEvents();

        // 更新ループ（30fps）
        updateTimer = root.schedule.Execute(OnUpdateTick).Every(33);
    }

    private void QueryElements()
    {
        // 防衛戦パネル
        defenseEventPanel = root.Q<VisualElement>("defense-event-panel");
        defenseTitle = root.Q<Label>("defense-title");
        defenseSubtitle = root.Q<Label>("defense-subtitle");
        defenseProgressFill = root.Q<VisualElement>("defense-progress-fill");
        defenseTimer = root.Q<Label>("defense-timer");
        defenseClickArea = root.Q<VisualElement>("defense-click-area");

        // 敵対的買収パネル
        takeoverBattlePanel = root.Q<VisualElement>("takeover-battle-panel");
        bossIcon = root.Q<VisualElement>("boss-icon");
        bossName = root.Q<Label>("boss-name");
        bossTitle = root.Q<Label>("boss-title");
        bossSpeech = root.Q<Label>("boss-speech");
        battleTarget = root.Q<Label>("battle-target");
        bossProgressFill = root.Q<VisualElement>("boss-progress-fill");
        bossProgressValue = root.Q<Label>("boss-progress-value");
        playerProgressFill = root.Q<VisualElement>("player-progress-fill");
        playerProgressValue = root.Q<Label>("player-progress-value");
        battleTimer = root.Q<Label>("battle-timer");
        defendSmallBtn = root.Q<Button>("defend-small");
        defendMediumBtn = root.Q<Button>("defend-medium");
        defendLargeBtn = root.Q<Button>("defend-large");

        // インサイダー情報パネル
        insiderTipPanel = root.Q<VisualElement>("insider-tip-panel");
        insiderSource = root.Q<Label>("insider-source");
        insiderHintText = root.Q<Label>("insider-hint-text");
        insiderTarget = root.Q<Label>("insider-target");
        insiderSignal = root.Q<VisualElement>("insider-signal");
        insiderTriggerTime = root.Q<Label>("insider-trigger-time");
        insiderExpireTime = root.Q<Label>("insider-expire-time");
        insiderDismissBtn = root.Q<Button>("insider-dismiss");
    }

    private void BindUIEvents()
    {
        // 防衛戦クリック
        if (defenseClickArea != null)
        {
            defenseClickArea.RegisterCallback<ClickEvent>(OnDefenseClick);
        }

        // 買収防衛ボタン
        if (defendSmallBtn != null)
        {
            defendSmallBtn.clicked += () => OnDefendClicked(1000);
        }
        if (defendMediumBtn != null)
        {
            defendMediumBtn.clicked += () => OnDefendClicked(10000);
        }
        if (defendLargeBtn != null)
        {
            defendLargeBtn.clicked += OnDefendAllIn;
        }

        // インサイダー情報確認ボタン
        if (insiderDismissBtn != null)
        {
            insiderDismissBtn.clicked += OnInsiderDismissClicked;
        }
    }

    private void BindPVEEvents()
    {
        // 防衛戦イベント
        if (EconomicEventManager.Instance != null)
        {
            EconomicEventManager.Instance.OnEventStarted += OnDefenseEventStarted;
            EconomicEventManager.Instance.OnEventEnded += OnDefenseEventEnded;
        }

        // 買収バトル
        if (TakeoverBattleManager.Instance != null)
        {
            TakeoverBattleManager.Instance.OnBattleStarted += OnTakeoverBattleStarted;
            TakeoverBattleManager.Instance.OnBattleEnded += OnTakeoverBattleEnded;
        }

        // インサイダー情報
        if (InsiderTipManager.Instance != null)
        {
            InsiderTipManager.Instance.OnTipReceived += OnInsiderTipReceived;
            InsiderTipManager.Instance.OnTipExpired += OnInsiderTipExpired;
            InsiderTipManager.Instance.OnTipTriggered += OnInsiderTipTriggered;
        }
    }

    private void UnbindPVEEvents()
    {
        if (EconomicEventManager.Instance != null)
        {
            EconomicEventManager.Instance.OnEventStarted -= OnDefenseEventStarted;
            EconomicEventManager.Instance.OnEventEnded -= OnDefenseEventEnded;
        }

        if (TakeoverBattleManager.Instance != null)
        {
            TakeoverBattleManager.Instance.OnBattleStarted -= OnTakeoverBattleStarted;
            TakeoverBattleManager.Instance.OnBattleEnded -= OnTakeoverBattleEnded;
        }

        if (InsiderTipManager.Instance != null)
        {
            InsiderTipManager.Instance.OnTipReceived -= OnInsiderTipReceived;
            InsiderTipManager.Instance.OnTipExpired -= OnInsiderTipExpired;
            InsiderTipManager.Instance.OnTipTriggered -= OnInsiderTipTriggered;
        }
    }

    // ========================================
    // 更新ループ
    // ========================================

    private void OnUpdateTick()
    {
        UpdateDefenseEventPanel();
        UpdateTakeoverBattlePanel();
        UpdateInsiderTipPanel();
    }

    // ========================================
    // 防衛戦イベント
    // ========================================

    private void OnDefenseEventStarted(ActiveEconomicEvent evt)
    {
        ShowDefenseEventPanel(evt);
    }

    private void OnDefenseEventEnded(ActiveEconomicEvent evt, bool success)
    {
        HideDefenseEventPanel();
    }

    private void ShowDefenseEventPanel(ActiveEconomicEvent evt)
    {
        if (defenseEventPanel == null) return;

        if (defenseTitle != null)
        {
            defenseTitle.text = $"緊急: {evt.stockName} が襲撃を受けています！";
        }

        if (defenseSubtitle != null)
        {
            defenseSubtitle.text = evt.description;
        }

        defenseEventPanel.AddToClassList("visible");
    }

    private void HideDefenseEventPanel()
    {
        defenseEventPanel?.RemoveFromClassList("visible");
    }

    private void UpdateDefenseEventPanel()
    {
        var manager = EconomicEventManager.Instance;
        if (manager == null || !manager.HasActiveEvent) return;

        // 最初のアクティブイベントを表示
        var events = manager.ActiveEvents;
        if (events.Count == 0) return;

        var evt = events[0];

        // 進捗バー更新
        if (defenseProgressFill != null)
        {
            float progress = evt.Progress * 100f;
            defenseProgressFill.style.width = new Length(progress, LengthUnit.Percent);
        }

        // タイマー更新
        if (defenseTimer != null)
        {
            defenseTimer.text = $"残り {evt.remainingTime:F0}秒";
        }
    }

    private void OnDefenseClick(ClickEvent evt)
    {
        var manager = EconomicEventManager.Instance;
        if (manager == null || !manager.HasActiveEvent) return;

        var events = manager.ActiveEvents;
        if (events.Count == 0) return;

        // クリックを送信
        manager.OnSupportClick(events[0].stockId);

        // クリックエフェクト
        PlayDefenseClickEffect();
    }

    private void PlayDefenseClickEffect()
    {
        if (defenseClickArea == null) return;

        // 一瞬明るくする
        defenseClickArea.style.backgroundColor = new Color(0.29f, 0.87f, 0.5f, 0.4f);

        root.schedule.Execute(() =>
        {
            defenseClickArea.style.backgroundColor = new Color(0.29f, 0.87f, 0.5f, 0.1f);
        }).ExecuteLater(50);
    }

    // ========================================
    // 敵対的買収バトル
    // ========================================

    private void OnTakeoverBattleStarted(ActiveTakeoverBattle battle)
    {
        ShowTakeoverBattlePanel(battle);
    }

    private void OnTakeoverBattleEnded(ActiveTakeoverBattle battle, bool playerWon)
    {
        HideTakeoverBattlePanel();
    }

    private void ShowTakeoverBattlePanel(ActiveTakeoverBattle battle)
    {
        if (takeoverBattlePanel == null) return;

        if (bossName != null) bossName.text = battle.bossName;
        if (bossTitle != null) bossTitle.text = battle.bossTitle;
        if (bossSpeech != null) bossSpeech.text = $"「{battle.tauntMessage}」";
        if (battleTarget != null) battleTarget.text = $"🎯 ターゲット: {battle.stockName}";

        if (battle.bossIcon != null && bossIcon != null)
        {
            bossIcon.style.backgroundImage = new StyleBackground(battle.bossIcon);
        }

        takeoverBattlePanel.AddToClassList("visible");
    }

    private void HideTakeoverBattlePanel()
    {
        takeoverBattlePanel?.RemoveFromClassList("visible");
    }

    private void UpdateTakeoverBattlePanel()
    {
        var manager = TakeoverBattleManager.Instance;
        if (manager == null || !manager.IsBattleActive) return;

        var battle = manager.CurrentBattle;
        if (battle == null) return;

        // ボス進捗
        if (bossProgressFill != null)
        {
            bossProgressFill.style.width = new Length(battle.bossProgress * 100f, LengthUnit.Percent);
        }
        if (bossProgressValue != null)
        {
            bossProgressValue.text = $"{battle.bossProgress * 100f:F0}%";
        }

        // プレイヤー進捗
        if (playerProgressFill != null)
        {
            playerProgressFill.style.width = new Length(battle.playerProgress * 100f, LengthUnit.Percent);
        }
        if (playerProgressValue != null)
        {
            playerProgressValue.text = $"{battle.playerProgress * 100f:F0}%";
        }

        // タイマー
        if (battleTimer != null)
        {
            int minutes = (int)(battle.remainingTime / 60);
            int seconds = (int)(battle.remainingTime % 60);
            battleTimer.text = $"残り {minutes:D2}:{seconds:D2}";
        }

        // ボタンの有効/無効
        double money = WalletManager.Instance?.Money ?? 0;
        if (defendSmallBtn != null) defendSmallBtn.SetEnabled(money >= 1000);
        if (defendMediumBtn != null) defendMediumBtn.SetEnabled(money >= 10000);
        if (defendLargeBtn != null) defendLargeBtn.SetEnabled(money > 0);
    }

    private void OnDefendClicked(double amount)
    {
        TakeoverBattleManager.Instance?.TryDefend(amount);
    }

    private void OnDefendAllIn()
    {
        TakeoverBattleManager.Instance?.QuickDefend(1f); // 全額投入
    }

    // ========================================
    // インサイダー情報
    // ========================================

    private void OnInsiderTipReceived(ActiveInsiderTip tip)
    {
        ShowInsiderTipPanel(tip);
    }

    private void OnInsiderTipExpired(ActiveInsiderTip tip)
    {
        if (currentDisplayedTip?.tipId == tip.tipId)
        {
            HideInsiderTipPanel();
        }
    }

    private void OnInsiderTipTriggered(ActiveInsiderTip tip)
    {
        // ヒントが実現した時のエフェクト
        if (currentDisplayedTip?.tipId == tip.tipId)
        {
            // パネルを点滅させる
            insiderTipPanel?.AddToClassList("triggered");
            root.schedule.Execute(() =>
            {
                insiderTipPanel?.RemoveFromClassList("triggered");
            }).ExecuteLater(500);
        }
    }

    private void ShowInsiderTipPanel(ActiveInsiderTip tip)
    {
        if (insiderTipPanel == null) return;

        currentDisplayedTip = tip;

        if (insiderSource != null) insiderSource.text = $"{tip.sourceCharacter}より";
        if (insiderHintText != null) insiderHintText.text = $"「{tip.hintText}」";
        if (insiderTarget != null) insiderTarget.text = $"対象: {tip.stockName}";

        // シグナル（上昇/下降）
        if (insiderSignal != null)
        {
            insiderSignal.RemoveFromClassList("positive");
            insiderSignal.RemoveFromClassList("negative");
            insiderSignal.AddToClassList(tip.isPositive ? "positive" : "negative");
        }

        insiderTipPanel.AddToClassList("visible");
    }

    private void HideInsiderTipPanel()
    {
        insiderTipPanel?.RemoveFromClassList("visible");
        currentDisplayedTip = null;
    }

    private void UpdateInsiderTipPanel()
    {
        if (currentDisplayedTip == null) return;

        // タイマー更新
        if (insiderTriggerTime != null)
        {
            if (currentDisplayedTip.triggersIn > 0)
            {
                int minutes = (int)(currentDisplayedTip.triggersIn / 60);
                int seconds = (int)(currentDisplayedTip.triggersIn % 60);
                insiderTriggerTime.text = $"実現まで: {minutes}分{seconds:D2}秒";
            }
            else
            {
                insiderTriggerTime.text = "実現済み！";
            }
        }

        if (insiderExpireTime != null)
        {
            int minutes = (int)(currentDisplayedTip.expiresIn / 60);
            int seconds = (int)(currentDisplayedTip.expiresIn % 60);
            insiderExpireTime.text = $"有効期限: {minutes}分{seconds:D2}秒";
        }
    }

    private void OnInsiderDismissClicked()
    {
        HideInsiderTipPanel();
    }

    // ========================================
    // 破棄
    // ========================================

    public void Dispose()
    {
        UnbindPVEEvents();

        if (updateTimer != null)
        {
            updateTimer.Pause();
            updateTimer = null;
        }
    }
}
