using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// ホーム画面（メインクリック画面）のUIコントローラー
/// キャラクターをクリックしてお金を稼ぐメイン画面 - Enhanced Edition
/// </summary>
public class HomeUIController : IViewController
{
    private VisualElement root;

    // ========================================
    // UI要素参照
    // ========================================

    // レイヤー
    private VisualElement particleLayer;
    private VisualElement effectLayer;
    private VisualElement feverOverlay;

    // クリック関連
    private VisualElement clickTarget;
    private VisualElement clickRipple;
    private VisualElement characterGlow;

    // 上部UI
    private Label moneyLabel;
    private Label incomeRateLabel;
    private VisualElement comboContainer;
    private Label comboCountLabel;
    private Label dpsValueLabel;
    private Label clicksValueLabel;
    private Label critValueLabel;

    // 中央
    private Label powerValueLabel;
    private Label multiplierLabel;

    // 下部（SP関連）
    private VisualElement spGaugeFill;
    private VisualElement spGaugeGlow;
    private VisualElement spIconGlow;
    private Label spText;
    private VisualElement feverIndicator;
    private Label feverTimerLabel;

    // Feverオーバーレイ
    private VisualElement feverFlash;
    private Label feverBigText;

    // スロットオーバーレイ
    private VisualElement slotOverlay;
    private VisualElement slotFlash;
    private Label slotBigText;
    private Label slotBonusText;

    // ========================================
    // 状態管理
    // ========================================

    // コンボシステム
    private int _comboCount;
    private float _lastClickTime;
    private const float ComboTimeout = 1.5f;
    private const int HighComboThreshold = 50;

    // 統計
    private int _totalClicks;
    private int _criticalHits;
    private float _dpsWindowStart;
    private double _dpsWindowDamage;
    private float _currentDPS;

    // パーティクル
    private readonly List<VisualElement> _particles = new();
    private const int MaxParticles = 30;
    private float _particleSpawnTimer;

    // ダメージ数字プール
    private readonly Queue<Label> _damageNumberPool = new();
    private readonly List<Label> _activeDamageNumbers = new();
    private const int DamageNumberPoolSize = 20;

    // ========================================
    // コールバック参照（解除用）
    // ========================================

    private EventCallback<ClickEvent> onClickCallback;
    private Action<double> onMoneyChangedCallback;
    private Action<float> onSPChangedCallback;
    private Action onFeverStartedCallback;
    private Action onFeverEndedCallback;
    private Action<string, int, int> onAffectionChangedCallback;
    private UnityEngine.Events.UnityAction onSlotTriggeredCallback;

    // 更新用
    private IVisualElementScheduledItem _updateSchedule;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        QueryElements();
        InitializeDamageNumberPool();
        SetupCallbacks();
        BindGameEvents();
        SetupUpdateLoop();
        UpdateUI();

        LogUIController.LogSystem("Home View Initialized (Enhanced).");
    }

    private void QueryElements()
    {
        // レイヤー
        particleLayer = root.Q<VisualElement>("particle-layer");
        effectLayer = root.Q<VisualElement>("effect-layer");
        feverOverlay = root.Q<VisualElement>("fever-overlay");

        // クリック関連
        clickTarget = root.Q<VisualElement>("click-target");
        clickRipple = root.Q<VisualElement>("click-ripple");
        characterGlow = root.Q<VisualElement>("character-glow");

        // 上部UI
        moneyLabel = root.Q<Label>("money-label");
        incomeRateLabel = root.Q<Label>("income-rate");
        comboContainer = root.Q<VisualElement>("combo-container");
        comboCountLabel = root.Q<Label>("combo-count");
        dpsValueLabel = root.Q<Label>("dps-value");
        clicksValueLabel = root.Q<Label>("clicks-value");
        critValueLabel = root.Q<Label>("crit-value");

        // 中央
        powerValueLabel = root.Q<Label>("power-value");
        multiplierLabel = root.Q<Label>("multiplier-label");

        // 下部（SP関連）
        spGaugeFill = root.Q<VisualElement>("sp-gauge-fill");
        spGaugeGlow = root.Q<VisualElement>("sp-gauge-glow");
        spIconGlow = root.Q<VisualElement>("sp-icon-glow");
        spText = root.Q<Label>("sp-text");
        feverIndicator = root.Q<VisualElement>("fever-indicator");
        feverTimerLabel = root.Q<Label>("fever-timer");

        // Feverオーバーレイ
        feverFlash = root.Q<VisualElement>("fever-flash");
        feverBigText = root.Q<Label>("fever-big-text");

        // スロットオーバーレイ
        slotOverlay = root.Q<VisualElement>("slot-overlay");
        slotFlash = root.Q<VisualElement>("slot-flash");
        slotBigText = root.Q<Label>("slot-big-text");
        slotBonusText = root.Q<Label>("slot-bonus-text");
    }

    private void InitializeDamageNumberPool()
    {
        for (int i = 0; i < DamageNumberPoolSize; i++)
        {
            var label = new Label();
            label.AddToClassList("damage-number");
            label.style.display = DisplayStyle.None;
            effectLayer?.Add(label);
            _damageNumberPool.Enqueue(label);
        }
    }

    private void SetupCallbacks()
    {
        onClickCallback = OnClickTarget;
        clickTarget?.RegisterCallback(onClickCallback);
    }

    private void BindGameEvents()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        onMoneyChangedCallback = _ => UpdateMoneyDisplay();
        onSPChangedCallback = _ => UpdateSPDisplay();
        onFeverStartedCallback = OnFeverStarted;
        onFeverEndedCallback = OnFeverEnded;

        if (gc.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged += onMoneyChangedCallback;
        }

        if (gc.SP != null)
        {
            gc.SP.OnSPChanged += onSPChangedCallback;
            gc.SP.OnFeverStarted += onFeverStartedCallback;
            gc.SP.OnFeverEnded += onFeverEndedCallback;
        }

        // 好感度イベント
        if (AffectionManager.Instance != null)
        {
            onAffectionChangedCallback = OnAffectionChanged;
            AffectionManager.Instance.OnAffectionChanged += onAffectionChangedCallback;
        }

        // スロットイベント
        if (gc.OnSlotTriggered != null)
        {
            onSlotTriggeredCallback = OnSlotTriggered;
            gc.OnSlotTriggered.AddListener(onSlotTriggeredCallback);
        }
    }

    private void SetupUpdateLoop()
    {
        _dpsWindowStart = Time.time;

        _updateSchedule = root.schedule.Execute(() =>
        {
            UpdateComboDecay();
            UpdateParticles();
            UpdateDPS();
            UpdateFeverTimer();
            CleanupDamageNumbers();
        }).Every(50); // 20fps更新
    }

    // ========================================
    // クリック処理
    // ========================================

    private void OnClickTarget(ClickEvent evt)
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        // クリック実行
        gc.ClickMainButton();

        // 統計更新
        _totalClicks++;
        _lastClickTime = Time.time;

        // コンボ更新
        UpdateCombo();

        // ダメージ計算とエフェクト
        ProcessClickDamage(evt.localPosition);

        // 視覚エフェクト
        PlayClickEffects();

        // 好感度処理
        AffectionManager.Instance?.OnCharacterClicked();
    }

    private void ProcessClickDamage(Vector2 clickPos)
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        // 基本ダメージ取得
        double baseDamage = gc.GetClickPower();

        // クリティカル判定
        bool isCritical = UnityEngine.Random.value < gc.GetCritChance();
        bool isSuperCritical = isCritical && UnityEngine.Random.value < 0.1f;

        double finalDamage = baseDamage;
        if (isSuperCritical)
        {
            finalDamage *= gc.GetCritMultiplier() * 2;
            _criticalHits++;
        }
        else if (isCritical)
        {
            finalDamage *= gc.GetCritMultiplier();
            _criticalHits++;
        }

        // コンボボーナス
        if (_comboCount > 10)
        {
            float comboMultiplier = 1f + (_comboCount * 0.01f);
            finalDamage *= comboMultiplier;
        }

        // DPS計算用
        _dpsWindowDamage += finalDamage;

        // ダメージ数字表示
        SpawnDamageNumber(clickPos, finalDamage, isCritical, isSuperCritical);
    }

    private void PlayClickEffects()
    {
        // リップルエフェクト
        if (clickRipple != null)
        {
            clickRipple.AddToClassList("active");
            root.schedule.Execute(() => clickRipple.RemoveFromClassList("active")).ExecuteLater(300);
        }

        // コンボが高い時はグロー
        if (_comboCount > HighComboThreshold && characterGlow != null)
        {
            characterGlow.AddToClassList("active");
            root.schedule.Execute(() => characterGlow.RemoveFromClassList("active")).ExecuteLater(500);
        }

        // SPが満タンに近い時
        var sp = GameController.Instance?.SP;
        if (sp != null && sp.FillRate > 0.9f)
        {
            spGaugeGlow?.AddToClassList("active");
            root.schedule.Execute(() => spGaugeGlow?.RemoveFromClassList("active")).ExecuteLater(200);
        }
    }

    // ========================================
    // コンボシステム
    // ========================================

    private void UpdateCombo()
    {
        _comboCount++;

        // UI更新
        if (comboCountLabel != null)
        {
            comboCountLabel.text = _comboCount.ToString();
            comboCountLabel.AddToClassList("pulse");
            root.schedule.Execute(() => comboCountLabel.RemoveFromClassList("pulse")).ExecuteLater(100);
        }

        // コンボコンテナ表示
        if (comboContainer != null)
        {
            comboContainer.AddToClassList("active");

            if (_comboCount >= HighComboThreshold)
            {
                comboContainer.AddToClassList("high");
            }
        }
    }

    private void UpdateComboDecay()
    {
        if (_comboCount > 0 && Time.time - _lastClickTime > ComboTimeout)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        _comboCount = 0;

        if (comboContainer != null)
        {
            comboContainer.RemoveFromClassList("active");
            comboContainer.RemoveFromClassList("high");
        }

        if (comboCountLabel != null)
        {
            comboCountLabel.text = "0";
        }
    }

    // ========================================
    // ダメージ数字
    // ========================================

    private void SpawnDamageNumber(Vector2 pos, double damage, bool isCritical, bool isSuperCritical)
    {
        Label label;
        if (_damageNumberPool.Count > 0)
        {
            label = _damageNumberPool.Dequeue();
        }
        else if (_activeDamageNumbers.Count > 0)
        {
            label = _activeDamageNumbers[0];
            _activeDamageNumbers.RemoveAt(0);
        }
        else
        {
            return;
        }

        // スタイル設定
        label.RemoveFromClassList("critical");
        label.RemoveFromClassList("super-critical");
        label.RemoveFromClassList("combo-bonus");

        if (isSuperCritical)
        {
            label.AddToClassList("super-critical");
        }
        else if (isCritical)
        {
            label.AddToClassList("critical");
        }

        // テキスト設定
        label.text = FormatDamage(damage);
        if (isSuperCritical) label.text += "!!";
        else if (isCritical) label.text += "!";

        // 位置設定（ランダムオフセット）
        float offsetX = UnityEngine.Random.Range(-50f, 50f);
        float offsetY = UnityEngine.Random.Range(-30f, 30f);
        label.style.left = pos.x + offsetX + 100;
        label.style.top = pos.y + offsetY + 150;

        // 表示
        label.style.display = DisplayStyle.Flex;
        label.style.opacity = 1f;
        label.style.translate = new Translate(0, 0);
        label.style.scale = new Scale(Vector2.one);

        // アニメーション
        root.schedule.Execute(() =>
        {
            label.style.translate = new Translate(0, -80);
            label.style.opacity = 0f;
        }).ExecuteLater(50);

        _activeDamageNumbers.Add(label);
    }

    private void CleanupDamageNumbers()
    {
        for (int i = _activeDamageNumbers.Count - 1; i >= 0; i--)
        {
            var label = _activeDamageNumbers[i];
            if (label.resolvedStyle.opacity < 0.1f)
            {
                label.style.display = DisplayStyle.None;
                _activeDamageNumbers.RemoveAt(i);
                _damageNumberPool.Enqueue(label);
            }
        }
    }

    private string FormatDamage(double damage)
    {
        if (damage >= 1_000_000) return $"{damage / 1_000_000:F1}M";
        if (damage >= 1_000) return $"{damage / 1_000:F1}K";
        return damage.ToString("N0");
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
                particleLayer?.Remove(particle);
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
        if (particleLayer == null) return;

        var particle = new VisualElement();
        particle.AddToClassList("particle");

        if (UnityEngine.Random.value > 0.7f)
        {
            particle.AddToClassList("large");
        }

        float x = UnityEngine.Random.Range(0f, particleLayer.resolvedStyle.width);
        float y = particleLayer.resolvedStyle.height + 10;

        particle.style.left = x;
        particle.style.top = y;
        particle.style.opacity = UnityEngine.Random.Range(0.3f, 0.8f);

        particleLayer.Add(particle);
        _particles.Add(particle);
    }

    // ========================================
    // UI更新
    // ========================================

    private void UpdateUI()
    {
        UpdateMoneyDisplay();
        UpdateSPDisplay();
        UpdatePowerDisplay();
        UpdateStatsDisplay();
    }

    private void UpdateMoneyDisplay()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        double money = gc.Wallet.Money;
        if (moneyLabel != null)
        {
            moneyLabel.text = FormatNumber(money);
        }

        // 収入レート計算
        if (incomeRateLabel != null)
        {
            double incomePerSecond = gc.GetBaseIncome();
            incomeRateLabel.text = $"+{FormatNumber(incomePerSecond)}/s";
        }
    }

    private void UpdateSPDisplay()
    {
        var sp = GameController.Instance?.SP;
        if (sp == null) return;

        // ゲージ更新
        if (spGaugeFill != null)
        {
            float fillPercent = sp.FillRate * 100f;
            spGaugeFill.style.width = new Length(fillPercent, LengthUnit.Percent);

            spGaugeFill.RemoveFromClassList("fever");
            spGaugeFill.RemoveFromClassList("ready");

            if (sp.IsFeverActive)
            {
                spGaugeFill.AddToClassList("fever");
            }
            else if (sp.FillRate >= 1f)
            {
                spGaugeFill.AddToClassList("ready");
            }
        }

        // テキスト更新
        if (spText != null)
        {
            spText.text = $"{sp.CurrentSP:F0} / {sp.MaxSP:F0}";
        }

        // アイコングロー
        if (spIconGlow != null)
        {
            spIconGlow.RemoveFromClassList("ready");
            spIconGlow.RemoveFromClassList("fever");

            if (sp.IsFeverActive)
            {
                spIconGlow.AddToClassList("fever");
            }
            else if (sp.FillRate >= 1f)
            {
                spIconGlow.AddToClassList("ready");
            }
        }
    }

    private void UpdatePowerDisplay()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        if (powerValueLabel != null)
        {
            double power = gc.GetClickPower();
            powerValueLabel.text = FormatNumber(power);
        }

        // 倍率表示
        if (multiplierLabel != null)
        {
            float mult = gc.GetGlobalMultiplier();
            if (mult > 1f)
            {
                multiplierLabel.text = $"x{mult:F1}";
                multiplierLabel.AddToClassList("active");
                powerValueLabel?.AddToClassList("boosted");
            }
            else
            {
                multiplierLabel.RemoveFromClassList("active");
                powerValueLabel?.RemoveFromClassList("boosted");
            }
        }
    }

    private void UpdateStatsDisplay()
    {
        if (clicksValueLabel != null)
        {
            clicksValueLabel.text = _totalClicks.ToString("N0");
        }

        if (critValueLabel != null)
        {
            float critRate = _totalClicks > 0 ? (float)_criticalHits / _totalClicks * 100f : 0f;
            critValueLabel.text = $"{critRate:F1}%";
        }

        if (dpsValueLabel != null)
        {
            dpsValueLabel.text = FormatNumber(_currentDPS);
        }
    }

    private void UpdateDPS()
    {
        float elapsed = Time.time - _dpsWindowStart;
        if (elapsed >= 1f)
        {
            _currentDPS = (float)(_dpsWindowDamage / elapsed);
            _dpsWindowDamage = 0;
            _dpsWindowStart = Time.time;
            UpdateStatsDisplay();
        }
    }

    private void UpdateFeverTimer()
    {
        var sp = GameController.Instance?.SP;
        if (sp == null || !sp.IsFeverActive) return;

        if (feverTimerLabel != null)
        {
            float remaining = sp.GetFeverRemainingTime();
            feverTimerLabel.text = $"{remaining:F1}";
        }
    }

    // ========================================
    // Fever処理
    // ========================================

    private void OnFeverStarted()
    {
        // フィーバーインジケーター
        feverIndicator?.AddToClassList("active");

        // オーバーレイ
        feverOverlay?.AddToClassList("active");

        // ビッグテキストアニメーション
        if (feverBigText != null)
        {
            feverBigText.AddToClassList("show");
            root.schedule.Execute(() => feverBigText.RemoveFromClassList("show")).ExecuteLater(2000);
        }

        // フラッシュエフェクト
        if (feverFlash != null)
        {
            feverFlash.AddToClassList("pulse");
            root.schedule.Execute(() => feverFlash.RemoveFromClassList("pulse")).ExecuteLater(150);
        }

        // SP表示更新
        spGaugeFill?.AddToClassList("fever");
        spIconGlow?.AddToClassList("fever");

        LogUIController.Msg("<color=#FF5050><b>FEVER MODE!!</b></color>");
    }

    private void OnFeverEnded()
    {
        feverIndicator?.RemoveFromClassList("active");
        feverOverlay?.RemoveFromClassList("active");
        spGaugeFill?.RemoveFromClassList("fever");
        spIconGlow?.RemoveFromClassList("fever");

        LogUIController.Msg("Fever mode ended.");
    }

    // ========================================
    // スロット処理
    // ========================================

    private void OnSlotTriggered()
    {
        // オーバーレイ表示
        slotOverlay?.AddToClassList("active");

        // フラッシュエフェクト
        if (slotFlash != null)
        {
            slotFlash.AddToClassList("pulse");
            root.schedule.Execute(() => slotFlash.RemoveFromClassList("pulse")).ExecuteLater(100);
            root.schedule.Execute(() =>
            {
                slotFlash.AddToClassList("pulse");
                root.schedule.Execute(() => slotFlash.RemoveFromClassList("pulse")).ExecuteLater(100);
            }).ExecuteLater(200);
        }

        // ビッグテキストアニメーション
        if (slotBigText != null)
        {
            slotBigText.AddToClassList("show");
        }

        // ボーナステキストアニメーション
        if (slotBonusText != null)
        {
            slotBonusText.AddToClassList("show");
        }

        // 3秒後に非表示
        root.schedule.Execute(() =>
        {
            slotBigText?.RemoveFromClassList("show");
            slotBonusText?.RemoveFromClassList("show");
        }).ExecuteLater(2500);

        root.schedule.Execute(() =>
        {
            slotOverlay?.RemoveFromClassList("active");
        }).ExecuteLater(3000);

        LogUIController.Msg("<color=#FFD700><b>★★★ JACKPOT!! ★★★</b></color>");
    }

    // ========================================
    // 好感度イベント
    // ========================================

    private void OnAffectionChanged(string characterId, int newValue, int delta)
    {
        if (delta > 0 && effectLayer != null)
        {
            // ハートエフェクト表示
            var heart = new Label();
            heart.text = "+";
            heart.AddToClassList("floating-text");
            heart.AddToClassList("affection");

            float x = UnityEngine.Random.Range(100f, 300f);
            heart.style.left = x;
            heart.style.top = 300;

            effectLayer.Add(heart);

            root.schedule.Execute(() => heart.AddToClassList("fade-out")).ExecuteLater(50);
            root.schedule.Execute(() => effectLayer.Remove(heart)).ExecuteLater(1100);
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

        // クリックイベント解除
        if (onClickCallback != null)
        {
            clickTarget?.UnregisterCallback(onClickCallback);
            onClickCallback = null;
        }

        // GameControllerイベント解除
        var gc = GameController.Instance;
        if (gc != null)
        {
            if (gc.Wallet != null && onMoneyChangedCallback != null)
            {
                gc.Wallet.OnMoneyChanged -= onMoneyChangedCallback;
            }

            if (gc.SP != null)
            {
                if (onSPChangedCallback != null)
                    gc.SP.OnSPChanged -= onSPChangedCallback;
                if (onFeverStartedCallback != null)
                    gc.SP.OnFeverStarted -= onFeverStartedCallback;
                if (onFeverEndedCallback != null)
                    gc.SP.OnFeverEnded -= onFeverEndedCallback;
            }
        }

        // 好感度イベント解除
        if (AffectionManager.Instance != null && onAffectionChangedCallback != null)
        {
            AffectionManager.Instance.OnAffectionChanged -= onAffectionChangedCallback;
        }

        // スロットイベント解除
        if (gc != null && gc.OnSlotTriggered != null && onSlotTriggeredCallback != null)
        {
            gc.OnSlotTriggered.RemoveListener(onSlotTriggeredCallback);
        }

        // パーティクルクリア
        foreach (var particle in _particles)
        {
            particleLayer?.Remove(particle);
        }
        _particles.Clear();

        // ダメージ数字クリア
        _damageNumberPool.Clear();
        _activeDamageNumbers.Clear();

        // 参照クリア
        onMoneyChangedCallback = null;
        onSPChangedCallback = null;
        onFeverStartedCallback = null;
        onFeverEndedCallback = null;
        onAffectionChangedCallback = null;
        onSlotTriggeredCallback = null;

        LogUIController.LogSystem("Home View Disposed.");
    }
}
