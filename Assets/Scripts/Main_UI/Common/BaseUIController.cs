using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// UIコントローラーの基底クラス
/// 共通の初期化・破棄パターンをテンプレートメソッドとして提供
/// </summary>
public abstract class BaseUIController : IViewController
{
    protected VisualElement root;
    protected string LogTag => $"[{GetType().Name}]";

    /// <summary>
    /// 初期化処理（外部から呼び出す）
    /// </summary>
    public void Initialize(VisualElement root)
    {
        if (root == null)
        {
            Debug.LogError($"{LogTag} Initialize: root VisualElement is null");
            return;
        }

        this.root = root;

        try
        {
            OnPreInitialize();
            QueryElements();

            if (!ValidateElements())
            {
                Debug.LogWarning($"{LogTag} Some UI elements are missing");
            }

            InitializeSubControllers();
            BindUIEvents();
            BindGameEvents();
            OnPostInitialize();
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LogTag} Initialize failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 初期化前の処理（オーバーライド可能）
    /// </summary>
    protected virtual void OnPreInitialize() { }

    /// <summary>
    /// UI要素の取得（派生クラスで実装）
    /// </summary>
    protected abstract void QueryElements();

    /// <summary>
    /// UI要素の検証（オーバーライド可能）
    /// </summary>
    protected virtual bool ValidateElements() => true;

    /// <summary>
    /// サブコントローラーの初期化（オーバーライド可能）
    /// </summary>
    protected virtual void InitializeSubControllers() { }

    /// <summary>
    /// UIイベントのバインド（ボタンクリック等）（オーバーライド可能）
    /// </summary>
    protected virtual void BindUIEvents() { }

    /// <summary>
    /// ゲームイベントのバインド（Manager系イベント）（オーバーライド可能）
    /// </summary>
    protected virtual void BindGameEvents() { }

    /// <summary>
    /// 初期化後の処理（オーバーライド可能）
    /// </summary>
    protected virtual void OnPostInitialize() { }

    /// <summary>
    /// 破棄処理（外部から呼び出す）
    /// </summary>
    public void Dispose()
    {
        try
        {
            OnPreDispose();
            UnbindGameEvents();
            UnbindUIEvents();
            DisposeSubControllers();
            OnPostDispose();
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LogTag} Dispose failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 破棄前の処理（オーバーライド可能）
    /// </summary>
    protected virtual void OnPreDispose() { }

    /// <summary>
    /// ゲームイベントの解除（オーバーライド可能）
    /// </summary>
    protected virtual void UnbindGameEvents() { }

    /// <summary>
    /// UIイベントの解除（オーバーライド可能）
    /// </summary>
    protected virtual void UnbindUIEvents() { }

    /// <summary>
    /// サブコントローラーの破棄（オーバーライド可能）
    /// </summary>
    protected virtual void DisposeSubControllers() { }

    /// <summary>
    /// 破棄後の処理（オーバーライド可能）
    /// </summary>
    protected virtual void OnPostDispose() { }

    // ========================================
    // ユーティリティメソッド
    // ========================================

    /// <summary>
    /// 要素を安全に取得
    /// </summary>
    protected T Query<T>(string name) where T : VisualElement
    {
        return root?.Q<T>(name);
    }

    /// <summary>
    /// 要素を安全に取得（クラス名で検索）
    /// </summary>
    protected T QueryByClass<T>(string className) where T : VisualElement
    {
        return root?.Q<T>(className: className);
    }

    /// <summary>
    /// ボタンにクリックイベントを安全に登録
    /// </summary>
    protected void RegisterButtonClick(Button button, Action action)
    {
        if (button != null && action != null)
        {
            button.clicked += action;
        }
    }

    /// <summary>
    /// ボタンからクリックイベントを安全に解除
    /// </summary>
    protected void UnregisterButtonClick(Button button, Action action)
    {
        if (button != null && action != null)
        {
            button.clicked -= action;
        }
    }

    /// <summary>
    /// 遅延実行
    /// </summary>
    protected void ExecuteLater(Action action, long delayMs)
    {
        root?.schedule.Execute(action).ExecuteLater(delayMs);
    }

    /// <summary>
    /// 定期実行
    /// </summary>
    protected IVisualElementScheduledItem ExecuteEvery(Action action, long intervalMs)
    {
        return root?.schedule.Execute(action).Every(intervalMs);
    }

    /// <summary>
    /// 数値をフォーマット
    /// </summary>
    protected static string FormatNumber(double value)
    {
        if (value >= 1_000_000_000_000) return $"{value / 1_000_000_000_000:F2}T";
        if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F2}B";
        if (value >= 1_000_000) return $"{value / 1_000_000:F2}M";
        if (value >= 1_000) return $"{value / 1_000:F2}K";
        return value.ToString("N0");
    }
}
