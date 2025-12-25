using UnityEngine;

/// <summary>
/// キャラクター撮影用カメラの管理
/// RenderTexture生成、カメラ設定、自動サイズ調整を担当
/// </summary>
public class CharacterCameraController
{
    // 設定
    private readonly Vector2Int _renderTextureSize;
    private readonly float _defaultOrthoSize;
    private readonly Color _backgroundColor;
    private readonly Vector3 _spawnPosition;

    // 内部状態
    private Camera _camera;
    private RenderTexture _renderTexture;
    private GameObject _cameraRig;
    private float _currentOrthoSize;

    // プロパティ
    public Camera Camera => _camera;
    public RenderTexture RenderTexture => _renderTexture;
    public GameObject CameraRig => _cameraRig;
    public bool IsInitialized => _camera != null && _renderTexture != null;

    public CharacterCameraController(
        Vector2Int renderTextureSize,
        float defaultOrthoSize = 5f,
        Color? backgroundColor = null,
        Vector3? spawnPosition = null)
    {
        _renderTextureSize = renderTextureSize;
        _defaultOrthoSize = defaultOrthoSize;
        _currentOrthoSize = defaultOrthoSize;
        _backgroundColor = backgroundColor ?? new Color(0, 0, 0, 0);
        _spawnPosition = spawnPosition ?? new Vector3(1000f, 0f, 0f);
    }

    /// <summary>
    /// カメラリグとRenderTextureを初期化
    /// </summary>
    public void Initialize(Transform parent, Camera existingCamera = null, RenderTexture existingRT = null)
    {
        // カメラリグを作成
        _cameraRig = new GameObject("CharacterRenderRig");
        _cameraRig.hideFlags = HideFlags.HideAndDontSave;
        _cameraRig.transform.SetParent(parent);
        _cameraRig.transform.position = _spawnPosition;

        // RenderTexture
        _renderTexture = existingRT ?? CreateRenderTexture();

        // カメラ
        _camera = existingCamera ?? CreateCamera();
        _camera.targetTexture = _renderTexture;

        // 初期状態は非アクティブ
        _cameraRig.SetActive(false);

        Debug.Log($"[CameraController] Initialized - RT: {_renderTextureSize.x}x{_renderTextureSize.y}");
    }

    private RenderTexture CreateRenderTexture()
    {
        var rt = new RenderTexture(
            _renderTextureSize.x,
            _renderTextureSize.y,
            24,
            RenderTextureFormat.ARGB32
        );
        rt.name = "CharacterRenderTexture";
        rt.hideFlags = HideFlags.HideAndDontSave;
        rt.Create();
        return rt;
    }

    private Camera CreateCamera()
    {
        var camObj = new GameObject("CharacterCamera");
        camObj.hideFlags = HideFlags.HideAndDontSave;
        camObj.transform.SetParent(_cameraRig.transform);
        camObj.transform.localPosition = new Vector3(0, 0, -10f);
        camObj.transform.localRotation = Quaternion.identity;

        var cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = _defaultOrthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = _backgroundColor;
        cam.cullingMask = -1;
        cam.depth = -100;

        return cam;
    }

    /// <summary>
    /// カメラリグをアクティブ化
    /// </summary>
    public void SetActive(bool active)
    {
        if (_cameraRig != null)
        {
            _cameraRig.SetActive(active);
        }
    }

    /// <summary>
    /// キャラクターのサイズに合わせてカメラを自動調整
    /// </summary>
    public void AdjustToCharacter(GameObject characterInstance)
    {
        if (characterInstance == null || _camera == null) return;

        var renderers = characterInstance.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return;

        // 全SpriteRendererのBoundsを計算
        Bounds bounds = renderers[0].bounds;
        foreach (var sr in renderers)
        {
            bounds.Encapsulate(sr.bounds);
        }

        float boundsHeight = bounds.size.y;
        float boundsWidth = bounds.size.x;

        // RenderTextureのアスペクト比を考慮
        float rtAspect = (float)_renderTexture.width / _renderTexture.height;
        float boundsAspect = boundsWidth / boundsHeight;

        if (boundsAspect > rtAspect)
        {
            // 横長 → 幅に合わせる
            _camera.orthographicSize = (boundsWidth / rtAspect) / 2f * 1.1f;
        }
        else
        {
            // 縦長 → 高さに合わせる
            _camera.orthographicSize = boundsHeight / 2f * 1.1f;
        }

        // キャラをBoundsの中心に配置
        Vector3 offset = bounds.center - characterInstance.transform.position;
        characterInstance.transform.localPosition = -offset;

        _currentOrthoSize = _camera.orthographicSize;
        Debug.Log($"[CameraController] Adjusted: orthoSize={_currentOrthoSize:F2}, bounds={bounds.size}");
    }

    /// <summary>
    /// カメラサイズを手動設定
    /// </summary>
    public void SetOrthoSize(float size)
    {
        _currentOrthoSize = size;
        if (_camera != null)
        {
            _camera.orthographicSize = size;
        }
    }

    /// <summary>
    /// 現在のカメラサイズを取得
    /// </summary>
    public float GetOrthoSize() => _currentOrthoSize;

    /// <summary>
    /// UIクリック座標からワールド座標へのRaycast
    /// </summary>
    public Collider2D RaycastFromNormalizedPosition(Vector2 normalizedPos)
    {
        if (_camera == null) return null;

        // Y軸反転（UI座標系→ビューポート座標系）
        Vector3 viewportPos = new Vector3(normalizedPos.x, 1f - normalizedPos.y, 0f);
        Ray ray = _camera.ViewportPointToRay(viewportPos);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        return hit.collider;
    }

    /// <summary>
    /// クリーンアップ
    /// </summary>
    public void Dispose()
    {
        if (_cameraRig != null)
        {
            Object.Destroy(_cameraRig);
            _cameraRig = null;
        }

        if (_renderTexture != null && Application.isPlaying)
        {
            _renderTexture.Release();
            _renderTexture = null;
        }

        _camera = null;
    }
}
