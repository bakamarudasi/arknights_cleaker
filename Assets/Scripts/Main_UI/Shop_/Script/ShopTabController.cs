using System;
using UnityEngine.UIElements;

/// <summary>
/// ショップUIのタブ管理を担当するコントローラー
/// 単一責任: カテゴリタブの表示と切り替え
/// </summary>
public class ShopTabController
{
    private VisualElement tabContainer;
    private UpgradeData.UpgradeCategory currentCategory = UpgradeData.UpgradeCategory.Click;

    /// <summary>カテゴリが切り替わった時に発火</summary>
    public event Action<UpgradeData.UpgradeCategory> OnCategoryChanged;

    // カテゴリ定義
    private static readonly (UpgradeData.UpgradeCategory category, string label, string icon)[] Categories =
    {
        (UpgradeData.UpgradeCategory.Click, "クリック", "⚔"),
        (UpgradeData.UpgradeCategory.Income, "収入", "💰"),
        (UpgradeData.UpgradeCategory.Special, "特殊", "⭐")
    };

    /// <summary>
    /// タブコントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement container)
    {
        tabContainer = container;
        SetupTabs();
    }

    /// <summary>
    /// 現在のカテゴリを取得
    /// </summary>
    public UpgradeData.UpgradeCategory CurrentCategory => currentCategory;

    /// <summary>
    /// タブUIをセットアップ
    /// </summary>
    private void SetupTabs()
    {
        if (tabContainer == null) return;

        tabContainer.Clear();

        foreach (var (category, label, icon) in Categories)
        {
            var tab = CreateTabElement(category, label, icon);
            tabContainer.Add(tab);
        }

        UpdateTabStyles();
    }

    /// <summary>
    /// タブ要素を作成
    /// </summary>
    private Button CreateTabElement(UpgradeData.UpgradeCategory category, string label, string icon)
    {
        var tab = new Button();
        tab.AddToClassList("shop-tab");

        // アイコン
        var iconLabel = new Label { text = icon };
        iconLabel.AddToClassList("tab-icon");

        // テキスト
        var textLabel = new Label { text = label };
        textLabel.AddToClassList("tab-text");

        // グロー効果用の要素
        var glow = new VisualElement();
        glow.AddToClassList("tab-glow");
        glow.pickingMode = PickingMode.Ignore;

        tab.Add(iconLabel);
        tab.Add(textLabel);
        tab.Add(glow);

        tab.clicked += () => SwitchCategory(category);

        return tab;
    }

    /// <summary>
    /// カテゴリを切り替え
    /// </summary>
    public void SwitchCategory(UpgradeData.UpgradeCategory category)
    {
        if (currentCategory == category) return;

        currentCategory = category;
        UpdateTabStyles();
        OnCategoryChanged?.Invoke(category);
    }

    /// <summary>
    /// タブのスタイルを更新
    /// </summary>
    private void UpdateTabStyles()
    {
        if (tabContainer == null) return;

        int index = (int)currentCategory;
        for (int i = 0; i < tabContainer.childCount; i++)
        {
            var tab = tabContainer[i];
            if (i == index)
                tab.AddToClassList("tab-active");
            else
                tab.RemoveFromClassList("tab-active");
        }
    }

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        OnCategoryChanged = null;
    }
}
