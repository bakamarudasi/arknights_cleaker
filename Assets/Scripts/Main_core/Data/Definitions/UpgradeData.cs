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
        Income_PercentAdd,  // 自動収入%加算
        Critical_ChanceAdd, // クリティカル率
        Critical_PowerAdd,  // クリティカル倍率
        SP_ChargeAdd,       // SPチャージ速度
        Fever_PowerAdd      // フィーバー倍率
    }

    // ========================================
    // カテゴリ（フィルター・UI表示用）
    // ========================================
    public enum UpgradeCategory
    {
        Click,      // クリック系
        Income,     // 自動収入系
        Critical,   // クリティカル系
        Skill,      // SP・フィーバー系
        Special     // 特殊・その他
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
    // コスト設定（素材）
    // ========================================
    [Header("コスト設定 (素材)")]
    [Tooltip("必要素材リスト（全レベル共通）")]
    public List<ItemCost> requiredMaterials;

    [Tooltip("レベルごとに素材数が増加する倍率（1.0 = 増加なし）")]
    public float materialScaling = 1.0f;

    // ========================================
    // 解放条件
    // ========================================
    [Header("解放条件")]
    [Tooltip("このアイテムを持っていれば解放（null = 条件なし）")]
    public ItemData requiredUnlockItem;

    [Tooltip("この強化が必要レベルに達していれば解放（null = 条件なし）")]
    public UpgradeData prerequisiteUpgrade;

    [Tooltip("前提強化の必要レベル")]
    public int prerequisiteLevel = 1;

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

    [Tooltip("カテゴリアイコン（絵文字: ⚔️=Click, 💰=Income, ⚡=Critical, 🎯=Skill, ⭐=Special）")]
    public string categoryIcon = "⚔️";

    [Tooltip("特別なアップグレードとしてマーク（STARバッジ表示）")]
    public bool isSpecial = false;

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
    /// 指定レベルでの素材必要数を計算
    /// </summary>
    public int GetMaterialAmountAtLevel(int baseAmount, int currentLevel)
    {
        if (materialScaling <= 1.0f) return baseAmount;
        return Mathf.CeilToInt(baseAmount * Mathf.Pow(materialScaling, currentLevel));
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
            UpgradeCategory.Critical => "クリティカル",
            UpgradeCategory.Skill => "スキル",
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
            UpgradeCategory.Critical => new Color(1.0f, 0.3f, 0.3f), // 赤
            UpgradeCategory.Skill => new Color(0.4f, 0.6f, 1.0f),    // 青
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