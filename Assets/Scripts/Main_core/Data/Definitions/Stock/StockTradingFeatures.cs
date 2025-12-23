using System;
using UnityEngine;

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
}

public enum BenefitType
{
    LMDBonus,           // LMDボーナス
    GachaTicket,        // ガチャチケット
    ExpBoost,           // 経験値ブースト
    UniqueItem,         // 限定アイテム
    FacilityDiscount,   // 施設割引
    SpecialSkin         // 特別スキン解放
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
