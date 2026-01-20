using System;
using UnityEngine;

/// <summary>
/// SP（スキルポイント）とフィーバーモードを管理
/// </summary>
public class SPManager : BaseSingleton<SPManager>
{
    protected override bool Persistent => false;

    // ========================================
    // 設定
    // ========================================

    [Header("SP設定")]
    [SerializeField] private float _maxSP = 100f;
    [SerializeField] private float _baseChargeAmount = 5f;

    [Header("フィーバー設定")]
    [SerializeField] private float _baseFeverMultiplier = 3f;
    [SerializeField] private float _feverDuration = 10f;

    // ========================================
    // 状態
    // ========================================

    [Header("現在の状態 (Debug)")]
    [SerializeField] private float _currentSP = 0f;
    [SerializeField] private bool _isFeverActive = false;

    // ========================================
    // ボーナス
    // ========================================

    private float _chargeBonus = 0f;
    private float _feverPowerBonus = 0f;

    // ========================================
    // プロパティ
    // ========================================

    public float CurrentSP => _currentSP;
    public float MaxSP => _maxSP;
    public bool IsFeverActive => _isFeverActive;
    public float FillRate => _currentSP / _maxSP;

    /// <summary>最終SP充填量</summary>
    public float FinalChargeAmount => _baseChargeAmount + _chargeBonus;

    /// <summary>最終フィーバー倍率</summary>
    public float FinalFeverMultiplier => _baseFeverMultiplier + _feverPowerBonus;

    /// <summary>フィーバー持続時間</summary>
    public float FeverDuration => _feverDuration;

    // フィーバー開始時刻
    private float _feverStartTime;

    /// <summary>フィーバー残り時間を取得</summary>
    public float GetFeverRemainingTime()
    {
        if (!_isFeverActive) return 0f;
        float elapsed = Time.time - _feverStartTime;
        return Mathf.Max(0f, _feverDuration - elapsed);
    }

    // ========================================
    // イベント
    // ========================================

    public event Action<float> OnSPChanged;
    public event Action OnFeverStarted;
    public event Action OnFeverEnded;

    // ========================================
    // SP操作
    // ========================================

    /// <summary>
    /// SPを加算（フィーバー中は加算しない）
    /// </summary>
    public void AddSP(float amount)
    {
        if (_isFeverActive) return;

        _currentSP = Mathf.Min(_currentSP + amount, _maxSP);
        OnSPChanged?.Invoke(_currentSP);

        if (_currentSP >= _maxSP)
        {
            StartFever();
        }
    }

    /// <summary>
    /// デフォルト量でSP加算
    /// </summary>
    public void ChargeSP()
    {
        AddSP(FinalChargeAmount);
    }

    // ========================================
    // フィーバー
    // ========================================

    private void StartFever()
    {
        if (_isFeverActive) return;

        _isFeverActive = true;
        _feverStartTime = Time.time;
        OnFeverStarted?.Invoke();
        Invoke(nameof(EndFever), _feverDuration);
    }

    private void EndFever()
    {
        // 既に終了済みなら何もしない（二重呼び出し防止）
        if (!_isFeverActive) return;

        _isFeverActive = false;
        _currentSP = 0;
        OnSPChanged?.Invoke(_currentSP);
        OnFeverEnded?.Invoke();
    }

    /// <summary>
    /// フィーバーを強制終了
    /// </summary>
    public void ForceEndFever()
    {
        CancelInvoke(nameof(EndFever));
        EndFever();
    }

    // ========================================
    // ボーナス設定
    // ========================================

    public void AddChargeBonus(float amount)
    {
        _chargeBonus += amount;
    }

    public void AddFeverPowerBonus(float amount)
    {
        _feverPowerBonus += amount;
    }

    public void SetBonuses(float chargeBonus, float feverPowerBonus)
    {
        _chargeBonus = chargeBonus;
        _feverPowerBonus = feverPowerBonus;
    }

    // ========================================
    // リセット
    // ========================================

    public void Reset()
    {
        ForceEndFever();
        _currentSP = 0;
        _chargeBonus = 0;
        _feverPowerBonus = 0;
        OnSPChanged?.Invoke(_currentSP);
    }
}
