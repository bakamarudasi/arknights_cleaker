using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ガチャバナーの設定データ（ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "GachaBanner", menuName = "Game/Gacha/Banner Data")]
public class GachaBannerData : ScriptableObject
{
    // ========================================
    // 基本情報
    // ========================================

    [Header("基本情報")]
    [Tooltip("バナーの一意識別子")]
    public string bannerId;

    [Tooltip("バナー表示名")]
    public string bannerName;

    [TextArea(2, 4)]
    [Tooltip("バナーの説明文")]
    public string description;

    [Tooltip("バナー画像")]
    public Sprite bannerSprite;

    [Tooltip("限定バナーフラグ")]
    public bool isLimited;

    // ========================================
    // コスト設定
    // ========================================

    [Header("コスト設定")]
    [Tooltip("使用する通貨タイプ")]
    public CurrencyType currencyType = CurrencyType.Certificate;

    [Tooltip("単発ガチャのコスト")]
    public double costSingle = 600;

    [Tooltip("10連ガチャのコスト")]
    public double costTen = 6000;

    // ========================================
    // 天井システム
    // ========================================

    [Header("天井システム")]
    [Tooltip("天井システムを有効にする")]
    public bool hasPity;

    [Tooltip("天井到達回数（確定入手）")]
    public int pityCount = 50;

    [Tooltip("ソフト天井開始（確率上昇開始）")]
    public int softPityStart = 40;

    // ========================================
    // 排出テーブル
    // ========================================

    [Header("排出テーブル")]
    [Tooltip("排出アイテムリスト")]
    public List<GachaPoolEntry> pool = new();

    // ========================================
    // ピックアップ
    // ========================================

    [Header("ピックアップ")]
    [Tooltip("ピックアップ対象アイテム")]
    public List<ItemData> pickupItems = new();

    [Range(0f, 1f)]
    [Tooltip("ピックアップ時の確率倍率（0.5 = 50%UP）")]
    public float pickupRateBoost = 0.5f;

    // ========================================
    // 解放条件
    // ========================================

    [Header("解放条件")]
    [Tooltip("初期状態でロックされているか")]
    public bool startsLocked = false;

    [Tooltip("このバナーのプールを全取得で解放（初心者バナー等）")]
    public GachaBannerData prerequisiteBanner;

    [Tooltip("このアイテムを持っていれば解放")]
    public ItemData requiredUnlockItem;

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// 指定回数分のコストを計算
    /// </summary>
    public double GetCost(int pullCount)
    {
        if (pullCount == 10) return costTen;
        return costSingle * pullCount;
    }

    /// <summary>
    /// 排出テーブルの総重みを取得
    /// </summary>
    public float GetTotalWeight()
    {
        float total = 0f;
        foreach (var entry in pool)
        {
            // weightが0以下の場合はデフォルト値1を使用（設定ミス対策）
            total += entry.weight > 0 ? entry.weight : 1f;
        }
        return total;
    }

    /// <summary>
    /// 封入数制限があるバナーかチェック
    /// </summary>
    public bool HasLimitedStock()
    {
        foreach (var entry in pool)
        {
            if (entry.HasStockLimit) return true;
        }
        return false;
    }

    /// <summary>
    /// 総封入数を取得（制限があるアイテムのみ）
    /// </summary>
    public int GetTotalStock()
    {
        int total = 0;
        foreach (var entry in pool)
        {
            if (entry.HasStockLimit)
                total += entry.stockCount;
        }
        return total;
    }
}

// ========================================
// 排出テーブルエントリ
// ========================================

[Serializable]
public class GachaPoolEntry
{
    [Tooltip("排出アイテム")]
    public ItemData item;

    [Tooltip("排出重み（確率計算用）")]
    [Range(0.01f, 100f)]
    public float weight = 1f;

    [Tooltip("このアイテムはピックアップ対象か")]
    public bool isPickup;

    [Tooltip("封入数（0 = 無制限）")]
    public int stockCount = 0;

    /// <summary>
    /// レアリティ（ItemDataから取得、1〜6）
    /// </summary>
    public int Rarity => item != null ? (int)item.rarity + 1 : 3;

    /// <summary>
    /// 封入数制限があるか
    /// </summary>
    public bool HasStockLimit => stockCount > 0;
}

// ========================================
// ガチャ結果データ
// ========================================

[Serializable]
public class GachaResultItem
{
    public string itemId;
    public string itemName;
    public int rarity;
    public Sprite icon;
    public bool isNew;
    public bool isPickup;

    public GachaResultItem() { }

    public GachaResultItem(ItemData item, bool isNew = false, bool isPickup = false)
    {
        if (item != null)
        {
            itemId = item.id;
            itemName = item.displayName;
            icon = item.icon;
            rarity = (int)item.rarity + 1;  // Star1=1 〜 Star6=6
        }
        this.isNew = isNew;
        this.isPickup = isPickup;
    }
}