using UnityEngine.UIElements;

/// <summary>
/// シーン別UIの共通インターフェース
/// 各ポーズ/シーンに応じたUI表示を制御する
///
/// 実装例:
/// - DefaultSceneUI: 通常のサイドパネル表示
/// - FullscreenSceneUI: サイドパネルなしのフルスクリーン
/// - EventSceneUI: イベント専用UI
/// </summary>
public interface ISceneUI
{
    /// <summary>
    /// シーンUIを表示
    /// </summary>
    void Show();

    /// <summary>
    /// シーンUIを非表示
    /// </summary>
    void Hide();

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="root">OperatorUIのルート要素</param>
    void Initialize(VisualElement root);

    /// <summary>
    /// 破棄
    /// </summary>
    void Dispose();

    /// <summary>
    /// 表示中かどうか
    /// </summary>
    bool IsVisible { get; }
}
