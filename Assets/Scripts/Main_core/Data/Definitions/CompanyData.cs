using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 企業の定義データ。
/// アイテムデータの「所属」欄にセットして使います。
///
/// 関連クラスは Stock/ フォルダに分割：
/// - OwnershipBonus.cs : 経営権ボーナス
/// - StockHoldingBonus.cs : 株式保有ボーナス
/// - StockEventTrigger.cs : 株価変動イベント
/// - MarketEventData.cs : グローバル市場イベント
/// - ShareholderMeetingResult.cs : 株主総会
/// - RhodesStockUIConfig.cs : ロドス株UI演出
/// - StockTradingFeatures.cs : インサイダー/空売り等
/// </summary>
[CreateAssetMenu(fileName = "New_Company", menuName = "ArknightsClicker/Company Data")]
public class CompanyData : ScriptableObject
{
    // ■ 企業ごとの「効果」の種類
    public enum CompanyTrait
    {
        None,           // なし
        TechInnovation, // 技術革新：スキルやSP回復速度アップ (ライン生命など)
        Logistics,      // 物流強化：クリック効率や生産速度アップ (ペンギン急便など)
        Military,       // 武力介入：攻撃力やクリティカル率アップ (BSWなど)
        Trading,        // 貿易特化：所持金上限や売却額アップ (カランド貿易など)
        Arts            // アーツ学：特殊リソース生成量アップ (ロドスなど)
    }

    // ■ 業種セクター（同業種連動用）
    public enum StockSector
    {
        Tech,           // テクノロジー（ライン生命、ロドスなど）
        Military,       // 軍事・傭兵（BSW、ブラックスチールなど）
        Logistics,      // 物流・運送（ペンギン急便など）
        Finance,        // 金融・貿易（カランド貿易、龍門など）
        Entertainment,  // エンタメ・サービス（シエスタなど）
        Resource        // 資源・エネルギー（ウルサスなど）
    }

    [Header("企業情報")]
    public string id;              // rhine_lab, penguin_logistics
    public string displayName;     // ライン生命, ペンギン急便
    public Sprite logo;            // 企業ロゴ
    [TextArea] public string description;

    [Header("表示設定")]
    [Tooltip("チャートの色")]
    public Color chartColor = Color.green;

    [Tooltip("企業テーマカラー")]
    public Color themeColor = Color.white;

    [Tooltip("ショップでの並び順")]
    public int sortOrder = 0;

    [Header("企業特性 (Company Trait)")]
    [Tooltip("この企業の株やアイテムを持っていると発生するボーナスの種類")]
    public CompanyTrait traitType;

    [Tooltip("ボーナスの効果量 (例: 1.1 なら 10%アップ)")]
    public float traitMultiplier = 1.0f;

    [Header("株価設定 (Stock Market)")]
    [Tooltip("初期株価（LMD）")]
    public double initialPrice = 1000;

    [Tooltip("最低株価（これ以下にはならない）")]
    public double minPrice = 10;

    [Tooltip("最高株価（0 = 無制限）")]
    public double maxPrice = 0;

    [Header("変動特性")]
    [Tooltip("ボラティリティ（変動の激しさ: 0.01〜0.5）")]
    [Range(0.01f, 0.5f)]
    public float volatility = 0.1f;

    [Tooltip("ドリフト（長期トレンド: -0.1〜0.2、正で右肩上がり）")]
    [Range(-0.1f, 0.2f)]
    public float drift = 0.02f;

    [Tooltip("ジャンプ確率（急騰/暴落の発生率: 0〜0.1）")]
    [Range(0f, 0.1f)]
    public float jumpProbability = 0.01f;

    [Tooltip("ジャンプ強度（急騰/暴落の大きさ: 0.1〜0.5）")]
    [Range(0.1f, 0.5f)]
    public float jumpIntensity = 0.2f;

    [Header("取引設定")]
    [Tooltip("取引手数料率（0〜0.05）")]
    [Range(0f, 0.05f)]
    public float transactionFee = 0.01f;

    [Tooltip("業種セクター（同セクターの株は連動しやすい）")]
    public StockSector sector = StockSector.Tech;

    [Tooltip("発行済み株式数（時価総額計算用）")]
    public long totalShares = 1000000;

    [Header("配当設定")]
    [Tooltip("配当率 (0.02 = 2%)")]
    [Range(0f, 0.1f)]
    public float dividendRate = 0f;

    [Tooltip("配当間隔（秒）- 0で配当なし")]
    public int dividendIntervalSeconds = 0;

    [Header("株式保有ボーナス")]
    [Tooltip("株式保有率に応じたボーナス（複数設定可）")]
    public List<StockHoldingBonus> holdingBonuses = new();

    [Header("解放条件")]
    [Tooltip("この株を解放するキーアイテム（null = 最初から解放）")]
    public ItemData unlockKeyItem;

    [Header("株価変動イベント")]
    [Tooltip("この企業固有のイベント（複数設定可）")]
    public List<StockEventTrigger> stockEvents = new();

    // ========================================
    // ロドス株（自社株）専用設定
    // ========================================

    [Header("自社株設定 (Rhodes Island Only)")]
    [Tooltip("自社株として扱う（クリック連動型）")]
    public bool isPlayerCompany = false;

    [Tooltip("売却可能か（falseで売却不可）")]
    public bool canSell = true;

    [Tooltip("買い戻しペナルティ率（0.1 = 10%割増）")]
    [Range(0f, 0.5f)]
    public float buybackPenalty = 0f;

    [Header("経営権ボーナス (自社株専用)")]
    [Tooltip("経営権ボーナス（保有率に応じた特殊効果）")]
    public List<OwnershipBonus> ownershipBonuses = new();

    [Header("クリック連動設定 (自社株専用)")]
    [Tooltip("クリック活発時の株価上昇率（1秒あたり）")]
    [Range(0f, 0.1f)]
    public float activeClickBonusRate = 0.02f;

    [Tooltip("放置時の株価下落率（1秒あたり）")]
    [Range(0f, 0.05f)]
    public float idleDecayRate = 0.005f;

    [Tooltip("アクティブ判定に必要なクリック数（10秒間）")]
    public int activeClickThreshold = 10;

    [Tooltip("株主総会の間隔（秒）- 0で無効")]
    public int shareholderMeetingInterval = 600;

    // ========================================
    // ヘルパーメソッド
    // ========================================

    /// <summary>
    /// 株が解放されているかチェック
    /// </summary>
    public bool IsUnlocked()
    {
        if (unlockKeyItem == null) return true;

        // InventoryManagerがあれば確認
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.GetCount(unlockKeyItem.id) > 0;
        }
        return false;
    }

    /// <summary>
    /// 特性の表示名を取得
    /// </summary>
    public string GetTraitDisplayName()
    {
        return traitType switch
        {
            CompanyTrait.Military => "軍事",
            CompanyTrait.TechInnovation => "革新",
            CompanyTrait.Logistics => "物流",
            CompanyTrait.Trading => "貿易",
            CompanyTrait.Arts => "アーツ",
            _ => "一般"
        };
    }

    /// <summary>
    /// 購入時の総コストを計算（手数料込み）
    /// </summary>
    public double CalculateBuyCost(double currentPrice, int quantity)
    {
        double baseCost = currentPrice * quantity;
        return baseCost * (1 + transactionFee);
    }

    /// <summary>
    /// 売却時の受取額を計算（手数料引き）
    /// </summary>
    public double CalculateSellReturn(double currentPrice, int quantity)
    {
        double baseReturn = currentPrice * quantity;
        return baseReturn * (1 - transactionFee);
    }
}
