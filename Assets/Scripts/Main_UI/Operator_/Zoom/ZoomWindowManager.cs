using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// ズーム窓の複数管理マネージャー
/// ゾーンタッチに応じてズーム窓を生成・管理
/// </summary>
public class ZoomWindowManager : MonoBehaviour
{
    public static ZoomWindowManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================

    [Header("=== 設定 ===")]
    [SerializeField] private int maxWindows = 3;

    [Header("=== デバッグ ===")]
    [SerializeField] private bool debugLog = true;

    // ========================================
    // 内部状態
    // ========================================

    private CharacterSceneData _currentSceneData;
    private VisualElement _container;
    private List<ZoomWindowInstance> _activeWindows = new List<ZoomWindowInstance>();
    private Queue<ZoomWindowInstance> _windowPool = new Queue<ZoomWindowInstance>();
    private int _windowIndex = 0;

    // ========================================
    // イベント
    // ========================================

    public event Action<int> OnActiveWindowCountChanged;
    public event Action<CharacterInteractionZone.ZoneType> OnZoomWindowOpened;
    public event Action OnAllWindowsClosed;

    // ========================================
    // プロパティ
    // ========================================

    public int ActiveWindowCount => _activeWindows.Count;
    public bool HasActiveWindows => _activeWindows.Count > 0;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cleanup();
    }

    /// <summary>
    /// UIコンテナとシーンデータで初期化
    /// </summary>
    public void Initialize(VisualElement container, CharacterSceneData sceneData)
    {
        _container = container;
        _currentSceneData = sceneData;

        // メインプレゼンターのレンズモード変更を監視
        SubscribeToMainPresenter();

        Log($"Initialized with scene: {sceneData?.sceneId ?? "null"}");
    }

    /// <summary>
    /// シーン変更時に呼び出し
    /// </summary>
    public void OnSceneChanged(CharacterSceneData newSceneData)
    {
        // 全窓を閉じる
        CloseAllWindows();

        _currentSceneData = newSceneData;
        Log($"Scene changed to: {newSceneData?.sceneId ?? "null"}");
    }

    // ========================================
    // ゾーンタッチ処理
    // ========================================

    /// <summary>
    /// ゾーンがタッチされた時の処理
    /// </summary>
    public void OnZoneTouched(CharacterInteractionZone.ZoneType zone, int combo)
    {
        if (_currentSceneData == null) return;

        // このゾーンのズーム設定を取得
        var config = _currentSceneData.GetZoomConfig(zone);
        if (config == null || config.zoomPrefab == null)
        {
            Log($"No zoom config for zone: {zone}");
            return;
        }

        // 既存の窓を探す
        var existingWindow = FindWindowByZone(zone);

        if (existingWindow != null)
        {
            // 既に表示中 → タッチ更新
            existingWindow.OnTouched(combo);
            Log($"Updated existing window for zone: {zone}, combo: {combo}");
        }
        else
        {
            // 新規表示判定
            if (combo >= config.minComboToShow)
            {
                ShowZoomWindow(zone, config, combo);
            }
        }
    }

    // ========================================
    // 窓の表示
    // ========================================

    private void ShowZoomWindow(CharacterInteractionZone.ZoneType zone, ZoomTargetConfig config, int combo)
    {
        if (_container == null)
        {
            Debug.LogWarning("[ZoomWindowManager] Container is null");
            return;
        }

        // 上限チェック
        if (_activeWindows.Count >= maxWindows)
        {
            CloseOldestWindow();
        }

        // 窓を取得または作成
        var window = GetOrCreateWindow();
        window.Show(config, zone, _container);
        window.OnTouched(combo);

        _activeWindows.Add(window);
        ArrangeWindows();

        Log($"Opened zoom window for zone: {zone}");

        OnActiveWindowCountChanged?.Invoke(_activeWindows.Count);
        OnZoomWindowOpened?.Invoke(zone);

        // メイン立ち絵のアニメーション変更
        PlayMainCharacterZoomAnimation(config.mainAnimWhileZoom);
    }

    // ========================================
    // 窓の管理
    // ========================================

    private ZoomWindowInstance GetOrCreateWindow()
    {
        ZoomWindowInstance window;

        if (_windowPool.Count > 0)
        {
            window = _windowPool.Dequeue();
        }
        else
        {
            var go = new GameObject($"ZoomWindow_{_windowIndex}");
            go.transform.SetParent(transform);
            window = go.AddComponent<ZoomWindowInstance>();
            window.Initialize(_windowIndex);
            _windowIndex++;
        }

        // イベント購読
        window.OnClosed += OnWindowClosed;
        window.OnStateChanged += OnWindowStateChanged;

        return window;
    }

    private void OnWindowClosed(ZoomWindowInstance window)
    {
        window.OnClosed -= OnWindowClosed;
        window.OnStateChanged -= OnWindowStateChanged;

        _activeWindows.Remove(window);
        _windowPool.Enqueue(window);

        ArrangeWindows();

        Log($"Window closed. Active count: {_activeWindows.Count}");

        OnActiveWindowCountChanged?.Invoke(_activeWindows.Count);

        if (_activeWindows.Count == 0)
        {
            OnAllWindowsClosed?.Invoke();
            RestoreMainCharacterAnimation();
        }
    }

    private void OnWindowStateChanged(ZoomWindowInstance window, ZoomAnimationState state)
    {
        Log($"Window {window.Zone} state changed to: {state}");
    }

    private ZoomWindowInstance FindWindowByZone(CharacterInteractionZone.ZoneType zone)
    {
        return _activeWindows.Find(w => w.Zone == zone);
    }

    private void CloseOldestWindow()
    {
        if (_activeWindows.Count == 0) return;

        // 最も古い（最初に追加された）窓を閉じる
        var oldest = _activeWindows[0];
        oldest.ForceClose();
    }

    public void CloseAllWindows()
    {
        foreach (var window in _activeWindows.ToArray())
        {
            window.ForceClose();
        }
    }

    // ========================================
    // 窓の配置
    // ========================================

    private void ArrangeWindows()
    {
        if (_activeWindows.Count == 0) return;

        // 複数窓を縦に並べる
        float startX = 0.65f;  // 右側
        float startY = 0.75f;  // 上から
        float stepY = 0.30f;   // 間隔

        for (int i = 0; i < _activeWindows.Count; i++)
        {
            var window = _activeWindows[i];
            float y = startY - (i * stepY);
            window.SetPosition(new Vector2(startX, y));
        }
    }

    // ========================================
    // メイン立ち絵連動
    // ========================================

    private string _originalMainAnimation = "idle";

    private void PlayMainCharacterZoomAnimation(string animName)
    {
        if (string.IsNullOrEmpty(animName)) return;

        var mainPresenter = OverlayCharacterPresenter.Instance;
        var layerController = mainPresenter?.LayerController as SpineLayerController;

        if (layerController != null)
        {
            // 現在のアニメーションを記憶（最初の窓が開いた時のみ）
            if (_activeWindows.Count == 1)
            {
                // TODO: 現在のアニメーション名を取得する方法がSpine導入後必要
                _originalMainAnimation = "idle";
            }

            layerController.PlayAnimation(animName, true);
            Log($"Main character animation: {animName}");
        }
    }

    private void RestoreMainCharacterAnimation()
    {
        var mainPresenter = OverlayCharacterPresenter.Instance;
        var layerController = mainPresenter?.LayerController as SpineLayerController;

        if (layerController != null)
        {
            layerController.PlayAnimation(_originalMainAnimation, true);
            Log($"Main character animation restored: {_originalMainAnimation}");
        }
    }

    // ========================================
    // レンズモード同期
    // ========================================

    private void SubscribeToMainPresenter()
    {
        var mainPresenter = OverlayCharacterPresenter.Instance;
        if (mainPresenter?.LayerController == null) return;

        mainPresenter.LayerController.OnPenetrateLevelChanged += OnMainPenetrateLevelChanged;
    }

    private void OnMainPenetrateLevelChanged(int level)
    {
        foreach (var window in _activeWindows)
        {
            window.OnMainPenetrateLevelChanged(level);
        }
    }

    // ========================================
    // ヘルパー
    // ========================================

    private void Log(string message)
    {
        if (debugLog)
        {
            Debug.Log($"[ZoomWindowManager] {message}");
        }
    }

    private void Cleanup()
    {
        CloseAllWindows();

        while (_windowPool.Count > 0)
        {
            var window = _windowPool.Dequeue();
            if (window != null)
            {
                Destroy(window.gameObject);
            }
        }

        var mainPresenter = OverlayCharacterPresenter.Instance;
        if (mainPresenter?.LayerController != null)
        {
            mainPresenter.LayerController.OnPenetrateLevelChanged -= OnMainPenetrateLevelChanged;
        }
    }
}
