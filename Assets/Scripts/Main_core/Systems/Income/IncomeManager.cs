using System;
using UnityEngine;

/// <summary>
/// 自動収入（毎秒収入）の計算を担当するマネージャー
/// </summary>
public class IncomeManager : MonoBehaviour
{
    public static IncomeManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================

    [Header("収入設定")]
    [Tooltip("収入計算の間隔（秒）")]
    [SerializeField] private float tickInterval = 1.0f;

    // ========================================
    // 計算パラメータ（外部から設定）
    // ========================================

    [Header("計算パラメータ (Debug)")]
    [SerializeField] private double _baseIncome = 0;
    [SerializeField] private double _flatBonus = 0;
    [SerializeField] private double _percentBonus = 0;
    [SerializeField] private double _globalMultiplier = 1.0;

    // ========================================
    // 計算結果
    // ========================================

    [Header("計算結果 (ReadOnly)")]
    [SerializeField] private double _finalIncomePerTick = 0;

    /// <summary>1ティックあたりの収入（計算済み）</summary>
    public double FinalIncomePerTick => _finalIncomePerTick;

    /// <summary>1秒あたりの収入（DPS表示用）</summary>
    public double IncomePerSecond => _finalIncomePerTick / tickInterval;

    // ========================================
    // イベント
    // ========================================

    /// <summary>収入が発生した時 (amount)</summary>
    public event Action<double> OnIncomeGenerated;

    // ========================================
    // 内部状態
    // ========================================

    private bool _isRunning = false;

    // ========================================
    // 初期化
    // ========================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========================================
    // 開始/停止
    // ========================================

    /// <summary>
    /// 自動収入を開始
    /// </summary>
    public void StartIncome()
    {
        if (_isRunning) return;
        _isRunning = true;
        InvokeRepeating(nameof(IncomeTick), tickInterval, tickInterval);
    }

    /// <summary>
    /// 自動収入を停止
    /// </summary>
    public void StopIncome()
    {
        if (!_isRunning) return;
        _isRunning = false;
        CancelInvoke(nameof(IncomeTick));
    }

    /// <summary>
    /// 一時停止（ポーズ用）
    /// </summary>
    public void Pause()
    {
        StopIncome();
    }

    /// <summary>
    /// 再開
    /// </summary>
    public void Resume()
    {
        StartIncome();
    }

    // ========================================
    // ティック処理
    // ========================================

    private void IncomeTick()
    {
        if (_finalIncomePerTick <= 0) return;

        OnIncomeGenerated?.Invoke(_finalIncomePerTick);
    }

    // ========================================
    // パラメータ設定
    // ========================================

    /// <summary>
    /// 収入計算パラメータを設定
    /// </summary>
    public void SetParameters(double baseIncome, double flatBonus, double percentBonus, double globalMultiplier)
    {
        _baseIncome = baseIncome;
        _flatBonus = flatBonus;
        _percentBonus = percentBonus;
        _globalMultiplier = globalMultiplier;
        Recalculate();
    }

    /// <summary>
    /// フラットボーナスを加算
    /// </summary>
    public void AddFlatBonus(double amount)
    {
        _flatBonus += amount;
        Recalculate();
    }

    /// <summary>
    /// パーセントボーナスを加算
    /// </summary>
    public void AddPercentBonus(double amount)
    {
        _percentBonus += amount;
        Recalculate();
    }

    /// <summary>
    /// グローバル倍率を設定
    /// </summary>
    public void SetGlobalMultiplier(double multiplier)
    {
        _globalMultiplier = multiplier;
        Recalculate();
    }

    // ========================================
    // 計算
    // ========================================

    /// <summary>
    /// 最終収入を再計算
    /// </summary>
    public void Recalculate()
    {
        _finalIncomePerTick = CalculateIncome(_baseIncome, _flatBonus, _percentBonus, _globalMultiplier);
    }

    /// <summary>
    /// 収入計算（静的メソッド、プレビュー用）
    /// </summary>
    public static double CalculateIncome(double baseIncome, double flatBonus, double percentBonus, double globalMultiplier)
    {
        // (基礎 + フラット) × (1 + パーセント) × グローバル
        return (baseIncome + flatBonus) * (1.0 + percentBonus) * globalMultiplier;
    }

    // ========================================
    // オフライン収入計算
    // ========================================

    /// <summary>
    /// オフライン中に貯まった収入を計算
    /// </summary>
    /// <param name="offlineSeconds">オフラインだった秒数</param>
    /// <param name="offlineEfficiency">オフライン効率（0.0〜1.0）</param>
    /// <returns>獲得すべき金額</returns>
    public double CalculateOfflineEarnings(double offlineSeconds, double offlineEfficiency = 0.5)
    {
        double tickCount = offlineSeconds / tickInterval;
        return _finalIncomePerTick * tickCount * offlineEfficiency;
    }

    // ========================================
    // デバッグ
    // ========================================

    /// <summary>
    /// 現在の状態をログ出力
    /// </summary>
    public void DebugLog()
    {
        Debug.Log($"[Income] Base:{_baseIncome} + Flat:{_flatBonus} × (1+{_percentBonus:P0}) × Global:{_globalMultiplier} = {_finalIncomePerTick}/tick ({IncomePerSecond}/s)");
    }
}
