using UnityEngine;

/// <summary>
/// スロット倍率の確率テーブル
/// </summary>
public static class SlotRarityTable
{
    // ========================================
    // 確率閾値（累積）
    // ========================================

    /// <summary>x2-10 の上限確率（50%）</summary>
    public const float TIER_1_THRESHOLD = 0.50f;

    /// <summary>x11-50 の上限確率（80%）</summary>
    public const float TIER_2_THRESHOLD = 0.80f;

    /// <summary>x51-100 の上限確率（95%）</summary>
    public const float TIER_3_THRESHOLD = 0.95f;

    /// <summary>x101-999 の上限確率（99%）</summary>
    public const float TIER_4_THRESHOLD = 0.99f;

    // ========================================
    // 倍率範囲
    // ========================================

    /// <summary>Tier1 最小倍率</summary>
    public const int TIER_1_MIN = 2;
    /// <summary>Tier1 最大倍率（排他）</summary>
    public const int TIER_1_MAX = 11;

    /// <summary>Tier2 最小倍率</summary>
    public const int TIER_2_MIN = 11;
    /// <summary>Tier2 最大倍率（排他）</summary>
    public const int TIER_2_MAX = 51;

    /// <summary>Tier3 最小倍率</summary>
    public const int TIER_3_MIN = 51;
    /// <summary>Tier3 最大倍率（排他）</summary>
    public const int TIER_3_MAX = 101;

    /// <summary>Tier4 最小倍率</summary>
    public const int TIER_4_MIN = 101;
    /// <summary>Tier4 最大倍率（排他）</summary>
    public const int TIER_4_MAX = 1000;

    /// <summary>Tier5 最小倍率</summary>
    public const int TIER_5_MIN = 1000;
    /// <summary>Tier5 最大倍率（排他）</summary>
    public const int TIER_5_MAX = 10000;

    // ========================================
    // ヘルパーメソッド
    // ========================================

    /// <summary>
    /// 確率に基づいてスロット倍率を決定
    /// </summary>
    /// <returns>決定された倍率</returns>
    public static int Roll()
    {
        float roll = Random.value;

        if (roll < TIER_1_THRESHOLD)
            return Random.Range(TIER_1_MIN, TIER_1_MAX);
        else if (roll < TIER_2_THRESHOLD)
            return Random.Range(TIER_2_MIN, TIER_2_MAX);
        else if (roll < TIER_3_THRESHOLD)
            return Random.Range(TIER_3_MIN, TIER_3_MAX);
        else if (roll < TIER_4_THRESHOLD)
            return Random.Range(TIER_4_MIN, TIER_4_MAX);
        else
            return Random.Range(TIER_5_MIN, TIER_5_MAX);
    }

    /// <summary>
    /// 倍率のティアを取得
    /// </summary>
    public static int GetTier(int multiplier)
    {
        if (multiplier < TIER_1_MAX) return 1;
        if (multiplier < TIER_2_MAX) return 2;
        if (multiplier < TIER_3_MAX) return 3;
        if (multiplier < TIER_4_MAX) return 4;
        return 5;
    }

    /// <summary>
    /// ティアの表示名を取得
    /// </summary>
    public static string GetTierName(int tier)
    {
        return tier switch
        {
            1 => "Common",
            2 => "Uncommon",
            3 => "Rare",
            4 => "Epic",
            5 => "Legendary",
            _ => "Unknown"
        };
    }
}
