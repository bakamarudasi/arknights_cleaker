using UnityEngine;
using System;

/// <summary>
/// ロドス自社株の管理クラス
/// プレイヤーの活動評価システム
/// - 放置しても損はしない（暴落なし）
/// - 頑張ると株価上昇 → 配当アップ
/// </summary>
public class RhodosStockManager : BaseSingleton<RhodosStockManager>
{
    // ========================================
    // 設定
    // ========================================
    [Header("株価設定")]
    [Tooltip("初期株価")]
    [SerializeField] private double initialPrice = 1000;

    [Tooltip("最低株価（これ以下にはならない）")]
    [SerializeField] private double minPrice = 100;

    [Tooltip("ベース株価の上昇係数（総資産1あたり）")]
    [SerializeField] private double assetPriceRatio = 0.001;

    [Tooltip("DPSによる株価係数")]
    [SerializeField] private double dpsPriceRatio = 0.1;

    [Header("ブースト設定")]
    [Tooltip("クリック時のブースト量")]
    [SerializeField] private double clickBoostAmount = 10;

    [Tooltip("フィーバー時の追加ブースト倍率")]
    [SerializeField] private double feverBoostMultiplier = 3.0;

    [Tooltip("ブーストの減衰速度（秒あたり）")]
    [SerializeField] private double boostDecayRate = 5.0;

    [Tooltip("最大ブースト量")]
    [SerializeField] private double maxBoost = 10000;

    [Header("配当設定")]
    [Tooltip("配当間隔（秒）")]
    [SerializeField] private float dividendInterval = 1200f; // 20分

    [Header("ランク閾値")]
    [Tooltip("Highランクの閾値（ベース比）")]
    [SerializeField] private double highThreshold = 1.5;

    [Tooltip("Superランクの閾値（ベース比）")]
    [SerializeField] private double superThreshold = 3.0;

    [Tooltip("Godランクの閾値（ベース比）")]
    [SerializeField] private double godThreshold = 10.0;

    // ========================================
    // ランタイム状態
    // ========================================
    private double basePrice;        // 総資産・DPSベースの株価
    private double currentBoost;     // 現在のブースト量
    private double currentPrice;     // 現在の株価（base + boost）
    private RhodosStockRank currentRank = RhodosStockRank.Normal;

    private float dividendTimer;
    private float lastClickTime;
    private bool isFeverActive;

    // ========================================
    // プロパティ
    // ========================================
    public double CurrentPrice => currentPrice;
    public double BasePrice => basePrice;
    public double CurrentBoost => currentBoost;
    public RhodosStockRank CurrentRank => currentRank;
    public float TimeUntilDividend => dividendInterval - dividendTimer;

    // ========================================
    // Unity ライフサイクル
    // ========================================

    private void Start()
    {
        currentPrice = initialPrice;
        basePrice = initialPrice;
        currentBoost = 0;
        dividendTimer = 0;

        BindEvents();
    }

    private void Update()
    {
        UpdateBasePrice();
        UpdateBoostDecay();
        UpdateCurrentPrice();
        UpdateRank();
        UpdateDividendTimer();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    // ========================================
    // イベントバインド
    // ========================================

    private void BindEvents()
    {
        // GameControllerのクリックイベントに接続
        if (GameController.Instance != null)
        {
            // クリックイベントがあれば購読
        }

        // SPManagerのフィーバーイベントに接続
        if (SPManager.Instance != null)
        {
            SPManager.Instance.OnFeverStarted += OnFeverStarted;
            SPManager.Instance.OnFeverEnded += OnFeverEnded;
        }
    }

    private void UnbindEvents()
    {
        if (SPManager.Instance != null)
        {
            SPManager.Instance.OnFeverStarted -= OnFeverStarted;
            SPManager.Instance.OnFeverEnded -= OnFeverEnded;
        }
    }

    // ========================================
    // 株価更新
    // ========================================

    /// <summary>
    /// ベース株価を総資産・DPSから計算
    /// </summary>
    private void UpdateBasePrice()
    {
        double totalAsset = 0;
        double dps = 0;

        if (WalletManager.Instance != null)
        {
            totalAsset = WalletManager.Instance.Money;
        }

        if (IncomeManager.Instance != null)
        {
            dps = IncomeManager.Instance.IncomePerSecond;
        }

        // ポートフォリオの評価額も加算
        if (PortfolioManager.Instance != null)
        {
            totalAsset += PortfolioManager.Instance.TotalValue;
        }

        // ベース株価 = 初期値 + 資産ベース + DPSベース
        basePrice = initialPrice + (totalAsset * assetPriceRatio) + (dps * dpsPriceRatio);
        basePrice = Math.Max(basePrice, minPrice);
    }

    /// <summary>
    /// ブーストの減衰処理
    /// </summary>
    private void UpdateBoostDecay()
    {
        if (currentBoost > 0)
        {
            // ベースラインに向けて軟着陸（暴落はしない）
            double decay = boostDecayRate * Time.deltaTime;

            // フィーバー中は減衰しない
            if (!isFeverActive)
            {
                currentBoost = Math.Max(0, currentBoost - decay);
            }
        }
    }

    /// <summary>
    /// 現在株価を計算
    /// </summary>
    private void UpdateCurrentPrice()
    {
        currentPrice = basePrice + currentBoost;
        currentPrice = Math.Max(currentPrice, minPrice);

        // イベント発火
        MarketEventBus.PublishRhodosStockUpdated(currentPrice, currentRank);
    }

    /// <summary>
    /// ランクを判定
    /// </summary>
    private void UpdateRank()
    {
        // ゼロ除算防止
        if (basePrice <= 0)
        {
            basePrice = 1;
        }
        double ratio = currentPrice / basePrice;

        RhodosStockRank newRank;
        if (ratio >= godThreshold)
        {
            newRank = RhodosStockRank.God;
        }
        else if (ratio >= superThreshold)
        {
            newRank = RhodosStockRank.Super;
        }
        else if (ratio >= highThreshold)
        {
            newRank = RhodosStockRank.High;
        }
        else
        {
            newRank = RhodosStockRank.Normal;
        }

        if (newRank != currentRank)
        {
            currentRank = newRank;
            OnRankChanged(newRank);
        }
    }

    /// <summary>
    /// 配当タイマー更新
    /// </summary>
    private void UpdateDividendTimer()
    {
        dividendTimer += Time.deltaTime;

        if (dividendTimer >= dividendInterval)
        {
            dividendTimer = 0;
            PayDividend();
        }
    }

    // ========================================
    // ブースト操作
    // ========================================

    /// <summary>
    /// クリックでブースト追加
    /// </summary>
    public void OnClick()
    {
        double boost = clickBoostAmount;

        // フィーバー中は倍率アップ
        if (isFeverActive)
        {
            boost *= feverBoostMultiplier;
        }

        AddBoost(boost);
        lastClickTime = Time.time;
    }

    /// <summary>
    /// ブーストを追加
    /// </summary>
    public void AddBoost(double amount)
    {
        currentBoost = Math.Min(currentBoost + amount, maxBoost);
    }

    /// <summary>
    /// フィーバー開始
    /// </summary>
    private void OnFeverStarted()
    {
        isFeverActive = true;

        // フィーバー開始時にボーナスブースト
        AddBoost(clickBoostAmount * 10);
    }

    /// <summary>
    /// フィーバー終了
    /// </summary>
    private void OnFeverEnded()
    {
        isFeverActive = false;
    }

    /// <summary>
    /// ランク変更時
    /// </summary>
    private void OnRankChanged(RhodosStockRank newRank)
    {
        string rankName = GetRankDisplayName(newRank);
        LogUIController.Msg($"📊 ロドス株がランク【{rankName}】に到達！");
    }

    // ========================================
    // 配当
    // ========================================

    /// <summary>
    /// 配当を支払う
    /// </summary>
    private void PayDividend()
    {
        var payment = CalculateDividend();

        // 報酬を付与
        if (WalletManager.Instance != null && payment.lmdAmount > 0)
        {
            WalletManager.Instance.AddMoney(payment.lmdAmount);
        }

        // アイテム報酬があれば付与
        if (InventoryManager.Instance != null && payment.itemRewards != null)
        {
            foreach (var reward in payment.itemRewards)
            {
                InventoryManager.Instance.Add(reward.itemId, reward.amount);
            }
        }

        // イベント発火
        MarketEventBus.PublishDividendPaid(payment);

        // ログ出力
        string rankName = GetRankDisplayName(payment.rank);
        LogUIController.Msg($"💰 【決算】ロドス株配当（{rankName}）: {payment.lmdAmount:N0} LMD");

        if (payment.rank >= RhodosStockRank.Super)
        {
            LogUIController.Msg($"🎁 ボーナス報酬も獲得！");
        }
    }

    /// <summary>
    /// 配当を計算
    /// </summary>
    public DividendPayment CalculateDividend()
    {
        var payment = new DividendPayment
        {
            rank = currentRank,
            stockPrice = currentPrice,
            lmdAmount = 0,
            expAmount = 0,
            gamaStoneAmount = 0,
            originiumAmount = 0,
            itemRewards = new System.Collections.Generic.List<ItemReward>()
        };

        // ベース報酬（株価の10%）
        double baseReward = currentPrice * 0.1;

        switch (currentRank)
        {
            case RhodosStockRank.Normal:
                // 少額のLMD
                payment.lmdAmount = baseReward;
                break;

            case RhodosStockRank.High:
                // LMD + 作戦記録（EXP）
                payment.lmdAmount = baseReward * 1.5;
                payment.expAmount = 100;
                payment.itemRewards.Add(new ItemReward { itemId = "exp_record_1", amount = 5 });
                break;

            case RhodosStockRank.Super:
                // LMD + 合成玉
                payment.lmdAmount = baseReward * 2.0;
                payment.expAmount = 200;
                payment.gamaStoneAmount = 100;
                payment.itemRewards.Add(new ItemReward { itemId = "exp_record_2", amount = 3 });
                break;

            case RhodosStockRank.God:
                // LMD + 純正源石 + 上級素材
                payment.lmdAmount = baseReward * 3.0;
                payment.expAmount = 500;
                payment.gamaStoneAmount = 300;
                payment.originiumAmount = 1;
                payment.itemRewards.Add(new ItemReward { itemId = "exp_record_3", amount = 2 });
                payment.itemRewards.Add(new ItemReward { itemId = "pure_gold", amount = 5 });
                break;
        }

        return payment;
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// ランクの表示名を取得
    /// </summary>
    public static string GetRankDisplayName(RhodosStockRank rank)
    {
        return rank switch
        {
            RhodosStockRank.Normal => "NORMAL",
            RhodosStockRank.High => "HIGH",
            RhodosStockRank.Super => "SUPER",
            RhodosStockRank.God => "GOD",
            _ => "---"
        };
    }

    /// <summary>
    /// ランクのCSSクラス名を取得
    /// </summary>
    public static string GetRankClassName(RhodosStockRank rank)
    {
        return rank switch
        {
            RhodosStockRank.High => "rank-high",
            RhodosStockRank.Super => "rank-super",
            RhodosStockRank.God => "rank-god",
            _ => ""
        };
    }

    /// <summary>
    /// 配当までの残り時間をフォーマット
    /// </summary>
    public string GetDividendTimerText()
    {
        float remaining = TimeUntilDividend;
        int minutes = (int)(remaining / 60);
        int seconds = (int)(remaining % 60);
        return $"次回配当: {minutes:D2}:{seconds:D2}";
    }

    /// <summary>
    /// 株価をフォーマット
    /// </summary>
    public string GetPriceText()
    {
        return $"₿ {StockPriceEngine.FormatPrice(currentPrice)}";
    }

    // ========================================
    // デバッグ用
    // ========================================

#if UNITY_EDITOR
    [ContextMenu("Force Dividend")]
    private void DebugForceDividend()
    {
        PayDividend();
    }

    [ContextMenu("Add Max Boost")]
    private void DebugAddMaxBoost()
    {
        AddBoost(maxBoost);
    }

    [ContextMenu("Reset Boost")]
    private void DebugResetBoost()
    {
        currentBoost = 0;
    }
#endif
}
