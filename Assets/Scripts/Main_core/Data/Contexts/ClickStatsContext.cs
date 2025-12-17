/// <summary>
/// クリック計算に必要なパラメータをまとめたContext（箱）
/// ClickManagerに渡して、計算結果を受け取る
/// </summary>
[System.Serializable]
public class ClickStatsContext
{
    // ========================================
    // 入力パラメータ（計算に使う値）
    // ========================================

    /// <summary>基礎クリック値（強化込みの最終値）</summary>
    public double BaseClickValue;

    /// <summary>クリティカル発生確率 (0.0 ~ 1.0)</summary>
    public float CriticalChance;

    /// <summary>クリティカル時の倍率</summary>
    public double CriticalMultiplier;

    /// <summary>フィーバー時の倍率</summary>
    public float FeverMultiplier;

    /// <summary>フィーバーモード中か</summary>
    public bool IsFeverActive;

    /// <summary>1クリックあたりのSP充填量</summary>
    public float SpChargeAmount;

    /// <summary>スロット発動確率 (0.0 ~ 1.0)</summary>
    public float SlotTriggerChance;

    // ========================================
    // コンストラクタ
    // ========================================

    public ClickStatsContext() { }


}

/// <summary>
/// クリック計算の結果を格納するクラス
/// ClickManagerから返される
/// </summary>
[System.Serializable]
public class ClickResult
{
    // ... (ここは変更なしでOK) ...
    /// <summary>最終的に獲得した金額</summary>
    public double EarnedAmount;

    /// <summary>クリティカルが発生したか</summary>
    public bool WasCritical;

    /// <summary>スロットが発動したか</summary>
    public bool TriggeredSlot;

    /// <summary>適用された倍率（デバッグ用）</summary>
    public double AppliedMultiplier;

    public ClickResult() { }

    public ClickResult(double earnedAmount, bool wasCritical, bool triggeredSlot, double appliedMultiplier)
    {
        EarnedAmount = earnedAmount;
        WasCritical = wasCritical;
        TriggeredSlot = triggeredSlot;
        AppliedMultiplier = appliedMultiplier;
    }
}