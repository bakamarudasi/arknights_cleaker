using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// ホーム画面（メインクリック画面）のUIコントローラー
/// 各ハンドラーを統括し、通貨・統計表示を管理
/// </summary>
public class HomeUIController : IViewController
{
    private VisualElement _root;

    // ========================================
    // ハンドラー
    // ========================================

    private ClickAreaHandler _clickHandler;
    private FeverEffectHandler _feverHandler;
    private SlotEffectHandler _slotHandler;

    // ========================================
    // UI要素参照
    // ========================================

    // レイヤー
    private VisualElement _particleLayer;
    private VisualElement _effectLayer;

    // 上部UI
    private Label _moneyLabel;
    private Label _incomeRateLabel;
    private Label _dpsValueLabel;
    private Label _clicksValueLabel;
    private Label _critValueLabel;

    // 中央
    private Label _powerValueLabel;
    private Label _multiplierLabel;

    // ========================================
    // パーティクルシステム
    // ========================================

    private readonly List<VisualElement> _particles = new();
    private const int MaxParticles = 30;
    private float _particleSpawnTimer;

    // ========================================
    // 好感度コールバック
    // ========================================

    private Action<string, int, int> _onAffectionChangedCallback;

    // ========================================
    // コールバック参照（解除用）
    // ========================================

    private Action<double> _onMoneyChangedCallback;
    private IVisualElementScheduledItem _updateSchedule;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement contentArea)
    {
        _root = contentArea;

        QueryElements();
        InitializeHandlers();
        BindGameEvents();
        SetupUpdateLoop();
        UpdateUI();

        LogUIController.LogSystem("Home View Initialized.");
    }

    private void QueryElements()
    {
        // レイヤー
        _particleLayer = _root.Q<VisualElement>("particle-layer");
        _effectLayer = _root.Q<VisualElement>("effect-layer");

        // 上部UI
        _moneyLabel = _root.Q<Label>("money-label");
        _incomeRateLabel = _root.Q<Label>("income-rate");
        _dpsValueLabel = _root.Q<Label>("dps-value");
        _clicksValueLabel = _root.Q<Label>("clicks-value");
        _critValueLabel = _root.Q<Label>("crit-value");

        // 中央
        _powerValueLabel = _root.Q<Label>("power-value");
        _multiplierLabel = _root.Q<Label>("multiplier-label");
    }

    private void InitializeHandlers()
    {
        // クリックハンドラー
        _clickHandler = new ClickAreaHandler(_root, _effectLayer);
        _clickHandler.Initialize();
        _clickHandler.OnClicked += OnClickHandled;

        // フィーバーハンドラー
        _feverHandler = new FeverEffectHandler(_root);
        _feverHandler.Initialize();

        // スロットハンドラー
        _slotHandler = new SlotEffectHandler(_root);
        _slotHandler.Initialize();
    }

    private void BindGameEvents()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        _onMoneyChangedCallback = _ => UpdateMoneyDisplay();

        if (gc.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged += _onMoneyChangedCallback;
        }

        // 好感度イベント
        if (AffectionManager.Instance != null)
        {
            _onAffectionChangedCallback = OnAffectionChanged;
            AffectionManager.Instance.OnAffectionChanged += _onAffectionChangedCallback;
        }
    }

    private void SetupUpdateLoop()
    {
        _updateSchedule = _root.schedule.Execute(() =>
        {
            // ハンドラーの更新
            _clickHandler?.UpdateComboDecay();
            _clickHandler?.CleanupDamageNumbers();
            _clickHandler?.UpdateDPS();
            _feverHandler?.UpdateFeverTimer();

            // パーティクル更新
            UpdateParticles();

            // 統計表示更新
            UpdateStatsDisplay();
        }).Every(50); // 20fps更新
    }

    // ========================================
    // クリックハンドラーからのコールバック
    // ========================================

    private void OnClickHandled()
    {
        // 必要に応じて追加処理
    }

    // ========================================
    // パーティクルエフェクト
    // ========================================

    private void UpdateParticles()
    {
        _particleSpawnTimer += 0.05f;

        // 新規パーティクル生成
        if (_particleSpawnTimer > 0.3f && _particles.Count < MaxParticles)
        {
            SpawnParticle();
            _particleSpawnTimer = 0f;
        }

        // パーティクル更新
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            var currentTop = particle.resolvedStyle.top;

            if (currentTop < -20)
            {
                _particleLayer?.Remove(particle);
                _particles.RemoveAt(i);
            }
            else
            {
                particle.style.top = currentTop - 1f;
            }
        }
    }

    private void SpawnParticle()
    {
        if (_particleLayer == null) return;

        var particle = new VisualElement();
        particle.AddToClassList("particle");

        if (UnityEngine.Random.value > 0.7f)
        {
            particle.AddToClassList("large");
        }

        float x = UnityEngine.Random.Range(0f, _particleLayer.resolvedStyle.width);
        float y = _particleLayer.resolvedStyle.height + 10;

        particle.style.left = x;
        particle.style.top = y;
        particle.style.opacity = UnityEngine.Random.Range(0.3f, 0.8f);

        _particleLayer.Add(particle);
        _particles.Add(particle);
    }

    // ========================================
    // UI更新
    // ========================================

    private void UpdateUI()
    {
        UpdateMoneyDisplay();
        UpdatePowerDisplay();
        UpdateStatsDisplay();
    }

    private void UpdateMoneyDisplay()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        double money = gc.Wallet.Money;
        if (_moneyLabel != null)
        {
            _moneyLabel.text = FormatNumber(money);
        }

        // 収入レート計算
        if (_incomeRateLabel != null)
        {
            double incomePerSecond = gc.BaseIncomePerSecond;
            _incomeRateLabel.text = $"+{FormatNumber(incomePerSecond)}/s";
        }
    }

    private void UpdatePowerDisplay()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        if (_powerValueLabel != null)
        {
            double power = gc.FinalClickPower;
            _powerValueLabel.text = FormatNumber(power);
        }

        // 倍率表示
        if (_multiplierLabel != null)
        {
            double mult = gc.GlobalMultiplier;
            if (mult > 1.0)
            {
                _multiplierLabel.text = $"x{mult:F1}";
                _multiplierLabel.AddToClassList("active");
                _powerValueLabel?.AddToClassList("boosted");
            }
            else
            {
                _multiplierLabel.RemoveFromClassList("active");
                _powerValueLabel?.RemoveFromClassList("boosted");
            }
        }
    }

    private void UpdateStatsDisplay()
    {
        if (_clickHandler == null) return;

        if (_clicksValueLabel != null)
        {
            _clicksValueLabel.text = _clickHandler.TotalClicks.ToString("N0");
        }

        if (_critValueLabel != null)
        {
            float critRate = _clickHandler.TotalClicks > 0
                ? (float)_clickHandler.CriticalHits / _clickHandler.TotalClicks * 100f
                : 0f;
            _critValueLabel.text = $"{critRate:F1}%";
        }

        if (_dpsValueLabel != null)
        {
            _dpsValueLabel.text = FormatNumber(_clickHandler.CurrentDPS);
        }
    }

    // ========================================
    // 好感度イベント
    // ========================================

    private void OnAffectionChanged(string characterId, int newValue, int delta)
    {
        if (delta > 0 && _effectLayer != null)
        {
            // ハートエフェクト表示
            var heart = new Label();
            heart.text = "+";
            heart.AddToClassList("floating-text");
            heart.AddToClassList("affection");

            float x = UnityEngine.Random.Range(100f, 300f);
            heart.style.left = x;
            heart.style.top = 300;

            _effectLayer.Add(heart);

            _root.schedule.Execute(() => heart.AddToClassList("fade-out")).ExecuteLater(50);
            _root.schedule.Execute(() => _effectLayer.Remove(heart)).ExecuteLater(1100);
        }
    }

    // ========================================
    // ユーティリティ
    // ========================================

    private string FormatNumber(double value)
    {
        if (value >= 1_000_000_000_000) return $"{value / 1_000_000_000_000:F2}T";
        if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F2}B";
        if (value >= 1_000_000) return $"{value / 1_000_000:F2}M";
        if (value >= 1_000) return $"{value / 1_000:F2}K";
        return value.ToString("N0");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        // スケジュール停止
        _updateSchedule?.Pause();

        // ハンドラーの破棄
        _clickHandler?.Dispose();
        _feverHandler?.Dispose();
        _slotHandler?.Dispose();

        // GameControllerイベント解除
        var gc = GameController.Instance;
        if (gc?.Wallet != null && _onMoneyChangedCallback != null)
        {
            gc.Wallet.OnMoneyChanged -= _onMoneyChangedCallback;
        }

        // 好感度イベント解除
        if (AffectionManager.Instance != null && _onAffectionChangedCallback != null)
        {
            AffectionManager.Instance.OnAffectionChanged -= _onAffectionChangedCallback;
        }

        // パーティクルクリア
        foreach (var particle in _particles)
        {
            _particleLayer?.Remove(particle);
        }
        _particles.Clear();

        // 参照クリア
        _onMoneyChangedCallback = null;
        _onAffectionChangedCallback = null;
        _clickHandler = null;
        _feverHandler = null;
        _slotHandler = null;

        LogUIController.LogSystem("Home View Disposed.");
    }
}
