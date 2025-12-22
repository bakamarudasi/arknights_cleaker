using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// シーンUIのベースクラス (MonoBehaviour)
/// 各シーン専用UIはこれを継承してプレハブ化する
///
/// 使い方:
/// 1. このクラスを継承したシーンUIを作成
/// 2. プレハブ化してInspectorでUXML/USSを設定
/// 3. OperatorUIControllerから参照
/// </summary>
public abstract class BaseSceneUI : MonoBehaviour, ISceneUI
{
    [Header("=== UI テンプレート ===")]
    [Tooltip("このシーン専用のUXML（オプション）")]
    [SerializeField] protected VisualTreeAsset uiTemplate;

    [Tooltip("このシーン専用のUSS（オプション）")]
    [SerializeField] protected StyleSheet styleSheet;

    // 参照
    protected VisualElement _root;           // OperatorUIのルート
    protected VisualElement _sceneContainer; // このシーンUIのコンテナ

    // 状態
    private bool _isVisible = false;
    public bool IsVisible => _isVisible;

    /// <summary>
    /// 初期化（OperatorUIControllerから呼ばれる）
    /// </summary>
    public virtual void Initialize(VisualElement root)
    {
        _root = root;

        // テンプレートがあれば生成（まだ追加しない）
        if (uiTemplate != null)
        {
            _sceneContainer = uiTemplate.Instantiate();

            if (styleSheet != null)
            {
                _sceneContainer.styleSheets.Add(styleSheet);
            }

            // 初期は非表示
            _sceneContainer.style.display = DisplayStyle.None;
        }

        OnInitialize();
    }

    /// <summary>
    /// 表示
    /// </summary>
    public virtual void Show()
    {
        // テンプレートがある場合、rootに追加
        if (_sceneContainer != null && _sceneContainer.parent == null)
        {
            _root?.Add(_sceneContainer);
        }

        if (_sceneContainer != null)
        {
            _sceneContainer.style.display = DisplayStyle.Flex;
        }

        _isVisible = true;
        OnShow();
    }

    /// <summary>
    /// 非表示
    /// </summary>
    public virtual void Hide()
    {
        if (_sceneContainer != null)
        {
            _sceneContainer.style.display = DisplayStyle.None;
        }

        _isVisible = false;
        OnHide();
    }

    /// <summary>
    /// 破棄
    /// </summary>
    public virtual void Dispose()
    {
        Hide();

        // DOMから削除
        _sceneContainer?.RemoveFromHierarchy();
        _sceneContainer = null;
        _root = null;

        OnDispose();
    }

    // ========================================
    // 継承先でオーバーライドするメソッド
    // ========================================

    /// <summary>
    /// 初期化時の追加処理（継承先で実装）
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// 表示時の追加処理（継承先で実装）
    /// </summary>
    protected virtual void OnShow() { }

    /// <summary>
    /// 非表示時の追加処理（継承先で実装）
    /// </summary>
    protected virtual void OnHide() { }

    /// <summary>
    /// 破棄時の追加処理（継承先で実装）
    /// </summary>
    protected virtual void OnDispose() { }

    // ========================================
    // ヘルパー
    // ========================================

    /// <summary>
    /// シーンコンテナ内の要素を取得
    /// </summary>
    protected T Query<T>(string name) where T : VisualElement
    {
        return _sceneContainer?.Q<T>(name);
    }

    /// <summary>
    /// ルート内の要素を取得
    /// </summary>
    protected T QueryRoot<T>(string name) where T : VisualElement
    {
        return _root?.Q<T>(name);
    }
}
