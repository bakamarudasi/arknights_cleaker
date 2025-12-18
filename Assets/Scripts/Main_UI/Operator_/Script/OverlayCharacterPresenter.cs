using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// PSBキャラ表示用のOverlay Canvas管理
/// RenderTextureを使用してUI Toolkitの上に表示
/// </summary>
public class OverlayCharacterPresenter : MonoBehaviour
{
    public static OverlayCharacterPresenter Instance { get; private set; }

    [Header("PSB Prefab設定")]
    [SerializeField] private GameObject psbPrefab;

    [Header("RenderTexture設定")]
    [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(1024, 1024);

    [Header("配置設定")]
    [SerializeField] private float characterScale = 0.5f;
    [SerializeField] private Vector2 padding = new Vector2(20f, 20f); // 表示エリア内のパディング

    [Header("Canvas設定")]
    [SerializeField] private int sortingOrder = 100; // UI Toolkitより上、でも最大値ではない
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);

    // 表示エリア（UI ToolkitのVisualElementから取得）
    private Rect _displayAreaScreen = Rect.zero;
    private CanvasScaler _canvasScaler;

    // 生成したオブジェクト
    private Camera characterCamera;
    private RenderTexture renderTexture;
    private Canvas overlayCanvas;
    private RawImage displayImage;
    private RectTransform displayRectTransform;
    private GameObject currentCharacter;
    private Transform characterRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 必要なオブジェクトを生成
    /// </summary>
    public void EnsureCreated()
    {
        if (overlayCanvas != null) return;

        CreateRenderTexture();
        CreateCharacterCamera();
        CreateOverlayCanvas();
        CreateDisplayImage();
    }

    /// <summary>
    /// 表示エリアを設定（UI ToolkitのVisualElementから）
    /// </summary>
    /// <param name="element">表示エリアとなるVisualElement</param>
    public void SetDisplayArea(VisualElement element)
    {
        if (element == null) return;

        // UI Toolkitのスクリーン座標を取得
        // worldBound: Y軸は上から下（上が0）
        Rect uitkBound = element.worldBound;

        // uGUIスクリーン座標に変換
        // uGUI: Y軸は下から上（下が0）
        _displayAreaScreen = new Rect(
            uitkBound.x,
            Screen.height - uitkBound.y - uitkBound.height,
            uitkBound.width,
            uitkBound.height
        );

        Debug.Log($"[Presenter] DisplayArea set: UITK={uitkBound}, Screen={_displayAreaScreen}");

        // 既に表示中なら位置を更新
        if (displayRectTransform != null)
        {
            UpdateDisplayPosition();
        }
    }

    /// <summary>
    /// RawImageの位置とサイズを表示エリアに合わせる
    /// </summary>
    private void UpdateDisplayPosition()
    {
        if (displayRectTransform == null || _displayAreaScreen == Rect.zero) return;

        // CanvasScalerのスケール係数を取得
        float scaleFactor = 1f;
        if (_canvasScaler != null)
        {
            // ScaleWithScreenSizeの場合のスケール計算
            float widthScale = Screen.width / referenceResolution.x;
            float heightScale = Screen.height / referenceResolution.y;
            scaleFactor = Mathf.Lerp(widthScale, heightScale, _canvasScaler.matchWidthOrHeight);
        }

        // パディングを適用した表示エリア
        Rect paddedArea = new Rect(
            _displayAreaScreen.x + padding.x,
            _displayAreaScreen.y + padding.y,
            _displayAreaScreen.width - padding.x * 2,
            _displayAreaScreen.height - padding.y * 2
        );

        // Canvas座標に変換（スケールファクターで割る）
        Vector2 canvasPos = new Vector2(
            paddedArea.x / scaleFactor,
            paddedArea.y / scaleFactor
        );
        Vector2 canvasSize = new Vector2(
            paddedArea.width / scaleFactor,
            paddedArea.height / scaleFactor
        );

        // RectTransformを設定（左下基準）
        displayRectTransform.anchorMin = Vector2.zero;
        displayRectTransform.anchorMax = Vector2.zero;
        displayRectTransform.pivot = Vector2.zero;
        displayRectTransform.anchoredPosition = canvasPos;
        displayRectTransform.sizeDelta = canvasSize;

        Debug.Log($"[Presenter] RawImage positioned: pos={canvasPos}, size={canvasSize}, scale={scaleFactor}");
    }

    /// <summary>
    /// キャラを表示
    /// </summary>
    public void Show()
    {
        if (psbPrefab == null)
        {
            Debug.LogWarning("[Presenter] PSB Prefab is NULL!");
            return;
        }

        EnsureCreated();

        // 既存キャラを破棄
        if (currentCharacter != null)
        {
            Destroy(currentCharacter);
        }

        // キャラルートを作成（まだなければ）
        if (characterRoot == null)
        {
            var rootObj = new GameObject("CharacterRoot");
            rootObj.transform.position = new Vector3(1000f, 0f, 0f);
            characterRoot = rootObj.transform;
            characterRoot.SetParent(transform);
        }

        // Prefab生成
        currentCharacter = Instantiate(psbPrefab, characterRoot, false);
        currentCharacter.transform.localPosition = Vector3.zero;
        currentCharacter.transform.localScale = Vector3.one * characterScale;

        // Animator再生開始
        var animator = currentCharacter.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play(0);
        }

        characterRoot.gameObject.SetActive(true);
        overlayCanvas.gameObject.SetActive(true);

        // 1フレーム待ってからセンタリング（スケールは維持）
        StartCoroutine(CenterCharacterNextFrame());

        Debug.Log($"[Presenter] Character shown via RenderTexture. Position: {currentCharacter.transform.position}");
    }

    /// <summary>
    /// 指定Prefabで表示（外部からPrefab指定する場合）
    /// </summary>
    public void Show(GameObject prefab)
    {
        if (prefab != null)
        {
            psbPrefab = prefab;
        }
        Show();
    }

    /// <summary>
    /// 非表示
    /// </summary>
    public void Hide()
    {
        if (currentCharacter != null)
        {
            Destroy(currentCharacter);
            currentCharacter = null;
        }

        if (characterRoot != null)
        {
            characterRoot.gameObject.SetActive(false);
        }

        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// RenderTexture生成
    /// </summary>
    private void CreateRenderTexture()
    {
        renderTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24, RenderTextureFormat.ARGB32);
        renderTexture.name = "CharacterRenderTexture";
        renderTexture.Create();
        Debug.Log($"[Presenter] RenderTexture created: {renderTextureSize.x}x{renderTextureSize.y}");
    }

    /// <summary>
    /// キャラ専用カメラ生成
    /// </summary>
    private void CreateCharacterCamera()
    {
        var camObj = new GameObject("CharacterCamera");
        camObj.transform.SetParent(transform);
        camObj.transform.position = new Vector3(1000f, 0f, -10f);

        characterCamera = camObj.AddComponent<Camera>();
        characterCamera.orthographic = true;
        characterCamera.orthographicSize = 5f;
        characterCamera.nearClipPlane = 0.1f;
        characterCamera.farClipPlane = 100f;
        characterCamera.clearFlags = CameraClearFlags.SolidColor;
        characterCamera.backgroundColor = new Color(0, 0, 0, 0);
        characterCamera.targetTexture = renderTexture;
        characterCamera.cullingMask = -1;
        characterCamera.depth = -100;

        Debug.Log($"[Presenter] CharacterCamera created at {camObj.transform.position}");
    }

    /// <summary>
    /// Overlay Canvas生成
    /// </summary>
    private void CreateOverlayCanvas()
    {
        var canvasObj = new GameObject("OverlayCharacterCanvas");
        canvasObj.transform.SetParent(transform);

        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = sortingOrder;

        _canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = referenceResolution;
        _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _canvasScaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log($"[Presenter] OverlayCanvas created with sortingOrder: {sortingOrder}");
    }

    /// <summary>
    /// RenderTexture表示用RawImage生成
    /// </summary>
    private void CreateDisplayImage()
    {
        var imageObj = new GameObject("CharacterDisplay");
        imageObj.transform.SetParent(overlayCanvas.transform, false);

        displayRectTransform = imageObj.AddComponent<RectTransform>();

        displayImage = imageObj.AddComponent<RawImage>();
        displayImage.texture = renderTexture;
        displayImage.raycastTarget = false;

        // 表示エリアが設定されていればそれに合わせる
        if (_displayAreaScreen != Rect.zero)
        {
            UpdateDisplayPosition();
        }
        else
        {
            // フォールバック：画面中央に配置
            displayRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            displayRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            displayRectTransform.pivot = new Vector2(0.5f, 0.5f);
            displayRectTransform.anchoredPosition = Vector2.zero;
            displayRectTransform.sizeDelta = new Vector2(500, 700);
        }

        Debug.Log($"[Presenter] DisplayImage created");
    }

    /// <summary>
    /// 1フレーム待ってからキャラをカメラ中央に配置（スケールは変更しない）
    /// </summary>
    private IEnumerator CenterCharacterNextFrame()
    {
        yield return null;

        if (currentCharacter == null || characterCamera == null) yield break;

        CenterCharacterToCamera();
    }

    /// <summary>
    /// キャラをカメラの中央に配置し、全体が収まるようにカメラサイズを調整
    /// </summary>
    private void CenterCharacterToCamera()
    {
        var renderers = currentCharacter.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return;

        // 全SpriteRendererのBoundsを統合
        Bounds bounds = renderers[0].bounds;
        foreach (var sr in renderers)
        {
            bounds.Encapsulate(sr.bounds);
        }

        // Boundsの中心をカメラの中心に合わせる
        Vector3 cameraPos = characterCamera.transform.position;
        Vector3 boundsCenter = bounds.center;
        Vector3 offset = boundsCenter - currentCharacter.transform.position;

        currentCharacter.transform.position = new Vector3(
            cameraPos.x - offset.x,
            cameraPos.y - offset.y,
            currentCharacter.transform.position.z
        );

        // 表示エリアのアスペクト比でカメラサイズを調整
        float displayAspect = 1f;
        if (_displayAreaScreen.width > 0 && _displayAreaScreen.height > 0)
        {
            displayAspect = _displayAreaScreen.width / _displayAreaScreen.height;
        }

        float boundsHalfHeight = bounds.extents.y;
        float boundsHalfWidth = bounds.extents.x;

        // 高さ基準と幅基準で必要なサイズを計算
        float sizeForHeight = boundsHalfHeight * 1.05f; // 5%マージン
        float sizeForWidth = (boundsHalfWidth / displayAspect) * 1.05f;

        characterCamera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

        Debug.Log($"[Presenter] Fit: Bounds={bounds.size}, Aspect={displayAspect:F2}, OrthoSize={characterCamera.orthographicSize}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
