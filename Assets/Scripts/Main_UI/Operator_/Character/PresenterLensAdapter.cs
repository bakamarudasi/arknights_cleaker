using UnityEngine;

/// <summary>
/// レンズマスク機能のアダプター
/// LensMaskControllerとILayerController（PSB/Spine両対応）を連携させる
/// </summary>
public class PresenterLensAdapter
{
    private LensMaskController _lensMaskController;
    private ILayerController _layerController;
    private Camera _camera;
    private Transform _parentTransform;

    public LensMaskController LensMask => _lensMaskController;
    public bool IsActive => _lensMaskController?.IsActive ?? false;

    /// <summary>
    /// 初期化（遅延初期化のため、カメラと親Transformを保持）
    /// </summary>
    public void Initialize(Camera camera, Transform parent)
    {
        _camera = camera;
        _parentTransform = parent;
    }

    /// <summary>
    /// レイヤーコントローラーを設定（PSB: CharacterLayerController, Spine: SpineLayerController）
    /// </summary>
    public void SetLayerController(ILayerController controller)
    {
        _layerController = controller;
    }

    /// <summary>
    /// レンズマスクを有効化
    /// </summary>
    public void Enable(int penetrateLevel)
    {
        EnsureLensMaskCreated();

        // レイヤーコントローラーにマスクモードを有効化
        _layerController?.EnableMaskMode(penetrateLevel);

        // マスクを表示
        _lensMaskController?.Show();

        Debug.Log($"[LensAdapter] Enabled - Level: {penetrateLevel}");
    }

    /// <summary>
    /// レンズマスクを無効化
    /// </summary>
    public void Disable()
    {
        // レイヤーコントローラーのマスクモードを無効化
        _layerController?.DisableMaskMode();

        // マスクを非表示
        _lensMaskController?.Hide();

        Debug.Log("[LensAdapter] Disabled");
    }

    /// <summary>
    /// レンズ位置を更新（正規化座標 0-1）
    /// </summary>
    public void UpdatePosition(Vector2 normalizedPos)
    {
        _lensMaskController?.UpdatePositionFromNormalized(normalizedPos);
    }

    /// <summary>
    /// レンズの形状を設定
    /// </summary>
    public void SetShape(LensMaskController.LensShape shape)
    {
        _lensMaskController?.SetShape(shape);
    }

    /// <summary>
    /// レンズのサイズを設定
    /// </summary>
    public void SetSize(float size)
    {
        _lensMaskController?.SetSize(size);
    }

    /// <summary>
    /// レンズマスクコントローラーを遅延生成
    /// </summary>
    private void EnsureLensMaskCreated()
    {
        if (_lensMaskController != null) return;
        if (_camera == null || _parentTransform == null)
        {
            Debug.LogWarning("[LensAdapter] Camera or parent not set");
            return;
        }

        var maskObj = new GameObject("LensMaskController");
        maskObj.transform.SetParent(_parentTransform);
        maskObj.transform.localPosition = Vector3.zero;

        _lensMaskController = maskObj.AddComponent<LensMaskController>();
        _lensMaskController.Initialize(_camera);

        Debug.Log("[LensAdapter] LensMask created");
    }

    /// <summary>
    /// クリーンアップ
    /// </summary>
    public void Dispose()
    {
        if (_lensMaskController != null)
        {
            Object.Destroy(_lensMaskController.gameObject);
            _lensMaskController = null;
        }
        _layerController = null;
        _camera = null;
        _parentTransform = null;
    }
}
