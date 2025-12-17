using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private float characterScale = 0.5f; // キャラを小さく
    [SerializeField] private Vector2 screenOffset = new Vector2(-100f, 0f); // 左に少しオフセット（右パネル分）

    [Header("Canvas設定")]
    [SerializeField] private int sortingOrder = 32767;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] private Vector2 displaySize = new Vector2(500, 700); // メインレイアウト内に収まるサイズ

    // 生成したオブジェクト
    private Camera characterCamera;
    private RenderTexture renderTexture;
    private Canvas overlayCanvas;
    private RawImage displayImage;
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

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

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

        var rectTransform = imageObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = screenOffset;
        rectTransform.sizeDelta = displaySize;

        displayImage = imageObj.AddComponent<RawImage>();
        displayImage.texture = renderTexture;
        displayImage.raycastTarget = false;

        Debug.Log($"[Presenter] DisplayImage created with size: {displaySize}");
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

        // displaySizeのアスペクト比でカメラサイズを調整
        float displayAspect = displaySize.x / displaySize.y;
        float boundsHalfHeight = bounds.extents.y;
        float boundsHalfWidth = bounds.extents.x;

        // 高さ基準と幅基準で必要なサイズを計算
        float sizeForHeight = boundsHalfHeight * 1.05f; // 5%マージン
        float sizeForWidth = (boundsHalfWidth / displayAspect) * 1.05f;

        characterCamera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

        Debug.Log($"[Presenter] Fit: Bounds={bounds.size}, OrthoSize={characterCamera.orthographicSize}");
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
