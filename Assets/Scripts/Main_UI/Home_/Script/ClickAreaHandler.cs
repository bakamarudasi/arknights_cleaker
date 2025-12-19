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

        // パーティクルエフェクト
        SpawnClickParticles(clickPos, isCritical, isSuperCritical);

        // ダメージ数字表示
        SpawnDamageNumber(clickPos, finalDamage, isCritical, isSuperCritical);
    }

    private void SpawnClickParticles(Vector2 clickPos, bool isCritical, bool isSuperCritical)
    {
        if (_effectLayer == null) return;

        // クリックフラッシュ
        var flash = new VisualElement();
        flash.AddToClassList("click-flash");
        flash.style.left = clickPos.x + 100 - 30;
        flash.style.top = clickPos.y + 150 - 30;
        _effectLayer.Add(flash);
        _root.schedule.Execute(() => flash.AddToClassList("expand")).ExecuteLater(10);
        _root.schedule.Execute(() => _effectLayer.Remove(flash)).ExecuteLater(250);

        // 衝撃波リング
        var ring = new VisualElement();
        ring.AddToClassList("impact-ring");
        if (isSuperCritical) ring.AddToClassList("super-critical");
        else if (isCritical) ring.AddToClassList("critical");

        ring.style.left = clickPos.x + 100 - 40;
        ring.style.top = clickPos.y + 150 - 40;
        _effectLayer.Add(ring);
        _root.schedule.Execute(() => ring.AddToClassList("expand")).ExecuteLater(10);
        _root.schedule.Execute(() => _effectLayer.Remove(ring)).ExecuteLater(450);

        // スパークパーティクル
        int sparkCount = isSuperCritical ? 12 : (isCritical ? 8 : 5);
        for (int i = 0; i < sparkCount; i++)
        {
            var spark = new VisualElement();
            spark.AddToClassList("spark-particle");
            if (isSuperCritical) spark.AddToClassList("super-critical");
            else if (isCritical) spark.AddToClassList("critical");
            else if (UnityEngine.Random.value > 0.7f) spark.AddToClassList("large");

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(80f, 180f);
            float offsetX = Mathf.Cos(angle) * distance;
            float offsetY = Mathf.Sin(angle) * distance - 50f; // 上方向にバイアス

            spark.style.left = clickPos.x + 100;
            spark.style.top = clickPos.y + 150;
            _effectLayer.Add(spark);

            // フライアウトアニメーション
            _root.schedule.Execute(() =>
            {
                spark.style.translate = new Translate(offsetX, offsetY);
                spark.AddToClassList("fly");
            }).ExecuteLater(10);
            _root.schedule.Execute(() => _effectLayer.Remove(spark)).ExecuteLater(550);
        }

        // スターパーティクル（クリティカル時のみ）
        if (isCritical)
        {
            int starCount = isSuperCritical ? 4 : 2;
            for (int i = 0; i < starCount; i++)
            {
                var star = new Label();
                star.AddToClassList("star-particle");
                star.text = "★";

                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float distance = UnityEngine.Random.Range(100f, 200f);
                float offsetX = Mathf.Cos(angle) * distance;
                float offsetY = Mathf.Sin(angle) * distance - 80f;
                float rotation = UnityEngine.Random.Range(-180f, 180f);

                star.style.left = clickPos.x + 100;
                star.style.top = clickPos.y + 150;
                _effectLayer.Add(star);

                _root.schedule.Execute(() =>
                {
                    star.style.translate = new Translate(offsetX, offsetY);
                    star.style.rotate = new Rotate(Angle.Degrees(rotation));
                    star.AddToClassList("fly");
                }).ExecuteLater(10);
                _root.schedule.Execute(() => _effectLayer.Remove(star)).ExecuteLater(850);
            }
        }
    }

    private void PlayClickEffects()
    {
        // クリックターゲットのバウンスバック
        if (_clickTarget != null)
        {
            _clickTarget.AddToClassList("flash");
            _root.schedule.Execute(() =>
            {
                _clickTarget.RemoveFromClassList("flash");
                _clickTarget.AddToClassList("bounce-back");
            }).ExecuteLater(50);
            _root.schedule.Execute(() => _clickTarget.RemoveFromClassList("bounce-back")).ExecuteLater(150);
        }

        // リップルエフェクト（広がって消える）
        if (_clickRipple != null)
        {
            _clickRipple.RemoveFromClassList("expand");
            _clickRipple.AddToClassList("active");
            _root.schedule.Execute(() =>
            {
                _clickRipple.RemoveFromClassList("active");
                _clickRipple.AddToClassList("expand");
            }).ExecuteLater(100);
            _root.schedule.Execute(() => _clickRipple.RemoveFromClassList("expand")).ExecuteLater(500);
        }

        // キャラクターグロー（コンボに応じて）
        if (_characterGlow != null)
        {
            if (_comboCount > HighComboThreshold)
            {
                _characterGlow.AddToClassList("active");
                _characterGlow.AddToClassList("pulse");
                _root.schedule.Execute(() =>
                {
                    _characterGlow.RemoveFromClassList("pulse");
                }).ExecuteLater(150);
                _root.schedule.Execute(() => _characterGlow.RemoveFromClassList("active")).ExecuteLater(400);
            }
            else if (_comboCount > 10)
            {
                _characterGlow.AddToClassList("active");
                _root.schedule.Execute(() => _characterGlow.RemoveFromClassList("active")).ExecuteLater(300);
            }
        }

        // 画面シェイク（高コンボ時）
        if (_comboCount > 30)
        {
            var container = _root.Q<VisualElement>("home-container");
            if (container != null)
            {
                bool shakeLeft = UnityEngine.Random.value > 0.5f;
                container.AddToClassList(shakeLeft ? "shake-left" : "shake");
                _root.schedule.Execute(() =>
                {
                    container.RemoveFromClassList("shake");
                    container.RemoveFromClassList("shake-left");
                }).ExecuteLater(50);
            }
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

            // コンボ数に応じたパルスエフェクト
            if (_comboCount >= 100 && _comboCount % 10 == 0)
            {
                // 100コンボ以上で10の倍数の時は特大パルス
                _comboCountLabel.AddToClassList("mega-pulse");
                _root.schedule.Execute(() => _comboCountLabel.RemoveFromClassList("mega-pulse")).ExecuteLater(150);
            }
            else
            {
                _comboCountLabel.AddToClassList("pulse");
                _root.schedule.Execute(() => _comboCountLabel.RemoveFromClassList("pulse")).ExecuteLater(80);
            }
        }

        if (_comboContainer != null)
        {
            _comboContainer.AddToClassList("active");

            // 高コンボ時のシェイク
            if (_comboCount > 30 && _comboCount % 5 == 0)
            {
                _comboContainer.AddToClassList("shake");
                _root.schedule.Execute(() => _comboContainer.RemoveFromClassList("shake")).ExecuteLater(60);
            }

            // コンボ段階に応じたスタイル変更
            if (_comboCount >= 100)
            {
                _comboContainer.RemoveFromClassList("high");
                _comboContainer.AddToClassList("fever");
            }
            else if (_comboCount >= HighComboThreshold)
            {
                _comboContainer.RemoveFromClassList("fever");
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
            _comboContainer.RemoveFromClassList("fever");
            _comboContainer.RemoveFromClassList("shake");
        }

        if (_comboCountLabel != null)
        {
            _comboCountLabel.text = "0";
            _comboCountLabel.RemoveFromClassList("pulse");
            _comboCountLabel.RemoveFromClassList("mega-pulse");
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

        // スタイルリセット
        label.RemoveFromClassList("critical");
        label.RemoveFromClassList("super-critical");
        label.RemoveFromClassList("combo-bonus");
        label.RemoveFromClassList("spawn");
        label.RemoveFromClassList("fly");

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
        float offsetX = UnityEngine.Random.Range(-60f, 60f);
        float offsetY = UnityEngine.Random.Range(-40f, 40f);
        label.style.left = pos.x + offsetX + 100;
        label.style.top = pos.y + offsetY + 150;

        // 初期状態
        label.style.display = DisplayStyle.Flex;
        label.style.translate = new Translate(0, 0);
        label.style.rotate = new Rotate(Angle.Degrees(0));

        // スポーンアニメーション（ポップイン）
        _root.schedule.Execute(() => label.AddToClassList("spawn")).ExecuteLater(10);

        // フライアウトアニメーション
        _root.schedule.Execute(() =>
        {
            label.RemoveFromClassList("spawn");
            label.AddToClassList("fly");
        }).ExecuteLater(200);

        _activeDamageNumbers.Add(label);
    }

    /// <summary>非表示になったダメージ数字をプールに戻す</summary>
    public void CleanupDamageNumbers()
    {
        for (int i = _activeDamageNumbers.Count - 1; i >= 0; i--)
        {
            var label = _activeDamageNumbers[i];
            if (label.ClassListContains("fly") && label.resolvedStyle.opacity < 0.1f)
            {
                label.style.display = DisplayStyle.None;
                label.RemoveFromClassList("spawn");
                label.RemoveFromClassList("fly");
                label.RemoveFromClassList("critical");
                label.RemoveFromClassList("super-critical");
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
