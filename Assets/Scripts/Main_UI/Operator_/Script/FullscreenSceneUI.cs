using UnityEngine.UIElements;

/// <summary>
/// フルスクリーンシーンUI（サイドパネルなし）
/// 温泉イベントなど、特殊なシーンで使用
/// </summary>
public class FullscreenSceneUI : ISceneUI
{
    private VisualElement root;
    private VisualElement sidePanel;
    private Button btnBack;
    private bool hideBackButton;

    private bool isVisible = false;

    public bool IsVisible => isVisible;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="hideBack">戻るボタンも非表示にするか</param>
    public void SetHideBackButton(bool hideBack)
    {
        hideBackButton = hideBack;
    }

    public void Initialize(VisualElement rootElement)
    {
        root = rootElement;
        sidePanel = root?.Q<VisualElement>("side-panel");
        btnBack = root?.Q<Button>("btn-back");
    }

    public void Show()
    {
        // サイドパネルを非表示
        if (sidePanel != null)
        {
            sidePanel.style.display = DisplayStyle.None;
        }

        // 戻るボタンの表示/非表示
        if (btnBack != null)
        {
            btnBack.style.display = hideBackButton ? DisplayStyle.None : DisplayStyle.Flex;

            // 戻るボタンを画面左上に移動（サイドパネルがないので）
            if (!hideBackButton)
            {
                btnBack.style.position = Position.Absolute;
                btnBack.style.top = 20;
                btnBack.style.left = 20;
                btnBack.style.width = 100;
            }
        }

        isVisible = true;
    }

    public void Hide()
    {
        // サイドパネルを再表示
        if (sidePanel != null)
        {
            sidePanel.style.display = DisplayStyle.Flex;
        }

        // 戻るボタンの位置をリセット
        if (btnBack != null)
        {
            btnBack.style.position = Position.Relative;
            btnBack.style.top = StyleKeyword.Auto;
            btnBack.style.left = StyleKeyword.Auto;
            btnBack.style.width = StyleKeyword.Auto;
            btnBack.style.display = DisplayStyle.Flex;
        }

        isVisible = false;
    }

    public void Dispose()
    {
        root = null;
        sidePanel = null;
        btnBack = null;
    }
}
