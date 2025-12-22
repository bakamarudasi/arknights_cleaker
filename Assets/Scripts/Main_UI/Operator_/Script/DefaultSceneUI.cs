using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// デフォルトのシーンUI（サイドパネル表示あり）
/// 通常のオペレーター画面で使用される標準UI
///
/// 使い方:
/// 1. 空のGameObjectにこのスクリプトをアタッチ
/// 2. プレハブ化
/// 3. OperatorUIControllerのdefaultSceneUIにアサイン
/// </summary>
public class DefaultSceneUI : BaseSceneUI
{
    // 既存UI要素への参照
    private VisualElement _sidePanel;
    private Button _btnBack;

    protected override void OnInitialize()
    {
        // 既存のUI要素を取得
        _sidePanel = QueryRoot<VisualElement>("side-panel");
        _btnBack = QueryRoot<Button>("btn-back");
    }

    protected override void OnShow()
    {
        // サイドパネルを表示
        if (_sidePanel != null)
        {
            _sidePanel.style.display = DisplayStyle.Flex;
        }

        // 戻るボタンを表示
        if (_btnBack != null)
        {
            _btnBack.style.display = DisplayStyle.Flex;
        }

        Debug.Log("[DefaultSceneUI] Shown");
    }

    protected override void OnHide()
    {
        // サイドパネルを非表示
        if (_sidePanel != null)
        {
            _sidePanel.style.display = DisplayStyle.None;
        }

        Debug.Log("[DefaultSceneUI] Hidden");
    }

    protected override void OnDispose()
    {
        _sidePanel = null;
        _btnBack = null;
    }
}
