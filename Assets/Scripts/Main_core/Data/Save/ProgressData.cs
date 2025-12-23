using System;
using System.Collections.Generic;

/// <summary>
/// セーブ/ロード対応のプレイヤー進行データ。
/// JSON等でシリアライズ可能な構造。
/// </summary>
[Serializable]
public class ProgressData
{
    // --- 強化レベル ---
    public List<UpgradeProgress> upgrades = new();
    
    // --- アイテム所持数 ---
    public List<ItemStack> inventory = new();
    
    // --- 統計データ（実績用） ---
    public StatisticsData statistics = new();

    // --- イベント発動状態 ---
    public List<string> triggeredEventIds = new();

    // --- 解放済みメニュー ---
    public List<string> unlockedMenus = new();

    // ========================================
    // ヘルパーメソッド
    // ========================================
    
    public int GetUpgradeLevel(string id)
    {
        var found = upgrades.Find(x => x.id == id);
        return found?.level ?? 0;
    }
    
    public void SetUpgradeLevel(string id, int level)
    {
        var found = upgrades.Find(x => x.id == id);
        if (found != null)
        {
            found.level = level;
        }
        else
        {
            upgrades.Add(new UpgradeProgress { id = id, level = level });
        }
    }
    
    public int GetItemCount(string id)
    {
        var found = inventory.Find(x => x.id == id);
        return found?.count ?? 0;
    }
    
    public void SetItemCount(string id, int count)
    {
        var found = inventory.Find(x => x.id == id);
        if (found != null)
        {
            found.count = count;
        }
        else
        {
            inventory.Add(new ItemStack { id = id, count = count });
        }
    }
}

/// <summary>
/// 個別の強化進捗
/// </summary>
[Serializable]
public class UpgradeProgress
{
    public string id;
    public int level;
}

/// <summary>
/// アイテムスタック（所持数）
/// </summary>
[Serializable]
public class ItemStack
{
    public string id;
    public int count;
}

/// <summary>
/// 統計データ（実績・分析用）
/// </summary>
[Serializable]
public class StatisticsData
{
    // 購入系
    public int totalUpgradesPurchased;
    public double totalMoneySpent;
    public int totalMaterialsUsed;
    
    // クリック系
    public long totalClicks;
    public double totalMoneyEarned;
    public int totalCriticalHits;
    
    // プレイ時間
    public double totalPlayTimeSeconds;
    
    // 最高記録
    public double highestClickDamage;
    public double highestMoneyHeld;
    public int highestUpgradeLevel;
}
