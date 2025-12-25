using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// PSBキャラ表示のファサード（RenderTexture方式）
/// 各サブシステム（カメラ、シーン、レンズ）を統合する
/// </summary>
public class OverlayCharacterPresenter : MonoBehaviour
{
    public static OverlayCharacterPresenter Instance { get; private set; }

    // ========================================
    // Inspector設定
    // ========================================

    [Header("=== RenderTexture設定 ===")]
    [SerializeField] private Camera characterCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(2048, 2048);

    [Header("=== キャラクター設定 ===")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Vector3 characterSpawnPosition = new Vector3(1000f, 0f, 0f);
    [SerializeField] private CharacterData characterData;

    [Header("=== カメラ設定 ===")]
    [SerializeField] private float cameraOrthoSize = 5f;
    [SerializeField] private Color cameraBackgroundColor = new Color(0, 0, 0, 0);

    // ========================================
    // サブシステム
    // ========================================

    private CharacterCameraController _cameraController;
    private CharacterSceneManager _sceneManager;
    private PresenterLensAdapter _lensAdapter;

    // ========================================
    // 内部状態
    // ========================================

    private GameObject _currentInstance;
    private CharacterLayerController _layerController;
    private VisualElement _displayElement;
    private bool _isShowing;
    private Action<float> _onUpdateCallback;

    // ========================================
    // イベント
    // ========================================

    public event Action OnCharacterReady;
#pragma warning disable CS0067
    public event Action<CharacterInteractionZone.ZoneType, int> OnZoneTouched;
#pragma warning restore CS0067
    public event Action<string> OnSceneChanged;
    public event Action<CharacterData> OnCharacterDataLoaded;

    // ========================================
    // プロパティ
    // ========================================

    public Camera CharacterCamera => _cameraController?.Camera ?? characterCamera;
    public RenderTexture RenderTexture => _cameraController?.RenderTexture ?? renderTexture;
    public GameObject CurrentInstance => _currentInstance;
    public bool IsShowing => _isShowing;

    public CharacterData CurrentCharacter => _sceneManager?.CurrentCharacter;
    public string CurrentSceneId => _sceneManager?.CurrentSceneId;
    public CharacterSceneData CurrentSceneData => _sceneManager?.CurrentScene;
    public CharacterLayerController LayerController => _layerController;
    public LensMaskController LensMask => _lensAdapter?.LensMask;

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

        InitializeSubsystems();

        if (characterData != null)
        {
            LoadCharacter(characterData, autoShow: false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cleanup();
    }

    private void InitializeSubsystems()
    {
        // カメラコントローラー
        _cameraController = new CharacterCameraController(
            renderTextureSize,
            cameraOrthoSize,
            cameraBackgroundColor,
            characterSpawnPosition
        );
        _cameraController.Initialize(transform, characterCamera, renderTexture);

        // シーンマネージャー
        _sceneManager = new CharacterSceneManager();
        _sceneManager.OnSceneChanged += sceneId => OnSceneChanged?.Invoke(sceneId);
        _sceneManager.OnCharacterLoaded += data => OnCharacterDataLoaded?.Invoke(data);

        // レンズアダプター
        _lensAdapter = new PresenterLensAdapter();
        _lensAdapter.Initialize(_cameraController.Camera, _cameraController.CameraRig.transform);
    }

    // ========================================
    // 表示エリア設定
    // ========================================

    public void SetDisplayArea(VisualElement element)
    {
        _displayElement = element;
        var rt = _cameraController.RenderTexture;

        if (_displayElement != null && rt != null)
        {
            _displayElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
            _displayElement.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _displayElement.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _displayElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            _displayElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        }
    }

    public void SetClickCallback(Action<Vector2> callback)
    {
        // 互換性のため残す（現在は使用していない）
    }

    // ========================================
    // 表示制御
    // ========================================

    public void EnsureCreated() { }

    public void Show()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("[Presenter] characterPrefab is NULL!");
            return;
        }

        if (_currentInstance != null)
        {
            _cameraController.SetActive(true);
            _currentInstance.SetActive(true);
            _isShowing = true;
            return;
        }

        // キャラクターを生成
        _currentInstance = Instantiate(characterPrefab, _cameraController.CameraRig.transform);
        _currentInstance.hideFlags = HideFlags.HideAndDontSave;
        _currentInstance.transform.localPosition = Vector3.zero;
        _currentInstance.transform.localRotation = Quaternion.identity;

        _cameraController.SetActive(true);
        _isShowing = true;

        SetupInteractionZones();

        // レイヤーコントローラーを取得してレンズアダプターに設定
        _layerController = _currentInstance.GetComponent<CharacterLayerController>();
        _lensAdapter.SetLayerController(_layerController);

        _cameraController.AdjustToCharacter(_currentInstance);

        Debug.Log("[Presenter] Character shown");
        OnCharacterReady?.Invoke();
    }

    public void Show(GameObject prefab)
    {
        if (prefab != null)
        {
            DestroyCurrentInstance();
            characterPrefab = prefab;
        }
        Show();
    }

    public void Hide()
    {
        _cameraController.SetActive(false);
        _isShowing = false;
    }

    public void DestroyCharacter()
    {
        DestroyCurrentInstance();
        _isShowing = false;
    }

    private void DestroyCurrentInstance()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
    }

    // ========================================
    // シーン管理（委譲）
    // ========================================

    public void LoadCharacter(CharacterData data, bool autoShow = true)
    {
        var prefab = _sceneManager.LoadCharacter(data);
        if (prefab != null)
        {
            characterPrefab = prefab;
            if (autoShow) Show();
        }
    }

    public bool SetScene(string sceneId)
    {
        var (prefab, recommendedSize, success) = _sceneManager.SetScene(sceneId);

        if (!success) return false;
        if (prefab == null) return true; // 切り替え不要

        DestroyCurrentInstance();
        characterPrefab = prefab;

        if (recommendedSize > 0)
        {
            _cameraController.SetOrthoSize(recommendedSize);
        }

        if (_isShowing) Show();
        return true;
    }

    public List<CharacterSceneData> GetAvailableScenes(int currentAffectionLevel = 999)
        => _sceneManager.GetAvailableScenes(currentAffectionLevel);

    public List<CharacterSceneData> GetAllScenes()
        => _sceneManager.GetAllScenes();

    public bool IsSceneUnlocked(string sceneId, int currentAffectionLevel = 999)
        => _sceneManager.IsSceneUnlocked(sceneId, currentAffectionLevel);

    // ========================================
    // カメラ制御（委譲）
    // ========================================

    public void SetCameraSize(float size) => _cameraController.SetOrthoSize(size);
    public void RefreshLayout() => _cameraController.AdjustToCharacter(_currentInstance);

    // ========================================
    // クリック・インタラクション
    // ========================================

    public Collider2D RaycastFromUI(Vector2 normalizedPos)
        => _cameraController.RaycastFromNormalizedPosition(normalizedPos);

    public CharacterInteractionZone GetInteractionZoneAt(Vector2 normalizedPos)
    {
        var collider = RaycastFromUI(normalizedPos);
        return collider?.GetComponent<CharacterInteractionZone>();
    }

    private void SetupInteractionZones()
    {
        foreach (var zone in GetInteractionZones())
        {
            if (zone.GetComponent<Collider2D>() == null)
            {
                var col = zone.gameObject.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
            }
        }
    }

    public CharacterInteractionZone[] GetInteractionZones()
    {
        if (_currentInstance == null) return Array.Empty<CharacterInteractionZone>();
        return _currentInstance.GetComponentsInChildren<CharacterInteractionZone>();
    }

    // ========================================
    // レイヤー制御（透視機能）
    // ========================================

    public void SetPenetrateLevel(int level)
        => _layerController?.SetPenetrateLevel(level);

    public void SetPenetrateLevelImmediate(int level)
        => _layerController?.SetPenetrateLevelImmediate(level);

    public void ResetPenetrateLevel() => SetPenetrateLevel(0);

    public int CurrentPenetrateLevel => _layerController?.CurrentPenetrateLevel ?? 0;

    // ========================================
    // レンズマスク制御（委譲）
    // ========================================

    public void EnableLensMask(int penetrateLevel) => _lensAdapter.Enable(penetrateLevel);
    public void DisableLensMask() => _lensAdapter.Disable();
    public void UpdateLensPosition(Vector2 normalizedPos) => _lensAdapter.UpdatePosition(normalizedPos);
    public void SetLensShape(LensMaskController.LensShape shape) => _lensAdapter.SetShape(shape);
    public void SetLensSize(float size) => _lensAdapter.SetSize(size);
    public bool IsLensMaskActive => _lensAdapter.IsActive;

    // ========================================
    // 互換性メソッド
    // ========================================

    public void SetSortingOrder(int order) { }

    public void SetUpdateCallback(Action<float> callback) => _onUpdateCallback = callback;
    public void ClearUpdateCallback() => _onUpdateCallback = null;

    private void Update()
    {
        if (_isShowing && _onUpdateCallback != null)
        {
            _onUpdateCallback.Invoke(Time.deltaTime);
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    private void Cleanup()
    {
        Hide();
        DestroyCurrentInstance();

        _lensAdapter?.Dispose();
        _cameraController?.Dispose();
        _sceneManager?.Clear();

        _lensAdapter = null;
        _cameraController = null;
        _sceneManager = null;
    }
}
