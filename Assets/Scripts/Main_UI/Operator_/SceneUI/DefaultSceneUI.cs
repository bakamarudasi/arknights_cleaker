using UnityEngine.UIElements;

/// <summary>
/// デフォルトのシーンUI（サイドパネル表示あり）
/// 通常のオペレーター画面で使用される標準UI
/// </summary>
public class DefaultSceneUI : ISceneUI
{
    private VisualElement root;
    private VisualElement sidePanel;
    private Button btnBack;

    private bool isVisible = false;

    public bool IsVisible => isVisible;

    public void Initialize(VisualElement rootElement)
    {
        root = rootElement;
        sidePanel = root?.Q<VisualElement>("side-panel");
        btnBack = root?.Q<Button>("btn-back");
    }

    public void Show()
    {
        if (sidePanel != null)
        {
            sidePanel.style.display = DisplayStyle.Flex;
        }

        if (btnBack != null)
        {
            btnBack.style.display = DisplayStyle.Flex;
        }

        isVisible = true;
    }

    public void Hide()
    {
        if (sidePanel != null)
        {
            sidePanel.style.display = DisplayStyle.None;
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
