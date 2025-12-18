using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// スタート画面（タイトル画面）のUIコントローラー
/// Rhodes Island Clicker - 起動時のタイトル画面
/// </summary>
public class StartUIController : IViewController
{
    private VisualElement root;

    // ========================================
    // UI要素参照
    // ========================================

    // レイヤー
    private VisualElement particleLayer;
    private VisualElement fadeOverlay;

    // ロゴ
    private Label logoPrefix;
    private Label logoMain;
    private VisualElement logoUnderline;
    private Label subtitle;

    // シンボル
    private VisualElement symbolContainer;
    private VisualElement symbolRingOuter;
    private VisualElement symbolRingInner;
    private VisualElement symbolCore;

    // スタートプロンプト
    private Label tapToStart;
    private VisualElement loadingBar;
    private VisualElement loadingFill;

    // ========================================
    // 状態管理
    // ========================================

    // パーティクル
    private readonly List<VisualElement> _particles = new();
    private const int MaxParticles = 40;
    private float _particleSpawnTimer;

    // アニメーション
    private bool _isBlinking = true;
    private float _blinkTimer;
    private const float BlinkInterval = 0.8f;

    private bool _isTransitioning = false;
    private float _loadProgress = 0f;

    // コールバック参照
    private EventCallback<ClickEvent> onClickCallback;
    private IVisualElementScheduledItem _updateSchedule;
    private IVisualElementScheduledItem _ringAnimSchedule;

    // 画面遷移イベント
    public event Action OnStartRequested;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        QueryElements();
        SetupCallbacks();
        SetupAnimations();
        SetupUpdateLoop();

        // 初期フェードイン
        PlayEntranceAnimation();

        LogUIController.LogSystem("Start View Initialized.");
    }

    private void QueryElements()
    {
        // レイヤー
        particleLayer = root.Q<VisualElement>("particle-layer");
        fadeOverlay = root.Q<VisualElement>("fade-overlay");

        // ロゴ
        logoPrefix = root.Q<Label>("logo-prefix");
        logoMain = root.Q<Label>("logo-main");
        logoUnderline = root.Q<VisualElement>("logo-underline");
        subtitle = root.Q<Label>("subtitle");

        // シンボル
        symbolContainer = root.Q<VisualElement>("symbol-container");
        symbolRingOuter = root.Q<VisualElement>("symbol-ring-outer");
        symbolRingInner = root.Q<VisualElement>("symbol-ring-inner");
        symbolCore = root.Q<VisualElement>("symbol-core");

        // スタートプロンプト
        tapToStart = root.Q<Label>("tap-to-start");
        loadingBar = root.Q<VisualElement>("loading-bar");
        loadingFill = root.Q<VisualElement>("loading-fill");
    }

    private void SetupCallbacks()
    {
        // 画面全体のクリックでゲーム開始
        var startContainer = root.Q<VisualElement>("start-container");
        if (startContainer != null)
        {
            onClickCallback = OnScreenTapped;
            startContainer.RegisterCallback(onClickCallback);
        }
    }

    private void SetupAnimations()
    {
        // シンボルリングの回転アニメーション
        float rotationOuter = 0f;
        float rotationInner = 0f;

        _ringAnimSchedule = root.schedule.Execute(() =>
        {
            rotationOuter += 0.5f;
            rotationInner -= 0.8f;

            if (symbolRingOuter != null)
            {
                symbolRingOuter.style.rotate = new Rotate(rotationOuter);
            }
            if (symbolRingInner != null)
            {
                symbolRingInner.style.rotate = new Rotate(rotationInner);
            }
        }).Every(30);
    }

    private void SetupUpdateLoop()
    {
        _updateSchedule = root.schedule.Execute(() =>
        {
            UpdateParticles();
            UpdateBlinkAnimation();
            UpdateLoadingProgress();
            UpdateSymbolPulse();
        }).Every(50); // 20fps更新
    }

    // ========================================
    // エントランスアニメーション
    // ========================================

    private void PlayEntranceAnimation()
    {
        // 初期状態: 全て透明
        if (logoPrefix != null) logoPrefix.style.opacity = 0;
        if (logoMain != null) logoMain.style.opacity = 0;
        if (logoUnderline != null) logoUnderline.style.opacity = 0;
        if (subtitle != null) subtitle.style.opacity = 0;
        if (symbolContainer != null) symbolContainer.style.opacity = 0;
        if (tapToStart != null) tapToStart.style.opacity = 0;

        // シーケンシャルなフェードイン
        root.schedule.Execute(() =>
        {
            if (logoPrefix != null)
            {
                logoPrefix.style.opacity = 1;
                logoPrefix.style.translate = new Translate(0, 0);
            }
        }).ExecuteLater(300);

        root.schedule.Execute(() =>
        {
            if (logoMain != null)
            {
                logoMain.style.opacity = 1;
            }
        }).ExecuteLater(500);

        root.schedule.Execute(() =>
        {
            if (logoUnderline != null) logoUnderline.style.opacity = 0.8f;
            if (subtitle != null) subtitle.style.opacity = 0.7f;
        }).ExecuteLater(700);

        root.schedule.Execute(() =>
        {
            if (symbolContainer != null) symbolContainer.style.opacity = 1;
        }).ExecuteLater(900);

        root.schedule.Execute(() =>
        {
            if (tapToStart != null) tapToStart.style.opacity = 1;
            _isBlinking = true;
        }).ExecuteLater(1200);
    }

    // ========================================
    // タップ処理
    // ========================================

    private void OnScreenTapped(ClickEvent evt)
    {
        if (_isTransitioning) return;

        _isTransitioning = true;
        _isBlinking = false;

        // TAP TO START を非表示
        tapToStart?.AddToClassList("hidden");

        // ローディングバー表示
        loadingBar?.AddToClassList("show");

        // シンボルのグロー
        symbolCore?.AddToClassList("glow");

        LogUIController.Msg("Initializing...");

        // ローディング完了後に画面遷移
        root.schedule.Execute(() =>
        {
            StartTransition();
        }).ExecuteLater(1500);
    }

    private void StartTransition()
    {
        // フェードアウト
        fadeOverlay?.AddToClassList("active");

        // 遷移イベント発火
        root.schedule.Execute(() =>
        {
            OnStartRequested?.Invoke();
            // Home画面に遷移
            MainUIController.Instance?.SwitchToMenu(MenuType.Home);
        }).ExecuteLater(600);
    }

    // ========================================
    // 更新処理
    // ========================================

    private void UpdateBlinkAnimation()
    {
        if (!_isBlinking || tapToStart == null) return;

        _blinkTimer += 0.05f;

        if (_blinkTimer >= BlinkInterval)
        {
            _blinkTimer = 0f;
            tapToStart.ToggleInClassList("blink");
        }
    }

    private void UpdateLoadingProgress()
    {
        if (!_isTransitioning || loadingFill == null) return;

        _loadProgress += 2f;
        if (_loadProgress > 100f) _loadProgress = 100f;

        loadingFill.style.width = new Length(_loadProgress, LengthUnit.Percent);
    }

    private void UpdateSymbolPulse()
    {
        // シンボルコアの微妙なパルス効果（CSS transitionで実現）
    }

    // ========================================
    // パーティクルエフェクト
    // ========================================

    private void UpdateParticles()
    {
        _particleSpawnTimer += 0.05f;

        // 新規パーティクル生成
        if (_particleSpawnTimer > 0.15f && _particles.Count < MaxParticles)
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
                particle.style.top = currentTop - 0.8f;
                // 少し横にも動かす
                var currentLeft = particle.resolvedStyle.left;
                particle.style.left = currentLeft + UnityEngine.Random.Range(-0.3f, 0.3f);
            }
        }
    }

    private void SpawnParticle()
    {
        if (particleLayer == null) return;

        var particle = new VisualElement();
        particle.AddToClassList("particle");

        // ランダムでサイズを変える
        float rand = UnityEngine.Random.value;
        if (rand > 0.85f)
        {
            particle.AddToClassList("large");
        }
        else if (rand > 0.7f)
        {
            particle.AddToClassList("bright");
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
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        // スケジュール停止
        _updateSchedule?.Pause();
        _ringAnimSchedule?.Pause();

        // クリックイベント解除
        var startContainer = root?.Q<VisualElement>("start-container");
        if (startContainer != null && onClickCallback != null)
        {
            startContainer.UnregisterCallback(onClickCallback);
            onClickCallback = null;
        }

        // パーティクルクリア
        foreach (var particle in _particles)
        {
            particleLayer?.Remove(particle);
        }
        _particles.Clear();

        // イベントクリア
        OnStartRequested = null;

        LogUIController.LogSystem("Start View Disposed.");
    }
}
