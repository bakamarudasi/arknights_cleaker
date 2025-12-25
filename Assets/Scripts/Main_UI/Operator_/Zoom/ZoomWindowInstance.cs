using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

/// <summary>
/// ズーム窓1つ分のインスタンス
/// Camera、RenderTexture、UI要素、Spineプレハブを管理
/// </summary>
public class ZoomWindowInstance : MonoBehaviour
{
    // ========================================
    // 状態
    // ========================================

    public CharacterInteractionZone.ZoneType Zone { get; private set; }
    public ZoomAnimationState State { get; private set; } = ZoomAnimationState.Idle;
    public bool IsActive => State != ZoomAnimationState.Idle;
    public float LastTouchTime { get; private set; }
    public int ComboCount { get; private set; }

    // ========================================
    // コンポーネント
    // ========================================

    private Camera _camera;
    private RenderTexture _renderTexture;
    private GameObject _prefabInstance;
    private SpineLayerController _layerController;
    private VisualElement _uiElement;
    private VisualElement _viewport;
    private ZoomTargetConfig _config;

    // ========================================
    // 設定
    // ========================================

    private static readonly Vector2Int RT_SIZE = new Vector2Int(1024, 1024);
    private static readonly int ZOOM_LAYER = 20; // ズーム専用レイヤー

    // ========================================
    // イベント
    // ========================================

    public event Action<ZoomWindowInstance> OnClosed;
    public event Action<ZoomWindowInstance, ZoomAnimationState> OnStateChanged;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(int index)
    {
        CreateCamera(index);
        CreateRenderTexture();
        CreateUIElement();

        // レイヤー設定
        gameObject.layer = ZOOM_LAYER;
    }

    private void CreateCamera(int index)
    {
        var cameraGO = new GameObject($"ZoomCamera_{index}");
        cameraGO.transform.SetParent(transform);
        cameraGO.transform.localPosition = new Vector3(0, 0, -10);

        _camera = cameraGO.AddComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = 2f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0, 0, 0, 0);
        _camera.cullingMask = 1 << ZOOM_LAYER;
        _camera.depth = 10 + index; // メインカメラより上
        _camera.enabled = false;
    }

    private void CreateRenderTexture()
    {
        _renderTexture = new RenderTexture(RT_SIZE.x, RT_SIZE.y, 0, RenderTextureFormat.ARGB32);
        _renderTexture.antiAliasing = 2;
        _renderTexture.Create();

        _camera.targetTexture = _renderTexture;
    }

    private void CreateUIElement()
    {
        _uiElement = new VisualElement();
        _uiElement.name = $"zoom-window-{GetInstanceID()}";
        _uiElement.AddToClassList("zoom-window");

        _viewport = new VisualElement();
        _viewport.name = "zoom-viewport";
        _viewport.AddToClassList("zoom-viewport");
        _uiElement.Add(_viewport);

        // 初期状態は非表示
        _uiElement.style.opacity = 0;
        _uiElement.style.display = DisplayStyle.None;
    }

    // ========================================
    // 表示制御
    // ========================================

    public void Show(ZoomTargetConfig config, CharacterInteractionZone.ZoneType zone, VisualElement container)
    {
        _config = config;
        Zone = zone;
        LastTouchTime = Time.time;
        ComboCount = 1;

        // プレハブ生成
        LoadPrefab(config.zoomPrefab);

        // カメラサイズ設定
        _camera.orthographicSize = config.cameraSize;
        _camera.enabled = true;

        // UI設定
        SetWindowSize(config.windowSizeRatio);
        _viewport.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_renderTexture));

        // コンテナに追加
        if (_uiElement.parent != container)
        {
            container.Add(_uiElement);
        }

        // フェードイン
        StartCoroutine(FadeIn(config.fadeInDuration));

        // アニメーション開始
        TransitionTo(ZoomAnimationState.Enter);

        // レンズモード同期
        SyncWithMainPresenter();
    }

    public void Hide()
    {
        if (State == ZoomAnimationState.Idle) return;
        TransitionTo(ZoomAnimationState.Exit);
    }

    public void ForceClose()
    {
        StartCoroutine(FadeOutAndClose(_config?.fadeOutDuration ?? 0.2f));
    }

    // ========================================
    // タッチ更新
    // ========================================

    public void OnTouched(int combo)
    {
        LastTouchTime = Time.time;
        ComboCount = combo;

        if (_config == null) return;

        // クライマックス判定
        bool shouldClimax =
            combo >= _config.comboForClimax ||
            (ExcitementManager.Instance?.CurrentExcitement ?? 0) >= _config.excitementForClimax;

        if (shouldClimax && State == ZoomAnimationState.Loop)
        {
            TransitionTo(ZoomAnimationState.Climax);
        }

        // アニメーション速度調整
        float speed = 1f + (combo * 0.1f);
        SetAnimationSpeed(Mathf.Min(speed, 2f));
    }

    // ========================================
    // アニメーション状態遷移
    // ========================================

    private void TransitionTo(ZoomAnimationState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(this, newState);

        if (_config == null) return;

        switch (newState)
        {
            case ZoomAnimationState.Enter:
                PlayAnimation(_config.animEnter, false, () => TransitionTo(ZoomAnimationState.Loop));
                break;

            case ZoomAnimationState.Loop:
                PlayAnimation(_config.animLoop, true);
                break;

            case ZoomAnimationState.Climax:
                PlayAnimation(_config.animClimax, false, () => TransitionTo(ZoomAnimationState.Exit));
                break;

            case ZoomAnimationState.Exit:
                PlayAnimation(_config.animExit, false, () => StartCoroutine(FadeOutAndClose(_config.fadeOutDuration)));
                break;
        }
    }

    // ========================================
    // プレハブ管理
    // ========================================

    private void LoadPrefab(GameObject prefab)
    {
        if (prefab == null) return;

        // 既存のインスタンスを破棄
        if (_prefabInstance != null)
        {
            Destroy(_prefabInstance);
        }

        // 新しいインスタンスを生成
        _prefabInstance = Instantiate(prefab, transform);
        _prefabInstance.transform.localPosition = Vector3.zero;
        _prefabInstance.transform.localRotation = Quaternion.identity;

        // レイヤー設定（子も含めて）
        SetLayerRecursive(_prefabInstance, ZOOM_LAYER);

        // SpineLayerController取得
        _layerController = _prefabInstance.GetComponent<SpineLayerController>();
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    // ========================================
    // アニメーション
    // ========================================

    private void PlayAnimation(string animName, bool loop, Action onComplete = null)
    {
        if (_layerController != null)
        {
            _layerController.PlayAnimation(animName, loop);
        }

        // spine-unity導入後にonCompleteを正しく実装
        // 仮実装：ループでない場合は推定時間後にコールバック
        if (!loop && onComplete != null)
        {
            StartCoroutine(InvokeAfterDelay(1.5f, onComplete));
        }
    }

    private void SetAnimationSpeed(float speed)
    {
        // spine-unity導入後に実装
        // _animState.TimeScale = speed;
    }

    private IEnumerator InvokeAfterDelay(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    // ========================================
    // レンズモード同期
    // ========================================

    private void SyncWithMainPresenter()
    {
        var main = OverlayCharacterPresenter.Instance;
        if (main?.LayerController == null || _layerController == null) return;

        int level = main.LayerController.CurrentPenetrateLevel;
        _layerController.SetPenetrateLevel(level);

        if (main.LayerController.IsMaskModeActive)
        {
            _layerController.EnableMaskMode(level);
        }
    }

    public void OnMainPenetrateLevelChanged(int level)
    {
        _layerController?.SetPenetrateLevel(level);
    }

    public void OnMainMaskModeChanged(bool active, int level)
    {
        if (active)
        {
            _layerController?.EnableMaskMode(level);
        }
        else
        {
            _layerController?.DisableMaskMode();
        }
    }

    // ========================================
    // UI
    // ========================================

    public void SetWindowSize(Vector2 sizeRatio)
    {
        _uiElement.style.width = new Length(sizeRatio.x * 100, LengthUnit.Percent);
        _uiElement.style.height = new Length(sizeRatio.y * 100, LengthUnit.Percent);
    }

    public void SetPosition(Vector2 anchor)
    {
        // アンカーからposition計算（右上基準）
        float rightPercent = (1f - anchor.x) * 100f;
        float topPercent = (1f - anchor.y) * 100f;

        _uiElement.style.right = new Length(rightPercent, LengthUnit.Percent);
        _uiElement.style.top = new Length(topPercent, LengthUnit.Percent);
    }

    public VisualElement UIElement => _uiElement;

    // ========================================
    // フェードアニメーション
    // ========================================

    private IEnumerator FadeIn(float duration)
    {
        _uiElement.style.display = DisplayStyle.Flex;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _uiElement.style.opacity = t;
            yield return null;
        }

        _uiElement.style.opacity = 1f;
    }

    private IEnumerator FadeOutAndClose(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / duration);
            _uiElement.style.opacity = t;
            yield return null;
        }

        _uiElement.style.opacity = 0f;
        _uiElement.style.display = DisplayStyle.None;

        // リセット
        State = ZoomAnimationState.Idle;
        _camera.enabled = false;

        if (_prefabInstance != null)
        {
            Destroy(_prefabInstance);
            _prefabInstance = null;
        }

        OnClosed?.Invoke(this);
    }

    // ========================================
    // タイムアウトチェック
    // ========================================

    private void Update()
    {
        if (State == ZoomAnimationState.Idle) return;
        if (State == ZoomAnimationState.Exit) return;
        if (_config == null) return;

        // タイムアウト
        if (Time.time - LastTouchTime > _config.timeout)
        {
            TransitionTo(ZoomAnimationState.Exit);
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }

        if (_prefabInstance != null)
        {
            Destroy(_prefabInstance);
        }

        if (_uiElement?.parent != null)
        {
            _uiElement.RemoveFromHierarchy();
        }
    }
}
