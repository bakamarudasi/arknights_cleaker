using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 株式周回（プレステージ）の設定データ
/// 100%買い占め時のボーナスと難易度上昇を定義
/// </summary>
[CreateAssetMenu(fileName = "New_StockPrestige", menuName = "ArknightsClicker/Market/Stock Prestige Data")]
public class StockPrestigeData : ScriptableObject
{
    [Header("対象銘柄")]
    [Tooltip("この周回設定が適用される銘柄")]
    public StockData targetStock;

    [Header("周回設定")]
    [Tooltip("周回ごとのtotalShares倍率（1.5 = 50%増加）")]
    [Range(1.1f, 3f)]
    public float sharesMultiplier = 1.5f;

    [Tooltip("最大周回数（0 = 無制限）")]
    public int maxPrestigeLevel = 0;

    [Header("永続ボーナス")]
    [Tooltip("周回ごとに獲得する永続ボーナス")]
    public List<PrestigeBonus> prestigeBonuses = new();

    [Header("買収完了演出")]
    [Tooltip("買収完了時のメッセージ")]
    public string acquisitionMessage = "{0}の買収が完了しました！";

    [Tooltip("買収完了時の効果音")]
    public AudioClip acquisitionSound;

    // ========================================
    // 計算ヘルパー
    // ========================================

    /// <summary>
    /// 指定周回数でのtotalSharesを計算
    /// </summary>
    public long CalculateTotalShares(int prestigeLevel)
    {
        if (targetStock == null) return 1000000;

        long baseShares = targetStock.totalShares;
        return (long)(baseShares * Math.Pow(sharesMultiplier, prestigeLevel));
    }

    /// <summary>
    /// 指定周回数での累計ボーナスを計算
    /// </summary>
    public float GetTotalBonus(PrestigeBonusType type, int prestigeLevel)
    {
        float total = 0f;
        foreach (var bonus in prestigeBonuses)
        {
            if (bonus.bonusType == type)
            {
                total += bonus.valuePerLevel * prestigeLevel;
            }
        }
        return total;
    }

    /// <summary>
    /// 最大周回に達しているか
    /// </summary>
    public bool IsMaxLevel(int prestigeLevel)
    {
        return maxPrestigeLevel > 0 && prestigeLevel >= maxPrestigeLevel;
    }
}

/// <summary>
/// 周回ボーナスの種類
/// </summary>
public enum PrestigeBonusType
{
    ClickEfficiency,        // クリック効率
    AutoIncome,             // 自動収入
    CriticalRate,           // クリティカル率
    CriticalPower,          // クリティカル倍率
    SPChargeSpeed,          // SP回復速度
    FeverPower,             // フィーバー倍率
    SellPriceBonus,         // 売却価格
    GachaCostReduction,     // ガチャコスト軽減
    UpgradeCostReduction,   // 強化コスト軽減
    DividendBonus           // 配当金ボーナス
}

/// <summary>
/// 周回ボーナス定義
/// </summary>
[Serializable]
public class PrestigeBonus
{
    [Tooltip("ボーナスの種類")]
    public PrestigeBonusType bonusType;

    [Tooltip("1周回あたりのボーナス値（0.05 = 5%）")]
    public float valuePerLevel = 0.05f;

    [Tooltip("ボーナスの説明")]
    public string description;

    /// <summary>
    /// 表示用の説明を生成
    /// </summary>
    public string GetDisplayText(int level)
    {
        float totalValue = valuePerLevel * level;
        return $"{GetBonusTypeName()}: +{totalValue * 100:F1}%";
    }

    private string GetBonusTypeName()
    {
        return bonusType switch
        {
            PrestigeBonusType.ClickEfficiency => "クリック効率",
            PrestigeBonusType.AutoIncome => "自動収入",
            PrestigeBonusType.CriticalRate => "クリティカル率",
            PrestigeBonusType.CriticalPower => "クリティカル倍率",
            PrestigeBonusType.SPChargeSpeed => "SP回復速度",
            PrestigeBonusType.FeverPower => "フィーバー倍率",
            PrestigeBonusType.SellPriceBonus => "売却価格",
            PrestigeBonusType.GachaCostReduction => "ガチャコスト軽減",
            PrestigeBonusType.UpgradeCostReduction => "強化コスト軽減",
            PrestigeBonusType.DividendBonus => "配当金",
            _ => "不明"
        };
    }
}
