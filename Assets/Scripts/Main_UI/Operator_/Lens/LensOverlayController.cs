using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// レンズオーバーレイを管理するコントローラー
/// マウス/タッチ追従で透視エリアを表示
/// </summary>
public class LensOverlayController
{
    public enum LensShape
    {
        Circle,     // 丸型
        Rectangle   // 四角型
    }

    // ========================================
    // UI要素
    // ========================================

    private VisualElement _root;
    private VisualElement _characterDisplay;
    private VisualElement _lensOverlay;
    private VisualElement _lensMask;
    private VisualElement _lensFrame;

    // ========================================
    // 設定
    // ========================================

    private LensShape _currentShape = LensShape.Circle;
    private float _lensSize = 200f;
    private bool _isActive = false;
    private int _penetrateLevel = 0;

    // ========================================
    // 状態
    // ========================================

    private Vector2 _currentPosition;
    private RenderTexture _xrayRenderTexture;

    // ========================================
    // イベント
    // ========================================

    public event Action<Vector2> OnLensPositionChanged;

    // ========================================
    // プロパティ
    // ========================================

    public bool IsActive => _isActive;
    public LensShape CurrentShape => _currentShape;
    public float LensSize => _lensSize;
    public int PenetrateLevel => _penetrateLevel;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root, VisualElement characterDisplay)
    {
        _root = root;
        _characterDisplay = characterDisplay;

        CreateLensOverlay();
        SetupEventHandlers();

        // 初期状態は非表示
        Hide();
    }

    private void CreateLensOverlay()
    {
        // レンズオーバーレイコンテナ
        _lensOverlay = new VisualElement();
        _lensOverlay.name = "lens-overlay";
        _lensOverlay.pickingMode = PickingMode.Ignore;
        _lensOverlay.style.position = Position.Absolute;
        _lensOverlay.style.left = 0;
        _lensOverlay.style.top = 0;
        _lensOverlay.style.right = 0;
        _lensOverlay.style.bottom = 0;

        // レンズマスク（透視表示エリア）
        _lensMask = new VisualElement();
        _lensMask.name = "lens-mask";
        _lensMask.pickingMode = PickingMode.Ignore;
        _lensMask.style.position = Position.Absolute;
        _lensMask.style.width = _lensSize;
        _lensMask.style.height = _lensSize;
        _lensMask.style.overflow = Overflow.Hidden;

        // レンズフレーム（枠線）
        _lensFrame = new VisualElement();
        _lensFrame.name = "lens-frame";
        _lensFrame.pickingMode = PickingMode.Ignore;
        _lensFrame.style.position = Position.Absolute;
        _lensFrame.style.width = _lensSize;
        _lensFrame.style.height = _lensSize;
        _lensFrame.style.borderTopWidth = 3;
        _lensFrame.style.borderBottomWidth = 3;
        _lensFrame.style.borderLeftWidth = 3;
        _lensFrame.style.borderRightWidth = 3;
        _lensFrame.style.borderTopColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        _lensFrame.style.borderBottomColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        _lensFrame.style.borderLeftColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        _lensFrame.style.borderRightColor = new Color(0.4f, 0.8f, 1f, 0.8f);

        _lensOverlay.Add(_lensMask);
        _lensOverlay.Add(_lensFrame);

        // characterDisplayの親に追加
        _characterDisplay.parent.Add(_lensOverlay);

        // 形状を適用
        ApplyShape(_currentShape);
    }

    private void SetupEventHandlers()
    {
        // マウス移動を追跡
        _characterDisplay.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _characterDisplay.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        _characterDisplay.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

        // タッチ対応
        _characterDisplay.RegisterCallback<PointerMoveEvent>(OnPointerMove);
    }

    // ========================================
    // イベントハンドラ
    // ========================================

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (!_isActive) return;
        UpdateLensPosition(evt.localMousePosition);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        if (_isActive)
        {
            _lensOverlay.style.display = DisplayStyle.Flex;
        }
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        if (_isActive)
        {
            // オーバーレイは表示したまま（好みで非表示にしてもOK）
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isActive) return;
        UpdateLensPosition(evt.localPosition);
    }

    private void UpdateLensPosition(Vector2 localPos)
    {
        _currentPosition = localPos;

        // レンズを中心に配置
        float halfSize = _lensSize / 2f;
        float x = localPos.x - halfSize;
        float y = localPos.y - halfSize;

        _lensMask.style.left = x;
        _lensMask.style.top = y;
        _lensFrame.style.left = x;
        _lensFrame.style.top = y;

        OnLensPositionChanged?.Invoke(localPos);
    }

    // ========================================
    // 表示制御
    // ========================================

    public void Show(int penetrateLevel)
    {
        _isActive = true;
        _penetrateLevel = penetrateLevel;
        _lensOverlay.style.display = DisplayStyle.Flex;

        // RenderTextureを設定
        UpdateXRayDisplay();

    }

    public void Hide()
    {
        _isActive = false;
        _penetrateLevel = 0;
        _lensOverlay.style.display = DisplayStyle.None;
    }

    public void SetRenderTexture(RenderTexture rt)
    {
        _xrayRenderTexture = rt;
        UpdateXRayDisplay();
    }

    private void UpdateXRayDisplay()
    {
        if (_xrayRenderTexture != null && _isActive)
        {
            _lensMask.style.backgroundImage = new StyleBackground(
                Background.FromRenderTexture(_xrayRenderTexture)
            );
            _lensMask.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _lensMask.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            _lensMask.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        }
    }

    // ========================================
    // 形状設定
    // ========================================

    public void SetShape(LensShape shape)
    {
        _currentShape = shape;
        ApplyShape(shape);
    }

    private void ApplyShape(LensShape shape)
    {
        switch (shape)
        {
            case LensShape.Circle:
                // 丸型
                _lensMask.style.borderTopLeftRadius = _lensSize / 2;
                _lensMask.style.borderTopRightRadius = _lensSize / 2;
                _lensMask.style.borderBottomLeftRadius = _lensSize / 2;
                _lensMask.style.borderBottomRightRadius = _lensSize / 2;

                _lensFrame.style.borderTopLeftRadius = _lensSize / 2;
                _lensFrame.style.borderTopRightRadius = _lensSize / 2;
                _lensFrame.style.borderBottomLeftRadius = _lensSize / 2;
                _lensFrame.style.borderBottomRightRadius = _lensSize / 2;
                break;

            case LensShape.Rectangle:
                // 四角型（角丸なし）
                _lensMask.style.borderTopLeftRadius = 8;
                _lensMask.style.borderTopRightRadius = 8;
                _lensMask.style.borderBottomLeftRadius = 8;
                _lensMask.style.borderBottomRightRadius = 8;

                _lensFrame.style.borderTopLeftRadius = 8;
                _lensFrame.style.borderTopRightRadius = 8;
                _lensFrame.style.borderBottomLeftRadius = 8;
                _lensFrame.style.borderBottomRightRadius = 8;
                break;
        }
    }

    // ========================================
    // サイズ設定
    // ========================================

    public void SetSize(float size)
    {
        _lensSize = size;

        _lensMask.style.width = size;
        _lensMask.style.height = size;
        _lensFrame.style.width = size;
        _lensFrame.style.height = size;

        // 形状を再適用（border-radiusの更新）
        ApplyShape(_currentShape);
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        if (_characterDisplay != null)
        {
            _characterDisplay.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            _characterDisplay.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            _characterDisplay.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
            _characterDisplay.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        if (_lensOverlay != null && _lensOverlay.parent != null)
        {
            _lensOverlay.parent.Remove(_lensOverlay);
        }

        OnLensPositionChanged = null;
    }
}
