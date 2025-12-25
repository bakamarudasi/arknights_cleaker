using UnityEngine;
using System;

/// <summary>
/// レンズ用SpriteMaskを管理するコンポーネント
/// マウス/タッチ位置に追従してSpriteMaskを動かす
/// </summary>
public class LensMaskController : MonoBehaviour
{
    public enum LensShape
    {
        Circle,
        Rectangle
    }

    [Header("=== マスク設定 ===")]
    [Tooltip("レンズの形状")]
    [SerializeField]
    private LensShape lensShape = LensShape.Circle;

    [Tooltip("レンズのサイズ（ワールド単位）")]
    [SerializeField]
    private float lensSize = 2f;

    [Header("=== 参照 ===")]
    [Tooltip("追従対象のカメラ")]
    [SerializeField]
    private Camera targetCamera;

    // コンポーネント
    private SpriteMask _spriteMask;
    private SpriteRenderer _frameRenderer;
    private GameObject _maskObject;

    // 状態
    private bool _isActive = false;
    private Vector3 _targetPosition;

    // イベント
    public event Action<Vector3> OnLensPositionChanged;

    // プロパティ
    public bool IsActive => _isActive;
    public LensShape CurrentShape => lensShape;
    public float LensSize => lensSize;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// レンズマスクを初期化
    /// </summary>
    public void Initialize(Camera camera)
    {
        targetCamera = camera;
        CreateMaskObject();
        Hide();
    }

    private void CreateMaskObject()
    {
        // マスクオブジェクトを作成
        _maskObject = new GameObject("LensMask");
        _maskObject.transform.SetParent(transform);
        _maskObject.transform.localPosition = Vector3.zero;

        // SpriteMaskコンポーネントを追加
        _spriteMask = _maskObject.AddComponent<SpriteMask>();
        _spriteMask.alphaCutoff = 0.5f;

        // 重要: SpriteMaskを全てのソーティングレイヤーに適用
        _spriteMask.isCustomRangeActive = false;

        // フレーム表示用のSpriteRenderer（オプション）
        var frameObj = new GameObject("LensFrame");
        frameObj.transform.SetParent(_maskObject.transform);
        frameObj.transform.localPosition = new Vector3(0, 0, -0.01f); // わずかに手前
        _frameRenderer = frameObj.AddComponent<SpriteRenderer>();
        _frameRenderer.sortingOrder = 1000; // 最前面

        // 形状を適用
        ApplyShape(lensShape);

        Debug.Log("[LensMask] Created mask object");
    }

    // ========================================
    // 表示制御
    // ========================================

    /// <summary>
    /// レンズを表示
    /// </summary>
    public void Show()
    {
        _isActive = true;
        if (_maskObject != null)
        {
            _maskObject.SetActive(true);
        }
    }

    /// <summary>
    /// レンズを非表示
    /// </summary>
    public void Hide()
    {
        _isActive = false;
        if (_maskObject != null)
        {
            _maskObject.SetActive(false);
        }
    }

    // ========================================
    // 位置更新
    // ========================================

    /// <summary>
    /// スクリーン座標からワールド座標に変換してレンズを移動
    /// </summary>
    public void UpdatePositionFromScreen(Vector2 screenPos)
    {
        if (targetCamera == null || !_isActive) return;

        // スクリーン座標をワールド座標に変換
        Vector3 worldPos = targetCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        worldPos.z = 0; // 2Dなのでzは0

        SetPosition(worldPos);
    }

    /// <summary>
    /// 正規化座標（0-1）からワールド座標に変換してレンズを移動
    /// </summary>
    public void UpdatePositionFromNormalized(Vector2 normalizedPos)
    {
        if (targetCamera == null || !_isActive) return;

        // 正規化座標をビューポート座標に変換（Y軸反転）
        Vector3 viewportPos = new Vector3(normalizedPos.x, 1f - normalizedPos.y, 10f);

        // ビューポート座標をワールド座標に変換
        Vector3 worldPos = targetCamera.ViewportToWorldPoint(viewportPos);
        worldPos.z = 0;

        SetPosition(worldPos);
    }

    /// <summary>
    /// ワールド座標でレンズを移動
    /// </summary>
    public void SetPosition(Vector3 worldPos)
    {
        _targetPosition = worldPos;
        if (_maskObject != null)
        {
            _maskObject.transform.position = worldPos;
        }
        OnLensPositionChanged?.Invoke(worldPos);
    }

    // ========================================
    // 形状・サイズ設定
    // ========================================

    /// <summary>
    /// レンズの形状を設定
    /// </summary>
    public void SetShape(LensShape shape)
    {
        lensShape = shape;
        ApplyShape(shape);
    }

    /// <summary>
    /// レンズのサイズを設定
    /// </summary>
    public void SetSize(float size)
    {
        lensSize = size;
        ApplySize();
    }

    private void ApplyShape(LensShape shape)
    {
        if (_spriteMask == null) return;

        // 形状に応じたスプライトを設定
        Sprite maskSprite = CreateShapeSprite(shape);
        _spriteMask.sprite = maskSprite;

        // フレームにも同じ形状を設定（色付き）
        if (_frameRenderer != null)
        {
            _frameRenderer.sprite = maskSprite;
            _frameRenderer.color = new Color(0.4f, 0.8f, 1f, 0.5f); // 水色半透明
        }

        ApplySize();
    }

    private void ApplySize()
    {
        if (_maskObject == null) return;
        _maskObject.transform.localScale = new Vector3(lensSize, lensSize, 1f);
    }

    /// <summary>
    /// 形状に応じたスプライトを生成
    /// </summary>
    private Sprite CreateShapeSprite(LensShape shape)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = false;

                switch (shape)
                {
                    case LensShape.Circle:
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        inside = dist <= radius;
                        break;

                    case LensShape.Rectangle:
                        inside = x >= 4 && x < size - 4 && y >= 4 && y < size - 4;
                        break;
                }

                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
    }

    // ========================================
    // クリーンアップ
    // ========================================

    private void OnDestroy()
    {
        if (_maskObject != null)
        {
            Destroy(_maskObject);
        }
    }
}
