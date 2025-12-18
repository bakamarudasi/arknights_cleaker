using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 敵対的買収（ボス戦）の管理クラス
/// 謎の投資家（ボス）が銘柄を買い占めようとする
/// プレイヤーはLMDを使って対抗買いで撃退
///
/// 疎結合設計：このクラスを削除しても他システムに影響なし
/// </summary>
public class TakeoverBattleManager : MonoBehaviour
{
    // ========================================
    // シングルトン
    // ========================================
    public static TakeoverBattleManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================
    [Header("ボス出現設定")]
    [Tooltip("ボス出現チェック間隔（秒）")]
    [SerializeField] private float checkInterval = 300f; // 5分

    [Tooltip("ボス出現確率（0〜1）")]
    [SerializeField] private float spawnProbability = 0.2f;

    [Header("バトル設定")]
    [Tooltip("バトル継続時間（秒）")]
    [SerializeField] private float battleDuration = 120f;

    [Tooltip("ボスの基本資金（株価の倍率）")]
    [SerializeField] private float bossBudgetMultiplier = 10f;

    [Tooltip("ボスの買い速度（秒あたりの進捗%）")]
    [SerializeField] private float bossAttackSpeed = 0.01f;

    [Header("報酬設定")]
    [Tooltip("勝利時のLMD報酬倍率")]
    [SerializeField] private float victoryRewardMultiplier = 2f;

    [Tooltip("勝利時のボーナス株数")]
    [SerializeField] private int victoryBonusShares = 10;

    [Header("ボスデータ")]
    [SerializeField] private List<TakeoverBossData> bossPool = new();

    // ========================================
    // ランタイム状態
    // ========================================
    private ActiveTakeoverBattle currentBattle;
    private float checkTimer;

    // ========================================
    // プロパティ
    // ========================================
    public ActiveTakeoverBattle CurrentBattle => currentBattle;
    public bool IsBattleActive => currentBattle != null;

    // ========================================
    // イベント
    // ========================================
    public event Action<ActiveTakeoverBattle> OnBattleStarted;
    public event Action<ActiveTakeoverBattle, bool> OnBattleEnded; // battle, playerWon

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
        if (!IsBattleActive)
        {
            UpdateCheckTimer();
        }
        else
        {
            UpdateBattle();
        }
    }

    // ========================================
    // ボス出現チェック
    // ========================================

    private void UpdateCheckTimer()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0;
            TrySpawnBoss();
        }
    }

    private void TrySpawnBoss()
    {
        // 確率チェック
        if (UnityEngine.Random.value > spawnProbability) return;

        // プレイヤーが株を持っている銘柄から選ぶ
        var holdings = PortfolioManager.Instance?.GetHoldingSummaries();
        if (holdings == null || holdings.Count == 0) return;

        // ランダムに選択
        var target = holdings[UnityEngine.Random.Range(0, holdings.Count)];

        // ボスを選択
        var boss = GetRandomBoss();

        // バトル開始
        StartBattle(target.stockId, target.companyName, boss);
    }

    private TakeoverBossData GetRandomBoss()
    {
        if (bossPool.Count == 0)
        {
            // デフォルトボス
            return new TakeoverBossData
            {
                bossName = "謎の投資家",
                bossTitle = "闇のファンドマネージャー",
                tauntMessage = "この会社、いただくよ",
                defeatMessage = "ぐぬぬ...覚えてろ！"
            };
        }

        return bossPool[UnityEngine.Random.Range(0, bossPool.Count)];
    }

    // ========================================
    // バトル管理
    // ========================================

    private void StartBattle(string stockId, string stockName, TakeoverBossData boss)
    {
        double currentPrice = MarketManager.Instance?.GetCurrentPrice(stockId) ?? 1000;
        int playerHoldings = PortfolioManager.Instance?.GetHoldingQuantity(stockId) ?? 0;

        currentBattle = new ActiveTakeoverBattle
        {
            battleId = Guid.NewGuid().ToString(),
            stockId = stockId,
            stockName = stockName,
            bossName = boss.bossName,
            bossTitle = boss.bossTitle,
            bossIcon = boss.bossIcon,
            tauntMessage = boss.tauntMessage,
            defeatMessage = boss.defeatMessage,
            duration = battleDuration,
            remainingTime = battleDuration,
            bossBudget = currentPrice * bossBudgetMultiplier,
            bossProgress = 0,
            playerDefenseTotal = 0,
            playerProgress = 0,
            targetShares = playerHoldings
        };

        // イベント発火
        var eventData = CreateEventData(currentBattle);
        MarketEventBus.PublishTakeoverStarted(eventData);
        OnBattleStarted?.Invoke(currentBattle);

        LogUIController.Msg($"👤 {boss.bossName}「{boss.tauntMessage}」");
        LogUIController.Msg($"⚔️ {stockName} を巡る買収バトル開始！");
    }

    private void UpdateBattle()
    {
        if (currentBattle == null) return;

        currentBattle.remainingTime -= Time.deltaTime;

        // ボスの進捗を増加
        currentBattle.bossProgress += bossAttackSpeed * Time.deltaTime;
        currentBattle.bossProgress = Mathf.Clamp01(currentBattle.bossProgress);

        // 勝敗判定
        if (currentBattle.playerProgress >= 1f)
        {
            EndBattle(true);
        }
        else if (currentBattle.bossProgress >= 1f || currentBattle.remainingTime <= 0)
        {
            EndBattle(false);
        }
    }

    private void EndBattle(bool playerWon)
    {
        if (currentBattle == null) return;

        var eventData = CreateEventData(currentBattle);
        MarketEventBus.PublishTakeoverEnded(eventData, playerWon);
        OnBattleEnded?.Invoke(currentBattle, playerWon);

        if (playerWon)
        {
            // 勝利報酬
            double reward = currentBattle.playerDefenseTotal * victoryRewardMultiplier;
            WalletManager.Instance?.AddMoney(reward);

            // ボーナス株付与
            PortfolioManager.Instance?.TryBuyStock(currentBattle.stockId, victoryBonusShares);

            // 株価上昇
            MarketManager.Instance?.ApplyExternalEvent(currentBattle.stockId, 0.15f, true);

            LogUIController.Msg($"🎉 買収防衛成功！ {currentBattle.bossName}「{currentBattle.defeatMessage}」");
            LogUIController.Msg($"💰 報酬: {reward:N0} LMD + {victoryBonusShares}株");
        }
        else
        {
            // 敗北：株を没収
            int lostShares = Mathf.Min(currentBattle.targetShares, PortfolioManager.Instance?.GetHoldingQuantity(currentBattle.stockId) ?? 0);
            if (lostShares > 0)
            {
                // 強制売却（収益なし）
                PortfolioManager.Instance?.TrySellStock(currentBattle.stockId, lostShares);
                // 売却益を取り消し（ボスに奪われた設定）
            }

            // 株価下落
            MarketManager.Instance?.ApplyExternalEvent(currentBattle.stockId, 0.2f, false);

            LogUIController.Msg($"💀 買収されてしまった... {currentBattle.stockName}株を失いました");
        }

        currentBattle = null;
    }

    private TakeoverEventData CreateEventData(ActiveTakeoverBattle battle)
    {
        return new TakeoverEventData
        {
            eventId = battle.battleId,
            targetStockId = battle.stockId,
            attackerName = battle.bossName,
            attackerTitle = battle.bossTitle,
            duration = battle.duration,
            attackerBudget = battle.bossBudget,
            playerDefense = battle.playerDefenseTotal,
            attackerProgress = battle.bossProgress,
            playerProgress = battle.playerProgress
        };
    }

    // ========================================
    // プレイヤーアクション
    // ========================================

    /// <summary>
    /// 対抗買い（LMDを使って防衛）
    /// </summary>
    public bool TryDefend(double amount)
    {
        if (currentBattle == null) return false;

        // 残高チェック
        if (WalletManager.Instance == null || !WalletManager.Instance.CanAfford(amount))
        {
            return false;
        }

        // LMDを消費
        WalletManager.Instance.SpendMoney(amount);

        // 防衛進捗を増加
        currentBattle.playerDefenseTotal += amount;
        currentBattle.playerProgress = (float)(currentBattle.playerDefenseTotal / currentBattle.bossBudget);
        currentBattle.playerProgress = Mathf.Clamp01(currentBattle.playerProgress);

        // ボスの進捗を少し押し戻す
        currentBattle.bossProgress -= (float)(amount / currentBattle.bossBudget) * 0.5f;
        currentBattle.bossProgress = Mathf.Max(0, currentBattle.bossProgress);

        return true;
    }

    /// <summary>
    /// クイック防衛（所持金の一定割合を投入）
    /// </summary>
    public bool QuickDefend(float percentOfMoney)
    {
        if (WalletManager.Instance == null) return false;

        double amount = WalletManager.Instance.Money * percentOfMoney;
        return TryDefend(amount);
    }

    // ========================================
    // デバッグ
    // ========================================

#if UNITY_EDITOR
    [ContextMenu("Force Spawn Boss")]
    private void DebugForceSpawnBoss()
    {
        spawnProbability = 1f;
        checkTimer = checkInterval;
    }

    [ContextMenu("Win Current Battle")]
    private void DebugWinBattle()
    {
        if (currentBattle != null)
        {
            currentBattle.playerProgress = 1f;
        }
    }

    [ContextMenu("Lose Current Battle")]
    private void DebugLoseBattle()
    {
        if (currentBattle != null)
        {
            currentBattle.bossProgress = 1f;
        }
    }
#endif
}

/// <summary>
/// 敵対的買収ボスのデータ
/// </summary>
[Serializable]
public class TakeoverBossData
{
    [Header("基本情報")]
    public string bossName = "謎の投資家";
    public string bossTitle = "闇のファンドマネージャー";
    public Sprite bossIcon;

    [Header("セリフ")]
    [TextArea(1, 2)]
    public string tauntMessage = "この会社、いただくよ";
    [TextArea(1, 2)]
    public string defeatMessage = "ぐぬぬ...覚えてろ！";

    [Header("特殊パラメータ")]
    [Tooltip("攻撃速度倍率")]
    public float attackSpeedMultiplier = 1f;
    [Tooltip("必要防衛資金倍率")]
    public float budgetMultiplier = 1f;
}

/// <summary>
/// アクティブな買収バトル
/// </summary>
[Serializable]
public class ActiveTakeoverBattle
{
    public string battleId;
    public string stockId;
    public string stockName;
    public string bossName;
    public string bossTitle;
    public Sprite bossIcon;
    public string tauntMessage;
    public string defeatMessage;
    public float duration;
    public float remainingTime;
    public double bossBudget;
    public float bossProgress;     // 0-1
    public double playerDefenseTotal;
    public float playerProgress;   // 0-1
    public int targetShares;       // 奪われる株数

    public float TimeProgress => duration > 0 ? remainingTime / duration : 0;
}
