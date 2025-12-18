using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// キャラクタープレハブのCanvas自動調整コントローラー
/// PSBプレハブをScreenSpaceOverlayで表示し、指定エリアに自動フィット
///
/// 使い方：
/// 1. キャラクタープレハブのルートにこのスクリプトを配置
/// 2. OperatorUIControllerからSetDisplayArea()を呼び出し
/// 3. PSBのサイズに応じて自動的にCanvasがフィット
/// </summary>
public class CharacterCanvasController : MonoBehaviour
{
    [Header("必須参照")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private RectTransform contentRoot; // PSBを配置する親

    [Header("PSB設定")]
    [SerializeField] private GameObject psbRoot; // PSBプレハブのルート
    [SerializeField] private bool autoDetectBounds = true;

    [Header("フィット設定")]
    [SerializeField] private Vector2 padding = new Vector2(20f, 20f);
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 2.0f;

    [Header("基準解像度")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

    // 表示エリア（スクリーン座標）
    private Rect _displayArea;
    private bool _isInitialized;

    // PSBのBounds
    private Bounds _psbBounds;
    private float _currentScale = 1f;

    // ========================================
    // イベント（親への通知）
    // ========================================

    /// <summary>表示準備完了時</summary>
    public event Action OnReady;

    /// <summary>サイズ変更時 (newScale)</summary>
    public event Action<float> OnScaleChanged;

    /// <summary>クリック時（インタラクション用）</summary>
    public event Action OnClicked;

    /// <summary>Bounds更新時</summary>
    public event Action<Bounds> OnBoundsUpdated;

    // ========================================
    // プロパティ
    // ========================================

    public Canvas Canvas => canvas;
    public float CurrentScale => _currentScale;
    public Bounds PSBBounds => _psbBounds;
    public bool IsInitialized => _isInitialized;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvasScaler == null) canvasScaler = GetComponent<CanvasScaler>();

        SetupCanvas();
    }

    private void SetupCanvas()
    {
        if (canvas == null) return;

        // ScreenSpaceOverlay設定
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // UI Toolkitより上

        // CanvasScaler設定
        if (canvasScaler != null)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = referenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // 幅と高さの中間
        }
    }

    // ========================================
    // 表示エリア設定（OperatorUIControllerから呼び出し）
    // ========================================

    /// <summary>
    /// 表示エリアを設定（スクリーン座標）
    /// </summary>
    /// <param name="screenRect">スクリーン座標での表示エリア（左下原点）</param>
    /// <param name="sortingOrder">描画順</param>
    public void SetDisplayArea(Rect screenRect, int sortingOrder = 100)
    {
        _displayArea = screenRect;
        canvas.sortingOrder = sortingOrder;

        UpdateLayout();
        _isInitialized = true;
        OnReady?.Invoke();
    }

    /// <summary>
    /// UI ToolkitのVisualElementに合わせて配置
    /// </summary>
    public void FitToVisualElement(UnityEngine.UIElements.VisualElement element, int sortingOrder = 100)
    {
        if (element == null) return;

        // UITK座標（左上原点）→ スクリーン座標（左下原点）
        var worldBound = element.worldBound;
        var screenRect = new Rect(
            worldBound.x,
            Screen.height - worldBound.y - worldBound.height,
            worldBound.width,
            worldBound.height
        );

        SetDisplayArea(screenRect, sortingOrder);
    }

    /// <summary>
    /// 画面全体に表示
    /// </summary>
    public void FitToScreen(int sortingOrder = 100)
    {
        SetDisplayArea(new Rect(0, 0, Screen.width, Screen.height), sortingOrder);
    }

    // ========================================
    // レイアウト更新
    // ========================================

    /// <summary>
    /// レイアウトを更新（PSBサイズ変更時にも呼び出し可能）
    /// </summary>
    public void UpdateLayout()
    {
        if (contentRoot == null) return;

        // PSBのBoundsを取得
        UpdatePSBBounds();

        // パディングを適用した表示エリア
        var paddedArea = new Rect(
            _displayArea.x + padding.x,
            _displayArea.y + padding.y,
            _displayArea.width - padding.x * 2,
            _displayArea.height - padding.y * 2
        );

        // スケール計算（PSBが表示エリアに収まるように）
        _currentScale = CalculateFitScale(paddedArea);
        _currentScale = Mathf.Clamp(_currentScale, minScale, maxScale);

        // contentRootの位置とサイズを設定
        ApplyLayout(paddedArea);

        OnScaleChanged?.Invoke(_currentScale);
    }

    /// <summary>
    /// PSBのBoundsを更新
    /// </summary>
    private void UpdatePSBBounds()
    {
        if (psbRoot == null || !autoDetectBounds)
        {
            _psbBounds = new Bounds(Vector3.zero, Vector3.one);
            return;
        }

        // SpriteRendererからBoundsを計算
        var renderers = psbRoot.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0)
        {
            _psbBounds = new Bounds(Vector3.zero, Vector3.one);
            return;
        }

        _psbBounds = renderers[0].bounds;
        foreach (var sr in renderers)
        {
            _psbBounds.Encapsulate(sr.bounds);
        }

        OnBoundsUpdated?.Invoke(_psbBounds);
    }

    /// <summary>
    /// 表示エリアに収まるスケールを計算
    /// </summary>
    private float CalculateFitScale(Rect targetArea)
    {
        if (_psbBounds.size.x <= 0 || _psbBounds.size.y <= 0)
            return 1f;

        // CanvasScalerのスケールファクターを考慮
        float scaleFactor = GetCanvasScaleFactor();

        // ターゲットエリアをCanvas座標に変換
        float targetWidth = targetArea.width / scaleFactor;
        float targetHeight = targetArea.height / scaleFactor;

        // PSBサイズ（ワールド単位 → ピクセル換算、100px = 1unit として）
        float psbWidth = _psbBounds.size.x * 100f;
        float psbHeight = _psbBounds.size.y * 100f;

        // アスペクト比を維持してフィット
        float scaleX = targetWidth / psbWidth;
        float scaleY = targetHeight / psbHeight;

        return Mathf.Min(scaleX, scaleY);
    }

    /// <summary>
    /// レイアウトを適用
    /// </summary>
    private void ApplyLayout(Rect targetArea)
    {
        if (contentRoot == null) return;

        float scaleFactor = GetCanvasScaleFactor();

        // Canvas座標に変換
        Vector2 canvasPos = new Vector2(
            targetArea.x / scaleFactor,
            targetArea.y / scaleFactor
        );
        Vector2 canvasSize = new Vector2(
            targetArea.width / scaleFactor,
            targetArea.height / scaleFactor
        );

        // contentRootを表示エリアの中央に配置
        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.zero;
        contentRoot.pivot = new Vector2(0.5f, 0.5f);

        // 中央位置を計算
        Vector2 centerPos = new Vector2(
            canvasPos.x + canvasSize.x / 2,
            canvasPos.y + canvasSize.y / 2
        );
        contentRoot.anchoredPosition = centerPos;

        // PSBにスケールを適用
        if (psbRoot != null)
        {
            psbRoot.transform.localScale = Vector3.one * _currentScale;

            // PSBの中心を原点に調整
            Vector3 boundsOffset = _psbBounds.center - psbRoot.transform.position;
            psbRoot.transform.localPosition = -boundsOffset * _currentScale;
        }

        Debug.Log($"[CharacterCanvas] Applied: pos={centerPos}, scale={_currentScale}, bounds={_psbBounds.size}");
    }

    /// <summary>
    /// CanvasScalerのスケールファクターを取得
    /// </summary>
    private float GetCanvasScaleFactor()
    {
        if (canvasScaler == null) return 1f;

        float widthScale = Screen.width / referenceResolution.x;
        float heightScale = Screen.height / referenceResolution.y;
        return Mathf.Lerp(widthScale, heightScale, canvasScaler.matchWidthOrHeight);
    }

    // ========================================
    // 表示制御
    // ========================================

    /// <summary>
    /// 表示
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateLayout();
    }

    /// <summary>
    /// 非表示
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 描画順を変更
    /// </summary>
    public void SetSortingOrder(int order)
    {
        if (canvas != null)
        {
            canvas.sortingOrder = order;
        }
    }

    // ========================================
    // 画面サイズ変更対応
    // ========================================

    private Vector2 _lastScreenSize;

    private void Update()
    {
        // 画面サイズ変更を検知して再レイアウト
        Vector2 currentSize = new Vector2(Screen.width, Screen.height);
        if (_lastScreenSize != currentSize && _isInitialized)
        {
            _lastScreenSize = currentSize;
            UpdateLayout();
        }
    }

    // ========================================
    // PSB変更対応
    // ========================================

    /// <summary>
    /// PSBを差し替え
    /// </summary>
    public void SetPSB(GameObject newPSB)
    {
        // 古いPSBを削除
        if (psbRoot != null && psbRoot != newPSB)
        {
            Destroy(psbRoot);
        }

        psbRoot = newPSB;

        if (psbRoot != null)
        {
            psbRoot.transform.SetParent(contentRoot, false);
            psbRoot.transform.localPosition = Vector3.zero;
        }

        // 1フレーム待ってからレイアウト更新（Boundsが確定するため）
        StartCoroutine(UpdateLayoutNextFrame());
    }

    private System.Collections.IEnumerator UpdateLayoutNextFrame()
    {
        yield return null;
        UpdateLayout();
    }

    // ========================================
    // デバッグ
    // ========================================

    private void OnDrawGizmosSelected()
    {
        if (!_isInitialized) return;

        // 表示エリアを可視化
        Gizmos.color = new Color(0, 1, 0, 0.3f);

        // スクリーン座標をワールド座標に変換（簡易表示）
        Vector3 bottomLeft = Camera.main?.ScreenToWorldPoint(new Vector3(_displayArea.x, _displayArea.y, 10)) ?? Vector3.zero;
        Vector3 topRight = Camera.main?.ScreenToWorldPoint(new Vector3(_displayArea.xMax, _displayArea.yMax, 10)) ?? Vector3.zero;

        Gizmos.DrawWireCube(
            (bottomLeft + topRight) / 2,
            topRight - bottomLeft
        );
    }
}
