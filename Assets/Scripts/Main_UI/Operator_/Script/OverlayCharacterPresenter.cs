using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// PSBキャラ表示のシンプル版
/// CharacterCanvasController付きプレハブを生成・管理
///
/// 旧版：RenderTexture + 専用カメラ（複雑）
/// 新版：Canvas付きプレハブを直接Instantiate（シンプル）
/// </summary>
public class OverlayCharacterPresenter : MonoBehaviour
{
    public static OverlayCharacterPresenter Instance { get; private set; }

    [Header("キャラクタープレハブ")]
    [Tooltip("CharacterCanvasController付きのプレハブ")]
    [SerializeField] private GameObject characterPrefab;

    [Header("表示設定")]
    [SerializeField] private int sortingOrder = 100;

    // 現在表示中のキャラクター
    private GameObject _currentInstance;
    private CharacterCanvasController _currentController;

    // 表示エリア
    private VisualElement _displayElement;

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
        if (Instance == this)
        {
            Instance = null;
        }
        Hide();
    }

    // ========================================
    // 表示制御
    // ========================================

    /// <summary>
    /// 表示エリアを設定
    /// </summary>
    public void SetDisplayArea(VisualElement element)
    {
        _displayElement = element;

        // 既に表示中なら位置を更新
        if (_currentController != null && _displayElement != null)
        {
            _currentController.FitToVisualElement(_displayElement, sortingOrder);
        }
    }

    /// <summary>
    /// 準備（互換性のため残す）
    /// </summary>
    public void EnsureCreated()
    {
        // 新版では不要（Show時に自動生成）
    }

    /// <summary>
    /// キャラクターを表示
    /// </summary>
    public void Show()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning("[Presenter] characterPrefab is NULL!");
            return;
        }

        // 既存インスタンスがあれば再利用
        if (_currentInstance != null)
        {
            _currentInstance.SetActive(true);
            UpdateDisplay();
            return;
        }

        // プレハブを生成
        _currentInstance = Instantiate(characterPrefab, transform);
        _currentController = _currentInstance.GetComponent<CharacterCanvasController>();

        if (_currentController == null)
        {
            Debug.LogError("[Presenter] CharacterCanvasController not found on prefab!");
            return;
        }

        // イベント購読
        _currentController.OnReady += OnCharacterReady;
        _currentController.OnScaleChanged += OnCharacterScaleChanged;

        // 表示エリアに配置
        UpdateDisplay();

        Debug.Log("[Presenter] Character shown (Simple Canvas method)");
    }

    /// <summary>
    /// 指定プレハブで表示
    /// </summary>
    public void Show(GameObject prefab)
    {
        if (prefab != null)
        {
            // 現在のインスタンスを破棄
            if (_currentInstance != null)
            {
                Destroy(_currentInstance);
                _currentInstance = null;
                _currentController = null;
            }

            characterPrefab = prefab;
        }
        Show();
    }

    /// <summary>
    /// 非表示
    /// </summary>
    public void Hide()
    {
        if (_currentInstance != null)
        {
            _currentInstance.SetActive(false);
        }
    }

    /// <summary>
    /// 破棄
    /// </summary>
    public void Destroy()
    {
        if (_currentInstance != null)
        {
            if (_currentController != null)
            {
                _currentController.OnReady -= OnCharacterReady;
                _currentController.OnScaleChanged -= OnCharacterScaleChanged;
            }

            Destroy(_currentInstance);
            _currentInstance = null;
            _currentController = null;
        }
    }

    // ========================================
    // 内部処理
    // ========================================

    private void UpdateDisplay()
    {
        if (_currentController == null) return;

        if (_displayElement != null)
        {
            _currentController.FitToVisualElement(_displayElement, sortingOrder);
        }
        else
        {
            // デフォルトは画面全体
            _currentController.FitToScreen(sortingOrder);
        }
    }

    private void OnCharacterReady()
    {
        Debug.Log("[Presenter] Character ready");
    }

    private void OnCharacterScaleChanged(float newScale)
    {
        Debug.Log($"[Presenter] Scale changed: {newScale}");
    }

    // ========================================
    // 外部アクセス
    // ========================================

    /// <summary>
    /// 現在のCharacterCanvasControllerを取得
    /// </summary>
    public CharacterCanvasController CurrentController => _currentController;

    /// <summary>
    /// 現在のインスタンスを取得
    /// </summary>
    public GameObject CurrentInstance => _currentInstance;

    /// <summary>
    /// 表示中かどうか
    /// </summary>
    public bool IsShowing => _currentInstance != null && _currentInstance.activeSelf;

    /// <summary>
    /// 描画順を変更
    /// </summary>
    public void SetSortingOrder(int order)
    {
        sortingOrder = order;
        _currentController?.SetSortingOrder(order);
    }

    /// <summary>
    /// レイアウトを更新（画面サイズ変更時など）
    /// </summary>
    public void RefreshLayout()
    {
        _currentController?.UpdateLayout();
    }
}
