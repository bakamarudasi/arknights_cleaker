using System;
using UnityEngine;

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
