using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// インサイダー情報の管理クラス
/// キャラクターから得た情報で、将来の株価変動を予測
///
/// 入手方法：
/// - キャラの好感度報酬
/// - クリック数マイルストーン
/// - ガチャの副産物
///
/// 疎結合設計：このクラスを削除しても他システムに影響なし
/// </summary>
public class InsiderTipManager : MonoBehaviour
{
    // ========================================
    // シングルトン
    // ========================================
    public static InsiderTipManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================
    [Header("ヒント設定")]
    [Tooltip("ヒントの有効期間（秒）")]
    [SerializeField] private float tipDuration = 600f; // 10分

    [Tooltip("ヒントが実現するまでの時間（秒）")]
    [SerializeField] private float tipTriggerDelay = 300f; // 5分後

    [Tooltip("ヒント実現時の株価変動率")]
    [SerializeField] private float tipImpactStrength = 0.15f;

    [Header("ヒントプール")]
    [SerializeField] private List<InsiderTipTemplate> tipTemplates = new();

    // ========================================
    // ランタイム状態
    // ========================================
    private List<ActiveInsiderTip> activeTips = new();
    private List<PendingTipEffect> pendingEffects = new();

    // ========================================
    // プロパティ
    // ========================================
    public IReadOnlyList<ActiveInsiderTip> ActiveTips => activeTips;
    public int ActiveTipCount => activeTips.Count;

    // ========================================
    // イベント
    // ========================================
    public event Action<ActiveInsiderTip> OnTipReceived;
    public event Action<ActiveInsiderTip> OnTipExpired;
    public event Action<ActiveInsiderTip> OnTipTriggered; // ヒントが実現した時

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
        // 好感度システムと連携（あれば）
        BindAffectionEvents();
    }

    private void Update()
    {
        UpdateActiveTips();
        UpdatePendingEffects();
    }

    private void OnDestroy()
    {
        UnbindAffectionEvents();
    }

    // ========================================
    // 好感度連携
    // ========================================

    private void BindAffectionEvents()
    {
        // AffectionManagerがあれば購読
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnAffectionLevelUp += OnAffectionLevelUp;
        }
    }

    private void UnbindAffectionEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnAffectionLevelUp -= OnAffectionLevelUp;
        }
    }

    private void OnAffectionLevelUp(string characterId, int newLevel)
    {
        // 特定レベルでインサイダー情報を付与
        if (newLevel % 5 == 0) // 5レベルごと
        {
            GiveRandomTip(characterId);
        }
    }

    // ========================================
    // ヒント管理
    // ========================================

    /// <summary>
    /// ランダムなヒントを付与
    /// </summary>
    public void GiveRandomTip(string sourceCharacter = "クロージャ")
    {
        // 対象銘柄をランダム選択
        var stocks = MarketManager.Instance?.GetUnlockedStocks();
        if (stocks == null || stocks.Count == 0) return;

        var targetStock = stocks[UnityEngine.Random.Range(0, stocks.Count)];
        bool isPositive = UnityEngine.Random.value > 0.4f; // 60%でポジティブ

        // テンプレートを選択
        var template = GetRandomTemplate(isPositive);

        GiveTip(sourceCharacter, targetStock.stockId, targetStock.companyName, isPositive, template);
    }

    /// <summary>
    /// 特定のヒントを付与
    /// </summary>
    public void GiveTip(string sourceCharacter, string stockId, string stockName, bool isPositive, InsiderTipTemplate template = null)
    {
        template ??= GetRandomTemplate(isPositive);

        string hint = template.hintText
            .Replace("{company}", stockName)
            .Replace("{character}", sourceCharacter);

        var tip = new ActiveInsiderTip
        {
            tipId = Guid.NewGuid().ToString(),
            sourceCharacter = sourceCharacter,
            stockId = stockId,
            stockName = stockName,
            hintText = hint,
            isPositive = isPositive,
            receivedTime = Time.time,
            expiresIn = tipDuration,
            triggersIn = tipTriggerDelay
        };

        activeTips.Add(tip);

        // 予定効果をスケジュール
        pendingEffects.Add(new PendingTipEffect
        {
            tipId = tip.tipId,
            stockId = stockId,
            isPositive = isPositive,
            triggerTime = Time.time + tipTriggerDelay,
            impactStrength = tipImpactStrength
        });

        // イベント発火
        var insiderTip = new InsiderTip
        {
            sourceCharacter = sourceCharacter,
            hint = hint,
            targetStockId = stockId,
            isPositive = isPositive,
            triggerTime = tipTriggerDelay
        };
        MarketEventBus.PublishInsiderTipReceived(insiderTip);
        OnTipReceived?.Invoke(tip);

        LogUIController.Msg($"📩 {sourceCharacter}: 「{hint}」");
    }

    private InsiderTipTemplate GetRandomTemplate(bool isPositive)
    {
        var filtered = tipTemplates.FindAll(t => t.isPositive == isPositive);

        if (filtered.Count == 0)
        {
            // デフォルトテンプレート
            return isPositive
                ? new InsiderTipTemplate
                {
                    hintText = "ねぇドクター、{company}がなんか新サービス始めるって噂... 私から聞いたって言わないでね？",
                    isPositive = true
                }
                : new InsiderTipTemplate
                {
                    hintText = "{company}、なんか最近やばいらしいよ... 内緒だけど",
                    isPositive = false
                };
        }

        return filtered[UnityEngine.Random.Range(0, filtered.Count)];
    }

    private void UpdateActiveTips()
    {
        for (int i = activeTips.Count - 1; i >= 0; i--)
        {
            var tip = activeTips[i];
            float elapsed = Time.time - tip.receivedTime;

            tip.expiresIn = tipDuration - elapsed;
            tip.triggersIn = Mathf.Max(0, tipTriggerDelay - elapsed);

            // 期限切れチェック
            if (tip.expiresIn <= 0)
            {
                OnTipExpired?.Invoke(tip);
                activeTips.RemoveAt(i);
            }
        }
    }

    private void UpdatePendingEffects()
    {
        for (int i = pendingEffects.Count - 1; i >= 0; i--)
        {
            var effect = pendingEffects[i];

            if (Time.time >= effect.triggerTime)
            {
                // 効果発動
                TriggerTipEffect(effect);
                pendingEffects.RemoveAt(i);
            }
        }
    }

    private void TriggerTipEffect(PendingTipEffect effect)
    {
        // 株価に影響
        MarketManager.Instance?.ApplyExternalEvent(effect.stockId, effect.impactStrength, effect.isPositive);

        // ニュースを生成
        var stock = MarketManager.Instance?.GetUnlockedStocks()?.Find(s => s.stockId == effect.stockId);
        string stockName = stock?.companyName ?? effect.stockId;

        string newsText = effect.isPositive
            ? $"【速報】{stockName}、好材料発表で急騰！"
            : $"【速報】{stockName}、悪材料で急落...";

        var news = new MarketNews(newsText,
            effect.isPositive ? MarketNewsType.Positive : MarketNewsType.Negative,
            effect.stockId,
            effect.isPositive ? effect.impactStrength : -effect.impactStrength);

        MarketEventBus.PublishNewsGenerated(news);

        // 該当するヒントを「実現済み」としてマーク
        var tip = activeTips.Find(t => t.tipId == effect.tipId);
        if (tip != null)
        {
            OnTipTriggered?.Invoke(tip);
        }
    }

    // ========================================
    // 公開API
    // ========================================

    /// <summary>
    /// 特定銘柄に関するアクティブなヒントを取得
    /// </summary>
    public ActiveInsiderTip GetTipForStock(string stockId)
    {
        return activeTips.Find(t => t.stockId == stockId);
    }

    /// <summary>
    /// ヒントがあるかチェック
    /// </summary>
    public bool HasTipForStock(string stockId)
    {
        return activeTips.Exists(t => t.stockId == stockId);
    }

    /// <summary>
    /// 全アクティブヒントを取得
    /// </summary>
    public List<ActiveInsiderTip> GetAllActiveTips()
    {
        return new List<ActiveInsiderTip>(activeTips);
    }

    // ========================================
    // デバッグ
    // ========================================

#if UNITY_EDITOR
    [ContextMenu("Give Random Tip")]
    private void DebugGiveRandomTip()
    {
        GiveRandomTip("クロージャ（デバッグ）");
    }

    [ContextMenu("Give Positive Tip")]
    private void DebugGivePositiveTip()
    {
        var stocks = MarketManager.Instance?.GetUnlockedStocks();
        if (stocks != null && stocks.Count > 0)
        {
            var stock = stocks[0];
            GiveTip("クロージャ", stock.stockId, stock.companyName, true);
        }
    }

    [ContextMenu("Give Negative Tip")]
    private void DebugGiveNegativeTip()
    {
        var stocks = MarketManager.Instance?.GetUnlockedStocks();
        if (stocks != null && stocks.Count > 0)
        {
            var stock = stocks[0];
            GiveTip("ドーベルマン", stock.stockId, stock.companyName, false);
        }
    }

    [ContextMenu("Trigger All Pending Effects")]
    private void DebugTriggerAllEffects()
    {
        foreach (var effect in pendingEffects)
        {
            effect.triggerTime = Time.time;
        }
    }
#endif
}

/// <summary>
/// インサイダーヒントのテンプレート
/// </summary>
[Serializable]
public class InsiderTipTemplate
{
    [TextArea(2, 4)]
    public string hintText = "{company}について面白い話があるんだけど...";
    public bool isPositive = true;
    public string sourceCharacterId; // 特定キャラ専用（空なら誰でも）
}

/// <summary>
/// アクティブなインサイダーヒント
/// </summary>
[Serializable]
public class ActiveInsiderTip
{
    public string tipId;
    public string sourceCharacter;
    public string stockId;
    public string stockName;
    public string hintText;
    public bool isPositive;
    public float receivedTime;
    public float expiresIn;  // 残り有効期間
    public float triggersIn; // 実現までの残り時間
}

/// <summary>
/// 保留中のヒント効果
/// </summary>
[Serializable]
public class PendingTipEffect
{
    public string tipId;
    public string stockId;
    public bool isPositive;
    public float triggerTime;
    public float impactStrength;
}
