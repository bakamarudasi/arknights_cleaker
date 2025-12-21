using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 企業の定義データ。
/// アイテムデータの「所属」欄にセットして使います。
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

/// <summary>
/// 経営権ボーナス（ロドス株専用）：保有比率に応じた特殊効果
/// </summary>
[Serializable]
public class OwnershipBonus
{
    [Tooltip("必要な保有率 (0.51 = 51%で過半数)")]
    [Range(0.01f, 1f)]
    public float requiredOwnership = 0.51f;

    [Tooltip("ボーナスの種類")]
    public OwnershipBonusType bonusType;

    [Tooltip("効果量")]
    public float effectValue = 0.1f;

    [Tooltip("ボーナスの説明")]
    public string description;

    [Tooltip("喪失時の演出メッセージ")]
    public string lostMessage;

    public enum OwnershipBonusType
    {
        ClickEfficiencyBase,    // クリック効率の基本値
        AllFacilityBoost,       // 全施設効率アップ
        GachaDiscount,          // ガチャコスト割引
        AutoClickUnlock,        // オートクリック解除
        UICustomization,        // UI変更権限（乗っ取られ演出防止）
        DoctorTitle,            // ドクター称号表示
        KelseySupportBonus      // ケルシーサポートボーナス
    }
}

/// <summary>
/// 保有ボーナスの効果タイプ
/// </summary>
public enum HoldingBonusType
{
    UpgradeCostReduction,   // 強化費用軽減（effectValue=0.1なら10%オフ）
    ClickEfficiency,        // クリック効率アップ
    AutoIncomeBoost,        // 自動収入アップ
    GachaRateUp,            // ガチャ確率アップ（この企業のキャラ）
    DividendBonus,          // 配当金ボーナス
    ExpBonus,               // 経験値ボーナス
    CriticalRate,           // クリティカル率アップ
    SellPriceBonus,         // 売却価格アップ
    TransactionFeeReduction // 取引手数料軽減
}

/// <summary>
/// 株式保有ボーナス：一定以上の保有率で効果発動
/// </summary>
[Serializable]
public class StockHoldingBonus
{
    [Tooltip("必要な保有率 (0.1 = 10%)")]
    [Range(0.01f, 1f)]
    public float requiredHoldingRate = 0.1f;

    [Tooltip("ボーナスの種類")]
    public HoldingBonusType bonusType;

    [Tooltip("効果量 (例: 0.1 = 10%減, 1.2 = 20%増)")]
    public float effectValue = 0.1f;

    [Tooltip("ボーナスの説明文")]
    public string description;
}

/// <summary>
/// 株価変動イベントトリガー
/// </summary>
[Serializable]
public class StockEventTrigger
{
    [Tooltip("イベント名")]
    public string eventName;

    [Tooltip("イベントの説明")]
    [TextArea]
    public string description;

    [Tooltip("トリガー条件")]
    public EventTriggerType triggerType;

    [Tooltip("株価への影響 (-0.3 = 30%下落, 0.5 = 50%上昇)")]
    [Range(-0.5f, 1f)]
    public float priceImpact = 0.1f;

    [Tooltip("影響の持続時間（秒）- 0で恒久")]
    public float durationSeconds = 300f;

    [Tooltip("発生確率 (1日あたり) - RandomDaily時のみ")]
    [Range(0f, 1f)]
    public float dailyProbability = 0.1f;

    [Tooltip("同セクター連動率 (0.5 = 50%の影響)")]
    [Range(0f, 1f)]
    public float sectorCorrelation = 0.3f;

    [Tooltip("イベント発生時の通知メッセージ")]
    public string notificationMessage;

    public enum EventTriggerType
    {
        RandomDaily,            // 毎日ランダムで発生
        OnGachaResult,          // ガチャでこの企業キャラが出た時
        OnUpgradePurchase,      // この企業関連の強化購入時
        OnMilestone,            // 特定マイルストーン達成時
        OnStockPurchase,        // 大量株式購入時（需要増）
        OnStockSell,            // 大量株式売却時（供給増）
        OnMarketCrash,          // 市場暴落時
        OnMarketBoom,           // 市場好況時
        Scheduled               // 特定時間に発生（決算発表など）
    }
}

/// <summary>
/// グローバル市場イベント（CompanyDataとは別で管理）
/// </summary>
[CreateAssetMenu(fileName = "New_MarketEvent", menuName = "ArknightsClicker/Stock/Market Event")]
public class MarketEventData : ScriptableObject
{
    [Header("イベント基本情報")]
    public string eventId;
    public string eventName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("影響設定")]
    [Tooltip("全体市場への影響 (-0.2 = 20%下落)")]
    [Range(-0.5f, 0.5f)]
    public float globalImpact = 0f;

    [Tooltip("特定セクターへの追加影響")]
    public List<SectorImpact> sectorImpacts = new();

    [Tooltip("特定企業への追加影響")]
    public List<CompanyImpact> companyImpacts = new();

    [Header("発生条件")]
    [Tooltip("発生確率（1日あたり）")]
    [Range(0f, 1f)]
    public float dailyProbability = 0.05f;

    [Tooltip("影響持続時間（秒）")]
    public float durationSeconds = 600f;

    [Tooltip("イベントの重大度")]
    public EventSeverity severity = EventSeverity.Normal;

    public enum EventSeverity
    {
        Minor,      // 小規模（±5%程度）
        Normal,     // 通常（±10%程度）
        Major,      // 大規模（±20%程度）
        Critical    // 危機的（±30%以上）
    }
}

[Serializable]
public class SectorImpact
{
    public CompanyData.StockSector sector;
    [Range(-0.5f, 0.5f)]
    public float impact;
}

[Serializable]
public class CompanyImpact
{
    public CompanyData company;
    [Range(-0.5f, 0.5f)]
    public float impact;
}

// ========================================
// 株主総会（ロドス株専用イベント）
// ========================================

/// <summary>
/// 株主総会の評価結果
/// </summary>
[CreateAssetMenu(fileName = "New_MeetingResult", menuName = "ArknightsClicker/Stock/Shareholder Meeting Result")]
public class ShareholderMeetingResult : ScriptableObject
{
    [Header("評価条件")]
    [Tooltip("この評価が適用される株価の下限")]
    public float minStockPrice = 0f;

    [Tooltip("この評価が適用される株価の上限")]
    public float maxStockPrice = 100f;

    [Header("評価内容")]
    public MeetingGrade grade;

    [Tooltip("評価者（ケルシーなど）")]
    public string evaluatorName = "ケルシー";

    [Tooltip("評価コメント")]
    [TextArea]
    public string comment;

    [Tooltip("追加の演出メッセージ（Mon3tr召喚など）")]
    public string extraMessage;

    [Header("効果")]
    [Tooltip("報酬/罰則の種類")]
    public List<MeetingEffect> effects = new();

    public enum MeetingGrade
    {
        Excellent,  // 優秀：株価が非常に高い
        Good,       // 良好：株価が高め
        Normal,     // 普通：株価が平均的
        Poor,       // 不調：株価が低め
        Critical    // 危機的：株価が暴落状態
    }
}

[Serializable]
public class MeetingEffect
{
    public MeetingEffectType effectType;
    public float value;
    public float durationSeconds; // 0で永続（次の総会まで）

    public enum MeetingEffectType
    {
        // 報酬系
        BonusLMD,               // LMDボーナス
        FacilityBoost,          // 施設効率アップ
        ClickEfficiencyBoost,   // クリック効率アップ
        GachaTicket,            // ガチャチケット付与

        // 罰則系
        FacilityDebuff,         // 施設効率ダウン（士気低下）
        ClickEfficiencyDebuff,  // クリック効率ダウン
        IncomeReduction         // 収入減少
    }
}

/// <summary>
/// ロドス株のUI演出設定
/// </summary>
[Serializable]
public class RhodesStockUIConfig
{
    [Header("キャラクター表情連動")]
    [Tooltip("株価低下時にアーミヤの表情が曇る")]
    public bool enableAmiyaMoodSync = true;

    [Tooltip("表情が変わる株価閾値（低い順）")]
    public float[] moodThresholds = { 30f, 50f, 70f, 90f };

    [Header("ニュースティッカー")]
    [Tooltip("暴落時のニュースメッセージ")]
    [TextArea]
    public string[] crashNewsMessages = {
        "【噂】ドクター、実はただの無職か？ 投資家が疑念",
        "【速報】ロドス製薬、資金繰り悪化の噂",
        "【独自】関係者「ドクターは最近寝てばかり」"
    };

    [Tooltip("急騰時のニュースメッセージ")]
    [TextArea]
    public string[] boomNewsMessages = {
        "【速報】ロドス製薬、驚異の業績！ドクターの手腕に注目",
        "【市況】ロドス株がストップ高、投資家殺到",
        "【独自】ドクター「まだまだこれから」と自信"
    };

    [Header("乗っ取り演出")]
    [Tooltip("経営権喪失時のUI変更を有効化")]
    public bool enableTakeoverUI = true;

    [Tooltip("乗っ取り時のメッセージ")]
    public string takeoverMessage = "新経営陣より：ドクターの権限は一時的に制限されています。";

    [Header("危機演出")]
    [Tooltip("株価急落時の警報サイレン演出")]
    public bool enableCrisisAlarm = true;

    [Tooltip("危機時にオペレーターが励ましてくれる")]
    public bool enableOperatorEncouragement = true;

    [Tooltip("励ましメッセージ（ランダム選択）")]
    [TextArea]
    public string[] encouragementMessages = {
        "ドクター、私たちがついています。（アーミヤ）",
        "経営状況は芳しくありませんが、諦めるのはまだ早い。（ケルシー）",
        "ボス、金がなくてもペンギン急便は止まらないぜ！（テキサス）",
        "ドクター殿、逆境こそが真の力を試す時でござる！（シラユキ）"
    };

    [Header("株価ストリーク演出")]
    [Tooltip("連続上昇時の特殊演出を有効化")]
    public bool enableStreakEffects = true;

    [Tooltip("ストリーク達成時のメッセージ")]
    public string[] streakMessages = {
        "3連騰！調子いいですね、ドクター！",
        "5連騰！投資家の信頼が高まっています！",
        "7連騰！伝説のドクターと呼ばれ始めました！",
        "10連騰！！ロドスの黄金時代到来か！？"
    };

    // ========================================
    // Null安全ヘルパーメソッド
    // ========================================

    /// <summary>
    /// 株価に対応する表情レベルを取得（0=最悪, 配列長=最高）
    /// </summary>
    public int GetMoodLevel(float stockPrice)
    {
        if (moodThresholds == null || moodThresholds.Length == 0)
            return 2; // デフォルト：普通

        int level = 0;
        foreach (var threshold in moodThresholds)
        {
            if (stockPrice >= threshold) level++;
            else break;
        }
        return level;
    }

    /// <summary>
    /// 暴落時のランダムニュースを取得
    /// </summary>
    public string GetRandomCrashNews()
    {
        if (crashNewsMessages == null || crashNewsMessages.Length == 0)
            return "【速報】市場が不安定な状況です";
        return crashNewsMessages[UnityEngine.Random.Range(0, crashNewsMessages.Length)];
    }

    /// <summary>
    /// 急騰時のランダムニュースを取得
    /// </summary>
    public string GetRandomBoomNews()
    {
        if (boomNewsMessages == null || boomNewsMessages.Length == 0)
            return "【速報】市場が好調です";
        return boomNewsMessages[UnityEngine.Random.Range(0, boomNewsMessages.Length)];
    }

    /// <summary>
    /// 励ましメッセージをランダム取得
    /// </summary>
    public string GetRandomEncouragement()
    {
        if (encouragementMessages == null || encouragementMessages.Length == 0)
            return "頑張りましょう、ドクター。";
        return encouragementMessages[UnityEngine.Random.Range(0, encouragementMessages.Length)];
    }

    /// <summary>
    /// 連騰数に対応するストリークメッセージを取得
    /// </summary>
    public string GetStreakMessage(int streakCount)
    {
        if (streakMessages == null || streakMessages.Length == 0)
            return $"{streakCount}連騰達成！";

        // 3, 5, 7, 10連騰に対応
        int index = streakCount switch
        {
            >= 10 => 3,
            >= 7 => 2,
            >= 5 => 1,
            >= 3 => 0,
            _ => -1
        };

        if (index < 0 || index >= streakMessages.Length)
            return $"{streakCount}連騰達成！";

        return streakMessages[index];
    }

    /// <summary>
    /// 乗っ取りメッセージを取得（null安全）
    /// </summary>
    public string GetTakeoverMessage()
    {
        return string.IsNullOrEmpty(takeoverMessage)
            ? "経営権が移譲されました。"
            : takeoverMessage;
    }
}

// ========================================
// 追加の面白要素
// ========================================

/// <summary>
/// インサイダー情報（事前に株価変動を知れる）
/// </summary>
[Serializable]
public class InsiderInfo
{
    [Tooltip("情報の信頼度 (0-1, 低いとガセの可能性)")]
    [Range(0f, 1f)]
    public float reliability = 0.8f;

    [Tooltip("対象企業")]
    public CompanyData targetCompany;

    [Tooltip("予測される変動方向 (true=上昇, false=下落)")]
    public bool predictedRise;

    [Tooltip("情報の有効期限（秒）")]
    public float expiresInSeconds = 300f;

    [Tooltip("情報提供者")]
    public string informant;

    [Tooltip("情報の内容")]
    [TextArea]
    public string rumor;
}

/// <summary>
/// 株式分割・併合イベント
/// </summary>
[Serializable]
public class StockSplitEvent
{
    public enum SplitType
    {
        Split,      // 分割（株数増加、単価下落）
        Merge       // 併合（株数減少、単価上昇）
    }

    public SplitType type;

    [Tooltip("分割/併合比率 (2 = 1株→2株 or 2株→1株)")]
    public int ratio = 2;

    [Tooltip("発動条件：株価がこの値を超えたら分割")]
    public float splitTriggerPrice = 1000f;

    [Tooltip("発動条件：株価がこの値を下回ったら併合")]
    public float mergeTriggerPrice = 10f;
}

/// <summary>
/// 相場操縦検知システム（短時間の大量売買でペナルティ）
/// </summary>
[Serializable]
public class MarketManipulationDetector
{
    [Tooltip("監視期間（秒）")]
    public float monitoringPeriod = 60f;

    [Tooltip("警告閾値：この割合以上の売買で警告")]
    [Range(0.01f, 0.5f)]
    public float warningThreshold = 0.1f;

    [Tooltip("ペナルティ閾値：この割合以上で取引停止")]
    [Range(0.05f, 0.5f)]
    public float penaltyThreshold = 0.2f;

    [Tooltip("取引停止期間（秒）")]
    public float suspensionDuration = 300f;

    [Tooltip("ペナルティメッセージ")]
    public string penaltyMessage = "【警告】短期間での大量取引が検知されました。取引が一時停止されます。";
}

/// <summary>
/// 緊急資金調達オプション（高利で即金を得る）
/// </summary>
[Serializable]
public class EmergencyFunding
{
    public enum FundingType
    {
        StockPledge,        // 株式担保融資
        ConvertibleBond,    // 転換社債発行
        PrivatePlacement,   // 第三者割当増資
        AssetSale           // 資産売却（施設を売る）
    }

    public FundingType type;

    [Tooltip("調達可能額の倍率（保有株価値に対して）")]
    public float fundingMultiplier = 0.5f;

    [Tooltip("金利/手数料率")]
    [Range(0f, 0.5f)]
    public float interestRate = 0.15f;

    [Tooltip("返済期限（秒）- 0で無期限")]
    public float repaymentDeadline = 600f;

    [Tooltip("返済不能時のペナルティ")]
    public string defaultPenalty;
}

/// <summary>
/// 株主優待システム
/// </summary>
[Serializable]
public class ShareholderBenefit
{
    [Tooltip("必要保有株数")]
    public int requiredShares = 100;

    [Tooltip("優待の種類")]
    public BenefitType benefitType;

    [Tooltip("優待の効果値")]
    public float effectValue;

    [Tooltip("優待の説明")]
    public string description;

    [Tooltip("優待付与間隔（秒）")]
    public float intervalSeconds = 3600f;

    public enum BenefitType
    {
        LMDBonus,           // LMDボーナス
        GachaTicket,        // ガチャチケット
        ExpBoost,           // 経験値ブースト
        UniqueItem,         // 限定アイテム
        FacilityDiscount,   // 施設割引
        SpecialSkin         // 特別スキン解放
    }
}

/// <summary>
/// 空売りシステム（株価下落で利益）
/// </summary>
[Serializable]
public class ShortSellingConfig
{
    [Tooltip("空売り可能か")]
    public bool enabled = true;

    [Tooltip("空売り手数料率")]
    [Range(0f, 0.1f)]
    public float borrowingFee = 0.02f;

    [Tooltip("最大空売り期間（秒）")]
    public float maxDuration = 1800f;

    [Tooltip("強制決済閾値（損失率がこれを超えると強制決済）")]
    [Range(0.1f, 1f)]
    public float forcedCoverThreshold = 0.5f;

    [Tooltip("ショートスクイーズ発生確率（空売り過多時）")]
    [Range(0f, 0.5f)]
    public float shortSqueezeProbability = 0.1f;
}