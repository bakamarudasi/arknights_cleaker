using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全強化データを管理するデータベース
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/UpgradeDatabase")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("全強化データ")]
    public List<UpgradeData> allUpgrades = new();

    // ========================================
    // 取得メソッド
    // ========================================

    /// <summary>
    /// カテゴリ別に取得（UpgradeData内のcategoryを使う）
    /// </summary>
    public List<UpgradeData> GetByCategory(UpgradeData.UpgradeCategory category)
    {
        return allUpgrades.FindAll(d => d.category == category);
    }

    /// <summary>
    /// ソート順で取得
    /// </summary>
    public List<UpgradeData> GetSorted(UpgradeData.UpgradeCategory category)
    {
        var list = GetByCategory(category);
        list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        return list;
    }

    // ========================================
    // フィルター付き取得
    // ========================================

    /// <summary>
    /// ロック解除済みのみ
    /// </summary>
    public List<UpgradeData> GetUnlocked(UpgradeData.UpgradeCategory category)
    {
        var list = GetByCategory(category);
        return list.FindAll(d => 
            GameController.Instance.Upgrade.MeetsPrerequisite(d));
    }

    /// <summary>
    /// 購入可能なもののみ
    /// </summary>
    public List<UpgradeData> GetPurchasable(UpgradeData.UpgradeCategory category)
    {
        var list = GetByCategory(category);
        return list.FindAll(d => 
            GameController.Instance.CanPurchaseUpgrade(d));
    }

    /// <summary>
    /// MAX未到達のみ
    /// </summary>
    public List<UpgradeData> GetNotMaxed(UpgradeData.UpgradeCategory category)
    {
        var list = GetByCategory(category);
        return list.FindAll(d => 
            GameController.Instance.GetUpgradeState(d) != UpgradeState.MaxLevel);
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// IDで検索
    /// </summary>
    public UpgradeData FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return allUpgrades.Find(d => d.id == id);
    }

    /// <summary>
    /// カテゴリ内の購入可能数（バッジ用）
    /// </summary>
    public int GetPurchasableCount(UpgradeData.UpgradeCategory category)
    {
        return GetPurchasable(category).Count;
    }

    /// <summary>
    /// 全体の購入可能数
    /// </summary>
    public int GetTotalPurchasableCount()
    {
        return allUpgrades.FindAll(d => 
            GameController.Instance.CanPurchaseUpgrade(d)).Count;
    }
}