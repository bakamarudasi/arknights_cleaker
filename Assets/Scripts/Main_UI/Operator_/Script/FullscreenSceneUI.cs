using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// フルスクリーンシーンUI（サイドパネルなし）
/// 温泉イベントなど、特殊なシーンで使用
///
/// 使い方:
/// 1. 空のGameObjectにこのスクリプトをアタッチ
/// 2. プレハブ化
/// 3. OperatorUIControllerのfullscreenSceneUIにアサイン
/// </summary>
public class FullscreenSceneUI : BaseSceneUI
{
    [Header("=== フルスクリーン設定 ===")]
    [Tooltip("戻るボタンも非表示にするか")]
    [SerializeField] private bool hideBackButton = false;

    // 既存UI要素への参照
    private VisualElement _sidePanel;
    private Button _btnBack;

    // 元の戻るボタンスタイル（復元用）
    private StyleLength _originalBtnTop;
    private StyleLength _originalBtnLeft;
    private StyleLength _originalBtnWidth;
    private Position _originalBtnPosition;

    /// <summary>
    /// 戻るボタンの非表示設定
    /// </summary>
    public void SetHideBackButton(bool hide)
    {
        hideBackButton = hide;
    }

    protected override void OnInitialize()
    {
        _sidePanel = QueryRoot<VisualElement>("side-panel");
        _btnBack = QueryRoot<Button>("btn-back");

        // 元のスタイルを保存
        if (_btnBack != null)
        {
            _originalBtnPosition = _btnBack.style.position.value;
            _originalBtnTop = _btnBack.style.top;
            _originalBtnLeft = _btnBack.style.left;
            _originalBtnWidth = _btnBack.style.width;
        }
    }

    protected override void OnShow()
    {
        // サイドパネルを非表示
        if (_sidePanel != null)
        {
            _sidePanel.style.display = DisplayStyle.None;
        }

        // 戻るボタンの処理
        if (_btnBack != null)
        {
            if (hideBackButton)
            {
                _btnBack.style.display = DisplayStyle.None;
            }
            else
            {
                // 戻るボタンを画面左上に移動
                _btnBack.style.display = DisplayStyle.Flex;
                _btnBack.style.position = Position.Absolute;
                _btnBack.style.top = 20;
                _btnBack.style.left = 20;
                _btnBack.style.width = 100;
            }
        }

        Debug.Log($"[FullscreenSceneUI] Shown (hideBackButton: {hideBackButton})");
    }

    protected override void OnHide()
    {
        // サイドパネルを再表示
        if (_sidePanel != null)
        {
            _sidePanel.style.display = DisplayStyle.Flex;
        }

        // 戻るボタンの位置をリセット
        if (_btnBack != null)
        {
            _btnBack.style.position = _originalBtnPosition;
            _btnBack.style.top = _originalBtnTop;
            _btnBack.style.left = _originalBtnLeft;
            _btnBack.style.width = _originalBtnWidth;
            _btnBack.style.display = DisplayStyle.Flex;
        }

        Debug.Log("[FullscreenSceneUI] Hidden");
    }

    protected override void OnDispose()
    {
        _sidePanel = null;
        _btnBack = null;
    }
}
