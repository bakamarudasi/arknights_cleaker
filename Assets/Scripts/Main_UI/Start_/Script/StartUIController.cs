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

    // メニューボタン
    private Button btnNewGame;
    private Button btnLoad;
    private VisualElement menuArea;

    // ========================================
    // 状態管理
    // ========================================

    // パーティクル
    private readonly List<VisualElement> _particles = new();
    private const int MaxParticles = 40;
    private float _particleSpawnTimer;

    // アニメーション
    private bool _isTransitioning = false;

    // コールバック参照
    private IVisualElementScheduledItem _updateSchedule;

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

        // メニューボタン
        menuArea = root.Q<VisualElement>("menu-area");
        btnNewGame = root.Q<Button>("btn-new-game");
        btnLoad = root.Q<Button>("btn-load");
    }

    private void SetupCallbacks()
    {
        // NEW GAME ボタン
        if (btnNewGame != null)
        {
            btnNewGame.clicked += OnNewGameClicked;
        }

        // CONTINUE ボタン
        if (btnLoad != null)
        {
            btnLoad.clicked += OnLoadClicked;

            // セーブデータがない場合は無効化
            if (!HasSaveData())
            {
                btnLoad.AddToClassList("disabled");
                btnLoad.SetEnabled(false);
            }
        }
    }

    private void SetupUpdateLoop()
    {
        _updateSchedule = root.schedule.Execute(() =>
        {
            UpdateParticles();
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
        if (menuArea != null) menuArea.style.opacity = 0;

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
            if (menuArea != null) menuArea.style.opacity = 1;
        }).ExecuteLater(1000);
    }

    // ========================================
    // ボタン処理
    // ========================================

    private void OnNewGameClicked()
    {
        if (_isTransitioning) return;

        LogUIController.Msg("Starting new game...");
        StartTransition(isNewGame: true);
    }

    private void OnLoadClicked()
    {
        if (_isTransitioning) return;

        LogUIController.Msg("Loading save data...");
        StartTransition(isNewGame: false);
    }

    private void StartTransition(bool isNewGame)
    {
        _isTransitioning = true;

        // フェードアウト
        fadeOverlay?.AddToClassList("active");

        // 遷移イベント発火
        root.schedule.Execute(() =>
        {
            OnStartRequested?.Invoke();

            if (isNewGame)
            {
                // 新規ゲーム開始処理（必要であれば既存セーブをクリア）
                // TODO: セーブデータの初期化処理があれば追加
            }

            // Home画面に遷移
            MainUIController.Instance?.SwitchToMenu(MenuType.Home);
        }).ExecuteLater(600);
    }

    // ========================================
    // セーブデータチェック
    // ========================================

    private bool HasSaveData()
    {
        // SaveManagerが存在するか確認し、セーブデータの有無を返す
        // 簡易実装: PlayerPrefsにセーブ存在フラグがあるかチェック
        return PlayerPrefs.HasKey("SaveExists") || PlayerPrefs.HasKey("Money");
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

        // ボタンイベント解除
        if (btnNewGame != null)
        {
            btnNewGame.clicked -= OnNewGameClicked;
        }
        if (btnLoad != null)
        {
            btnLoad.clicked -= OnLoadClicked;
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
