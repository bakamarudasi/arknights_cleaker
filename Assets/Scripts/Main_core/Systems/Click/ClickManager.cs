using UnityEngine;

/// <summary>
/// クリック時の収入計算を担当するマネージャー
/// 純粋な計算ロジックのみ。副作用（お金加算・統計更新等）は呼び出し元で行う
/// </summary>
public static class ClickManager
{
    // ========================================
    // メイン計算メソッド
    // ========================================

    /// <summary>
    /// クリック時の計算を実行し、結果を返す
    /// </summary>
    /// <param name="context">計算に必要なパラメータ</param>
    /// <returns>計算結果（獲得金額、クリティカル有無など）</returns>
    public static ClickResult Calculate(ClickStatsContext context)
    {
        // クリティカル判定
        bool isCritical = Random.value < context.CriticalChance;

        // スロット発動判定
        bool triggeredSlot = Random.value < context.SlotTriggerChance;

        // 倍率計算
        double multiplier = CalculateMultiplier(context, isCritical);

        // 最終金額
        double earnedAmount = context.BaseClickValue * multiplier;

        return new ClickResult(
            earnedAmount: earnedAmount,
            wasCritical: isCritical,
            triggeredSlot: triggeredSlot,
            appliedMultiplier: multiplier
        );
    }

    /// <summary>
    /// 倍率を計算（クリティカル）
    /// </summary>
    private static double CalculateMultiplier(ClickStatsContext context, bool isCritical)
    {
        // クリティカル倍率
        if (isCritical)
        {
            return context.CriticalMultiplier;
        }

        return 1.0;
    }

    // ========================================
    // ユーティリティメソッド
    // ========================================

    /// <summary>
    /// クリティカル判定のみ（プレビュー用など）
    /// </summary>
    public static bool RollCritical(float criticalChance)
    {
        return Random.value < criticalChance;
    }

    /// <summary>
    /// 期待値を計算（DPS表示などに使用）
    /// </summary>
    public static double CalculateExpectedValue(ClickStatsContext context)
    {
        // 通常時の期待値
        double normalValue = context.BaseClickValue * (1.0 - context.CriticalChance);

        // クリティカル時の期待値
        double criticalValue = context.BaseClickValue * context.CriticalMultiplier * context.CriticalChance;

        return normalValue + criticalValue;
    }

    /// <summary>
    /// 最大ダメージを計算（クリティカル時）
    /// </summary>
    public static double CalculateMaxDamage(ClickStatsContext context)
    {
        return context.BaseClickValue * context.CriticalMultiplier;
    }

    /// <summary>
    /// 最小ダメージを計算（通常時、フィーバーなし）
    /// </summary>
    public static double CalculateMinDamage(ClickStatsContext context)
    {
        return context.BaseClickValue;
    }
}