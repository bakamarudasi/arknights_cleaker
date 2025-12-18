using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// クリックエリアの処理を担当するハンドラー
/// クリックイベント、コンボシステム、ダメージ数字表示を管理
/// </summary>
public class ClickAreaHandler : IDisposable
{
    // ========================================
    // UI要素参照
    // ========================================

    private readonly VisualElement _root;
    private readonly VisualElement _effectLayer;
    private VisualElement _clickTarget;
    private VisualElement _clickRipple;
    private VisualElement _characterGlow;

    // ========================================
    // コンボシステム
    // ========================================

    private int _comboCount;
    private float _lastClickTime;
    private const float ComboTimeout = 1.5f;
    private const int HighComboThreshold = 50;

    private VisualElement _comboContainer;
    private Label _comboCountLabel;

    // ========================================
    // 統計
    // ========================================

    private int _totalClicks;
    private int _criticalHits;
    private float _dpsWindowStart;
    private double _dpsWindowDamage;
    private float _currentDPS;

    public int TotalClicks => _totalClicks;
    public int CriticalHits => _criticalHits;
    public float CurrentDPS => _currentDPS;

    // ========================================
    // ダメージ数字プール
    // ========================================

    private readonly Queue<Label> _damageNumberPool = new();
    private readonly List<Label> _activeDamageNumbers = new();
    private const int DamageNumberPoolSize = 20;

    // ========================================
    // コールバック
    // ========================================

    private EventCallback<PointerDownEvent> _onPointerDownCallback;

    /// <summary>クリック時に発火するイベント</summary>
    public event Action OnClicked;

    // ========================================
    // コンストラクタ
    // ========================================

    public ClickAreaHandler(VisualElement root, VisualElement effectLayer)
    {
        _root = root;
        _effectLayer = effectLayer;
        _dpsWindowStart = Time.time;
    }

    // ========================================
    // 初期化
    // ========================================

    public void Initialize()
    {
        QueryElements();
        InitializeDamageNumberPool();
        SetupCallbacks();

        LogUIController.LogSystem("ClickAreaHandler Initialized.");
    }

    private void QueryElements()
    {
        _clickTarget = _root.Q<VisualElement>("click-target");
        _clickRipple = _root.Q<VisualElement>("click-ripple");
        _characterGlow = _root.Q<VisualElement>("character-glow");
        _comboContainer = _root.Q<VisualElement>("combo-container");
        _comboCountLabel = _root.Q<Label>("combo-count");

        Debug.Log($"[ClickAreaHandler] clickTarget = {_clickTarget}");
    }

    private void InitializeDamageNumberPool()
    {
        if (_effectLayer == null) return;

        for (int i = 0; i < DamageNumberPoolSize; i++)
        {
            var label = new Label();
            label.AddToClassList("damage-number");
            label.style.display = DisplayStyle.None;
            _effectLayer.Add(label);
            _damageNumberPool.Enqueue(label);
        }
    }

    private void SetupCallbacks()
    {
        if (_clickTarget == null)
        {
            Debug.LogError("[ClickAreaHandler] clickTarget is NULL! Click events will not work.");
            return;
        }

        _onPointerDownCallback = OnPointerDown;
        _clickTarget.RegisterCallback(_onPointerDownCallback);

        _clickTarget.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            Debug.Log($"[ClickAreaHandler] clickTarget geometry: {evt.newRect.width}x{evt.newRect.height}");
        });

        Debug.Log("[ClickAreaHandler] PointerDown callback registered.");
    }

    // ========================================
    // クリック処理
    // ========================================

    private void OnPointerDown(PointerDownEvent evt)
    {
        Debug.Log($"[ClickAreaHandler] PointerDown detected at {evt.localPosition}");

        var gc = GameController.Instance;
        if (gc == null)
        {
            Debug.LogError("[ClickAreaHandler] GameController.Instance is NULL!");
            return;
        }

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

        // イベント発火
        OnClicked?.Invoke();
    }

    private void ProcessClickDamage(Vector2 clickPos)
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        double baseDamage = gc.FinalClickPower;

        // クリティカル判定
        bool isCritical = UnityEngine.Random.value < gc.FinalCritChance;
        bool isSuperCritical = isCritical && UnityEngine.Random.value < 0.1f;

        double finalDamage = baseDamage;
        if (isSuperCritical)
        {
            finalDamage *= gc.FinalCritMultiplier * 2;
            _criticalHits++;
        }
        else if (isCritical)
        {
            finalDamage *= gc.FinalCritMultiplier;
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
        if (_clickRipple != null)
        {
            _clickRipple.AddToClassList("active");
            _root.schedule.Execute(() => _clickRipple.RemoveFromClassList("active")).ExecuteLater(300);
        }

        // コンボが高い時はグロー
        if (_comboCount > HighComboThreshold && _characterGlow != null)
        {
            _characterGlow.AddToClassList("active");
            _root.schedule.Execute(() => _characterGlow.RemoveFromClassList("active")).ExecuteLater(500);
        }

        // SPが満タンに近い時
        var sp = GameController.Instance?.SP;
        if (sp != null && sp.FillRate > 0.9f)
        {
            var spGaugeGlow = _root.Q<VisualElement>("sp-gauge-glow");
            spGaugeGlow?.AddToClassList("active");
            _root.schedule.Execute(() => spGaugeGlow?.RemoveFromClassList("active")).ExecuteLater(200);
        }
    }

    // ========================================
    // コンボシステム
    // ========================================

    private void UpdateCombo()
    {
        _comboCount++;

        if (_comboCountLabel != null)
        {
            _comboCountLabel.text = _comboCount.ToString();
            _comboCountLabel.AddToClassList("pulse");
            _root.schedule.Execute(() => _comboCountLabel.RemoveFromClassList("pulse")).ExecuteLater(100);
        }

        if (_comboContainer != null)
        {
            _comboContainer.AddToClassList("active");

            if (_comboCount >= HighComboThreshold)
            {
                _comboContainer.AddToClassList("high");
            }
        }
    }

    /// <summary>コンボの減衰をチェック（毎フレーム呼び出し）</summary>
    public void UpdateComboDecay()
    {
        if (_comboCount > 0 && Time.time - _lastClickTime > ComboTimeout)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        _comboCount = 0;

        if (_comboContainer != null)
        {
            _comboContainer.RemoveFromClassList("active");
            _comboContainer.RemoveFromClassList("high");
        }

        if (_comboCountLabel != null)
        {
            _comboCountLabel.text = "0";
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
        _root.schedule.Execute(() =>
        {
            label.style.translate = new Translate(0, -80);
            label.style.opacity = 0f;
        }).ExecuteLater(50);

        _activeDamageNumbers.Add(label);
    }

    /// <summary>非表示になったダメージ数字をプールに戻す</summary>
    public void CleanupDamageNumbers()
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

    // ========================================
    // DPS計算
    // ========================================

    /// <summary>DPSを更新（毎秒呼び出し推奨）</summary>
    public void UpdateDPS()
    {
        float elapsed = Time.time - _dpsWindowStart;
        if (elapsed >= 1f)
        {
            _currentDPS = (float)(_dpsWindowDamage / elapsed);
            _dpsWindowDamage = 0;
            _dpsWindowStart = Time.time;
        }
    }

    // ========================================
    // ユーティリティ
    // ========================================

    private string FormatDamage(double damage)
    {
        if (damage >= 1_000_000) return $"{damage / 1_000_000:F1}M";
        if (damage >= 1_000) return $"{damage / 1_000:F1}K";
        return damage.ToString("N0");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        if (_onPointerDownCallback != null)
        {
            _clickTarget?.UnregisterCallback(_onPointerDownCallback);
            _onPointerDownCallback = null;
        }

        _damageNumberPool.Clear();
        _activeDamageNumbers.Clear();

        OnClicked = null;

        LogUIController.LogSystem("ClickAreaHandler Disposed.");
    }
}
