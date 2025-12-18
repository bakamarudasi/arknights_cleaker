using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// Fever演出を担当するハンドラー
/// Feverオーバーレイ、開始/終了アニメーション、タイマー表示を管理
/// </summary>
public class FeverEffectHandler : IDisposable
{
    // ========================================
    // UI要素参照
    // ========================================

    private readonly VisualElement _root;

    // Feverオーバーレイ
    private VisualElement _feverOverlay;
    private VisualElement _feverFlash;
    private Label _feverBigText;

    // SP関連
    private VisualElement _spGaugeFill;
    private VisualElement _spGaugeGlow;
    private VisualElement _spIconGlow;
    private Label _spText;
    private VisualElement _feverIndicator;
    private Label _feverTimerLabel;

    // ========================================
    // コールバック参照
    // ========================================

    private Action _onFeverStartedCallback;
    private Action _onFeverEndedCallback;
    private Action<float> _onSPChangedCallback;

    // ========================================
    // コンストラクタ
    // ========================================

    public FeverEffectHandler(VisualElement root)
    {
        _root = root;
    }

    // ========================================
    // 初期化
    // ========================================

    public void Initialize()
    {
        QueryElements();
        BindEvents();
        UpdateSPDisplay();

        LogUIController.LogSystem("FeverEffectHandler Initialized.");
    }

    private void QueryElements()
    {
        // Feverオーバーレイ
        _feverOverlay = _root.Q<VisualElement>("fever-overlay");
        _feverFlash = _root.Q<VisualElement>("fever-flash");
        _feverBigText = _root.Q<Label>("fever-big-text");

        // SP関連
        _spGaugeFill = _root.Q<VisualElement>("sp-gauge-fill");
        _spGaugeGlow = _root.Q<VisualElement>("sp-gauge-glow");
        _spIconGlow = _root.Q<VisualElement>("sp-icon-glow");
        _spText = _root.Q<Label>("sp-text");
        _feverIndicator = _root.Q<VisualElement>("fever-indicator");
        _feverTimerLabel = _root.Q<Label>("fever-timer");
    }

    private void BindEvents()
    {
        var gc = GameController.Instance;
        if (gc?.SP == null) return;

        _onSPChangedCallback = _ => UpdateSPDisplay();
        _onFeverStartedCallback = OnFeverStarted;
        _onFeverEndedCallback = OnFeverEnded;

        gc.SP.OnSPChanged += _onSPChangedCallback;
        gc.SP.OnFeverStarted += _onFeverStartedCallback;
        gc.SP.OnFeverEnded += _onFeverEndedCallback;
    }

    // ========================================
    // SP表示更新
    // ========================================

    public void UpdateSPDisplay()
    {
        var sp = GameController.Instance?.SP;
        if (sp == null) return;

        // ゲージ更新
        if (_spGaugeFill != null)
        {
            float fillPercent = sp.FillRate * 100f;
            _spGaugeFill.style.width = new Length(fillPercent, LengthUnit.Percent);

            _spGaugeFill.RemoveFromClassList("fever");
            _spGaugeFill.RemoveFromClassList("ready");

            if (sp.IsFeverActive)
            {
                _spGaugeFill.AddToClassList("fever");
            }
            else if (sp.FillRate >= 1f)
            {
                _spGaugeFill.AddToClassList("ready");
            }
        }

        // テキスト更新
        if (_spText != null)
        {
            _spText.text = $"{sp.CurrentSP:F0} / {sp.MaxSP:F0}";
        }

        // アイコングロー
        if (_spIconGlow != null)
        {
            _spIconGlow.RemoveFromClassList("ready");
            _spIconGlow.RemoveFromClassList("fever");

            if (sp.IsFeverActive)
            {
                _spIconGlow.AddToClassList("fever");
            }
            else if (sp.FillRate >= 1f)
            {
                _spIconGlow.AddToClassList("ready");
            }
        }
    }

    // ========================================
    // Feverタイマー更新
    // ========================================

    /// <summary>Feverタイマーを更新（毎フレーム呼び出し）</summary>
    public void UpdateFeverTimer()
    {
        var sp = GameController.Instance?.SP;
        if (sp == null || !sp.IsFeverActive) return;

        if (_feverTimerLabel != null)
        {
            float remaining = sp.GetFeverRemainingTime();
            _feverTimerLabel.text = $"{remaining:F1}";
        }
    }

    // ========================================
    // Fever開始演出
    // ========================================

    private void OnFeverStarted()
    {
        // フィーバーインジケーター
        _feverIndicator?.AddToClassList("active");

        // オーバーレイ
        _feverOverlay?.AddToClassList("active");

        // ビッグテキストアニメーション
        if (_feverBigText != null)
        {
            _feverBigText.AddToClassList("show");
            _root.schedule.Execute(() => _feverBigText.RemoveFromClassList("show")).ExecuteLater(2000);
        }

        // フラッシュエフェクト
        if (_feverFlash != null)
        {
            _feverFlash.AddToClassList("pulse");
            _root.schedule.Execute(() => _feverFlash.RemoveFromClassList("pulse")).ExecuteLater(150);
        }

        // SP表示更新
        _spGaugeFill?.AddToClassList("fever");
        _spIconGlow?.AddToClassList("fever");

        LogUIController.Msg("<color=#FF5050><b>FEVER MODE!!</b></color>");
    }

    // ========================================
    // Fever終了演出
    // ========================================

    private void OnFeverEnded()
    {
        _feverIndicator?.RemoveFromClassList("active");
        _feverOverlay?.RemoveFromClassList("active");
        _spGaugeFill?.RemoveFromClassList("fever");
        _spIconGlow?.RemoveFromClassList("fever");

        LogUIController.Msg("Fever mode ended.");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        var gc = GameController.Instance;
        if (gc?.SP != null)
        {
            if (_onSPChangedCallback != null)
                gc.SP.OnSPChanged -= _onSPChangedCallback;
            if (_onFeverStartedCallback != null)
                gc.SP.OnFeverStarted -= _onFeverStartedCallback;
            if (_onFeverEndedCallback != null)
                gc.SP.OnFeverEnded -= _onFeverEndedCallback;
        }

        _onSPChangedCallback = null;
        _onFeverStartedCallback = null;
        _onFeverEndedCallback = null;

        LogUIController.LogSystem("FeverEffectHandler Disposed.");
    }
}
