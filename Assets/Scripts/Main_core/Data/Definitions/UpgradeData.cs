using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New_Upgrade", menuName = "ArknightsClicker/Upgrade Data")]
public class UpgradeData : BaseData
{
    // ========================================
    // 強化タイプ
    // ========================================
    public enum UpgradeType
    {
        Click_FlatAdd,      // クリック固定値加算
        Click_PercentAdd,   // クリック%加算
        Income_FlatAdd,     // 自動収入固定値
        Income_PercentAdd   // 自動収入%加算
    }

    // ========================================
    // カテゴリ（フィルター・UI表示用）
    // ========================================
    public enum UpgradeCategory
    {
        Click,      // クリック系
        Income,     // 自動収入系
        Special     // 特殊・その他（オペレーター用）
    }

    // ========================================
    // 通貨タイプ
    // ========================================
    public enum CurrencyType
    {
        LMD,          // 龍門幣
        Certificate,  // 資格証
        Originium     // 純正源石（将来用）
    }

    // ========================================
    // 基本強化設定
    // ========================================
    [Header("強化設定")]
    public UpgradeType upgradeType;
    public UpgradeCategory category;

    [Tooltip("1レベルあたりの効果値")]
    public double effectValue = 1;

    [Tooltip("最大レベル（0 = 無制限）")]
    public int maxLevel = 10;

    
    // ========================================
    // コスト設定（通貨）
    // ========================================
    [Header("コスト設定 (通貨)")]
    [Tooltip("支払いに使う通貨の種類")]
    public CurrencyType currencyType = CurrencyType.LMD;

    [Tooltip("レベル1購入時の費用")]
    public double baseCost = 100;

    [Tooltip("レベルごとのコスト上昇率")]
    public float costMultiplier = 1.15f;

    // ========================================
    // 解放条件
    // ========================================
    [Header("解放条件")]
    [Tooltip("このアイテムを持っていれば解放（null = 条件なし）")]
    public ItemData requiredUnlockItem;

    // ========================================
    // 株式連動設定
    // ========================================
    [Header("株式連動設定")]
    [Tooltip("関連する企業の株（保有率でボーナスが適用される）")]
    public StockData relatedStock;

    [Tooltip("株式保有率による効果倍率（true = 保有率に応じてeffectValueが増加）")]
    public bool scaleWithHolding = false;

    [Tooltip("保有率による最大倍率（2.0 = 100%保有時に効果2倍）")]
    [Range(1f, 5f)]
    public float maxHoldingMultiplier = 2.0f;

    // ========================================
    // 表示設定（UI用）
    // ========================================
    [Header("表示設定")]
    [Tooltip("ショップでの並び順（小さい方が上）")]
    public int sortOrder = 0;

    [Tooltip("効果の表示フォーマット（例: 'クリック +{0}'）")]
    public string effectFormat = "+{0}";

    [Tooltip("パーセント表示するか")]
    public bool isPercentDisplay = false;

    // ========================================
    // 計算ヘルパー
    // ========================================

    /// <summary>
    /// 指定レベルでの購入コストを計算
    /// </summary>
    public double GetCostAtLevel(int currentLevel)
    {
        return baseCost * System.Math.Pow(costMultiplier, currentLevel);
    }

    /// <summary>
    /// 指定レベルでの累計効果を計算
    /// </summary>
    public double GetTotalEffectAtLevel(int level)
    {
        return effectValue * level;
    }

    /// <summary>
    /// 効果値を表示用文字列に変換
    /// </summary>
    public string GetEffectDisplayString(int level)
    {
        double totalEffect = GetTotalEffectAtLevel(level);
        string valueStr = isPercentDisplay
            ? $"{totalEffect * 100:F1}%"
            : $"{totalEffect:F1}";
        return string.Format(effectFormat, valueStr);
    }

    /// <summary>
    /// 最大レベルに達しているか
    /// </summary>
    public bool IsMaxLevel(int currentLevel)
    {
        return maxLevel > 0 && currentLevel >= maxLevel;
    }

    // ========================================
    // カテゴリ表示用
    // ========================================
    public string GetCategoryDisplayName()
    {
        return category switch
        {
            UpgradeCategory.Click => "クリック",
            UpgradeCategory.Income => "自動収入",
            UpgradeCategory.Special => "特殊",
            _ => "その他"
        };
    }

    public Color GetCategoryColor()
    {
        return category switch
        {
            UpgradeCategory.Click => new Color(1.0f, 0.6f, 0.2f),    // オレンジ
            UpgradeCategory.Income => new Color(0.2f, 0.8f, 0.4f),   // 緑
            UpgradeCategory.Special => new Color(0.8f, 0.5f, 1.0f),  // 紫
            _ => Color.white
        };
    }
}

/// <summary>
/// 素材コスト定義
/// </summary>
[System.Serializable]
public class ItemCost
{
    public ItemData item;
    public int amount = 1;
}