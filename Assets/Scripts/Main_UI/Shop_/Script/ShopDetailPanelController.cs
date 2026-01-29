using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ショップUIの詳細パネル表示を担当するコントローラー
/// 単一責任: 選択アイテムの詳細表示と購入ボタン管理
/// </summary>
public class ShopDetailPanelController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private VisualElement detailPanel;
    private ScrollView detailScrollView;
    private VisualElement detailIcon;
    private Label detailName;
    private Label detailLevel;
    private Label detailCategory;
    private Label detailDesc;
    private Label detailCost;
    private VisualElement detailMaterials;
    private VisualElement effectPreviewContainer;

    // 一括購入ボタン
    private Button buyX1Btn;
    private Button buyX10Btn;
    private Button buyMaxBtn;

    // ========================================
    // 依存関係
    // ========================================

    private ShopService shopService;
    private ShopAnimationHelper animationHelper;

    // ========================================
    // 状態
    // ========================================

    private UpgradeData selectedUpgrade;

    // ========================================
    // イベント
    // ========================================

    /// <summary>購入ボタンがクリックされた時（数量を通知）</summary>
    public event Action<UpgradeData, int> OnBuyClicked;

    /// <summary>MAX購入ボタンがクリックされた時</summary>
    public event Action<UpgradeData> OnBuyMaxClicked;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// 詳細パネルコントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement rootElement, ShopService service, ShopAnimationHelper animation)
    {
        root = rootElement;
        shopService = service;
        animationHelper = animation;

        QueryElements();
        SetupBulkBuyButtons();
    }

    private void QueryElements()
    {
        detailPanel = root.Q<VisualElement>("detail-panel");
        detailScrollView = root.Q<ScrollView>("detail-scroll-view");
        detailIcon = root.Q<VisualElement>("detail-icon");
        detailName = root.Q<Label>("detail-name");
        detailLevel = root.Q<Label>("detail-level");
        detailCategory = root.Q<Label>("detail-category");
        detailDesc = root.Q<Label>("detail-desc");
        detailCost = root.Q<Label>("detail-cost");
        detailMaterials = root.Q<VisualElement>("detail-materials");
        effectPreviewContainer = root.Q<VisualElement>("effect-preview-container");

        buyX1Btn = root.Q<Button>("buy-x1-btn");
        buyX10Btn = root.Q<Button>("buy-x10-btn");
        buyMaxBtn = root.Q<Button>("buy-max-btn");
    }

    private void SetupBulkBuyButtons()
    {
        if (buyX1Btn != null)
        {
            buyX1Btn.AddToClassList("buy-x1");
            buyX1Btn.clicked += () => OnBuyClicked?.Invoke(selectedUpgrade, 1);
            buyX1Btn.AddManipulator(new HoldButtonManipulator(
                () => OnBuyClicked?.Invoke(selectedUpgrade, 1),
                ShopUIConstants.HOLD_BUTTON_INITIAL_DELAY_MS,
                ShopUIConstants.HOLD_BUTTON_X1_INTERVAL_MS));
        }

        if (buyX10Btn != null)
        {
            buyX10Btn.AddToClassList("buy-x10");
            buyX10Btn.clicked += () => OnBuyClicked?.Invoke(selectedUpgrade, 10);
            buyX10Btn.AddManipulator(new HoldButtonManipulator(
                () => OnBuyClicked?.Invoke(selectedUpgrade, 10),
                ShopUIConstants.HOLD_BUTTON_INITIAL_DELAY_MS,
                ShopUIConstants.HOLD_BUTTON_X10_INTERVAL_MS));
        }

        if (buyMaxBtn != null)
        {
            buyMaxBtn.clicked += () => OnBuyMaxClicked?.Invoke(selectedUpgrade);
        }
    }

    // ========================================
    // 選択・表示
    // ========================================

    /// <summary>
    /// アイテムを選択して詳細を表示
    /// </summary>
    public void SelectItem(UpgradeData upgrade)
    {
        selectedUpgrade = upgrade;

        if (upgrade != null)
        {
            RefreshDetailPanel();
            if (detailScrollView != null)
            {
                detailScrollView.scrollOffset = Vector2.zero;
            }
        }
        else
        {
            ClearDetailPanel();
        }
    }

    /// <summary>
    /// 選択中のアップグレードを取得
    /// </summary>
    public UpgradeData SelectedUpgrade => selectedUpgrade;

    /// <summary>
    /// 詳細パネルを更新
    /// </summary>
    public void RefreshDetailPanel()
    {
        if (selectedUpgrade == null) return;

        int level = shopService.GetUpgradeLevel(selectedUpgrade.id);
        double cost = shopService.GetSingleCost(selectedUpgrade);
        UpgradeState state = shopService.GetUpgradeState(selectedUpgrade);
        bool isMax = shopService.IsMaxLevel(selectedUpgrade);
        double money = shopService.GetMoney();
        bool canAfford = money >= cost;

        // アイコン表示
        UpdateIcon(isMax);

        // 名前表示
        if (detailName != null) detailName.text = selectedUpgrade.displayName;

        // レベル表示
        UpdateLevelDisplay(level, isMax);

        // カテゴリ表示
        if (detailCategory != null) detailCategory.text = selectedUpgrade.GetCategoryDisplayName().ToUpper();

        // 説明文（タイプライター）
        animationHelper?.StartTypewriterEffect(selectedUpgrade.description ?? "");

        // 効果プレビュー
        RefreshEffectPreview(level, isMax);

        // コスト表示
        UpdateCostDisplay(cost, isMax, canAfford);

        // 素材表示
        RefreshMaterialsDisplay();

        // 購入ボタン状態
        UpdateBulkBuyButtons(state, isMax, cost);
    }

    private void UpdateIcon(bool isMax)
    {
        if (detailIcon == null) return;

        if (selectedUpgrade.icon != null)
        {
            detailIcon.style.display = DisplayStyle.Flex;
            detailIcon.style.backgroundImage = new StyleBackground(selectedUpgrade.icon);
            detailIcon.RemoveFromClassList("highlight");
            if (!isMax) detailIcon.AddToClassList("highlight");
        }
        else
        {
            detailIcon.style.backgroundImage = null;
            detailIcon.style.display = DisplayStyle.None;
        }
    }

    private void UpdateLevelDisplay(int level, bool isMax)
    {
        if (detailLevel == null) return;

        bool isUnlimited = selectedUpgrade.maxLevel <= 0;
        if (isMax)
        {
            detailLevel.text = $"Lv.{level} (MAX)";
        }
        else if (isUnlimited)
        {
            detailLevel.text = $"Lv.{level} → Lv.{level + 1} (∞)";
        }
        else
        {
            int maxLv = selectedUpgrade.maxLevel;
            detailLevel.text = $"Lv.{level} → Lv.{level + 1} /{maxLv}";
        }
    }

    private void UpdateCostDisplay(double cost, bool isMax, bool canAfford)
    {
        if (detailCost == null) return;

        detailCost.text = isMax ? "---" : $"{cost:N0}";
        detailCost.RemoveFromClassList("not-enough");
        if (!isMax && !canAfford) detailCost.AddToClassList("not-enough");
    }

    // ========================================
    // 効果プレビュー
    // ========================================

    private void RefreshEffectPreview(int currentLevel, bool isMax)
    {
        if (effectPreviewContainer == null) return;
        effectPreviewContainer.Clear();

        if (selectedUpgrade == null) return;

        double currentEffect = selectedUpgrade.GetTotalEffectAtLevel(currentLevel);
        double nextEffect = selectedUpgrade.GetTotalEffectAtLevel(currentLevel + 1);

        // メイン効果行
        var effectRow = CreateEffectRow(currentEffect, nextEffect, isMax);
        effectPreviewContainer.Add(effectRow);

        // 増加量表示（MAXでない場合）
        if (!isMax)
        {
            var diffRow = CreateDiffRow(currentEffect, nextEffect);
            effectPreviewContainer.Add(diffRow);
        }
    }

    private VisualElement CreateEffectRow(double currentEffect, double nextEffect, bool isMax)
    {
        var effectRow = new VisualElement();
        effectRow.AddToClassList("effect-row");
        if (!isMax) effectRow.AddToClassList("effect-row-border");

        var effectLabel = new Label { text = GetEffectTypeName(selectedUpgrade.upgradeType) };
        effectLabel.AddToClassList("effect-label");

        var currentLabel = new Label { text = FormatEffectValue(currentEffect, selectedUpgrade.isPercentDisplay) };
        currentLabel.AddToClassList("effect-current");

        var arrowLabel = new Label { text = isMax ? "" : "▶" };
        arrowLabel.AddToClassList("effect-arrow");

        var nextLabel = new Label();
        nextLabel.AddToClassList("effect-next");
        if (isMax)
        {
            nextLabel.text = "MAX";
            nextLabel.AddToClassList("effect-max");
        }
        else
        {
            nextLabel.text = FormatEffectValue(nextEffect, selectedUpgrade.isPercentDisplay);
        }

        effectRow.Add(effectLabel);
        effectRow.Add(currentLabel);
        effectRow.Add(arrowLabel);
        effectRow.Add(nextLabel);

        return effectRow;
    }

    private VisualElement CreateDiffRow(double currentEffect, double nextEffect)
    {
        var diffRow = new VisualElement();
        diffRow.AddToClassList("effect-row");

        var diffLabel = new Label { text = "増加量" };
        diffLabel.AddToClassList("effect-label");

        var diffValue = new Label();
        diffValue.AddToClassList("effect-next");
        double diff = nextEffect - currentEffect;
        diffValue.text = $"+{FormatEffectValue(diff, selectedUpgrade.isPercentDisplay)}";

        diffRow.Add(diffLabel);
        diffRow.Add(new VisualElement { style = { flexGrow = 1 } }); // スペーサー
        diffRow.Add(diffValue);

        return diffRow;
    }

    private string GetEffectTypeName(UpgradeData.UpgradeType type)
    {
        return type switch
        {
            UpgradeData.UpgradeType.Click_FlatAdd => "クリック威力",
            UpgradeData.UpgradeType.Click_PercentAdd => "クリック倍率",
            UpgradeData.UpgradeType.Income_FlatAdd => "自動収入",
            UpgradeData.UpgradeType.Income_PercentAdd => "収入倍率",
            _ => "効果"
        };
    }

    private string FormatEffectValue(double value, bool isPercent)
    {
        if (isPercent)
        {
            return $"{value * 100:F1}%";
        }
        return value >= 1000 ? $"{value:N0}" : $"{value:F1}";
    }

    // ========================================
    // 素材表示
    // ========================================

    private void RefreshMaterialsDisplay()
    {
        if (detailMaterials == null) return;
        detailMaterials.Clear();

        if (selectedUpgrade?.requiredMaterials == null) return;

        foreach (var mat in selectedUpgrade.requiredMaterials)
        {
            if (mat.item == null) continue;

            int owned = shopService.GetItemCount(mat.item.id);
            bool enough = owned >= mat.amount;

            var matElement = CreateMaterialElement(mat, owned, enough);
            detailMaterials.Add(matElement);
        }
    }

    private VisualElement CreateMaterialElement(ItemCost mat, int owned, bool enough)
    {
        var matElement = new VisualElement();
        matElement.AddToClassList("material-item");

        var matIcon = new VisualElement();
        matIcon.AddToClassList("material-icon");
        if (mat.item.icon != null)
        {
            matIcon.style.backgroundImage = new StyleBackground(mat.item.icon);
        }

        var matCount = new Label();
        matCount.AddToClassList("material-count");
        matCount.text = $"{owned}/{mat.amount}";
        matCount.style.color = enough ? new Color(0.6f, 0.9f, 0.6f) : new Color(1f, 0.4f, 0.4f);

        matElement.Add(matIcon);
        matElement.Add(matCount);

        return matElement;
    }

    // ========================================
    // 購入ボタン
    // ========================================

    /// <summary>
    /// 購入ボタンの状態を更新（通貨変動時に呼び出し）
    /// </summary>
    public void RefreshBulkBuyButtons()
    {
        if (selectedUpgrade == null) return;

        bool isMax = shopService.IsMaxLevel(selectedUpgrade);
        double singleCost = shopService.GetSingleCost(selectedUpgrade);
        UpgradeState state = shopService.GetUpgradeState(selectedUpgrade);

        UpdateBulkBuyButtons(state, isMax, singleCost);
    }

    private void UpdateBulkBuyButtons(UpgradeState state, bool isMax, double singleCost)
    {
        double money = shopService.GetMoney();
        bool canBuyOne = state == UpgradeState.ReadyToUpgrade;

        // ×1 ボタン
        if (buyX1Btn != null)
        {
            buyX1Btn.SetEnabled(canBuyOne);
            buyX1Btn.text = isMax ? "-" : $"×1\n{singleCost:N0}";
        }

        // ×10 ボタン
        if (buyX10Btn != null)
        {
            int maxBuyCount = shopService.CalculateMaxBuyCount(selectedUpgrade, money);
            int buyCount = System.Math.Min(10, maxBuyCount);
            double totalCost = shopService.CalculateTotalCost(selectedUpgrade, buyCount);
            bool canBuy10 = canBuyOne && buyCount > 0;

            buyX10Btn.SetEnabled(canBuy10);
            buyX10Btn.text = isMax ? "-" : $"×{buyCount}\n{totalCost:N0}";
        }

        // MAX ボタン
        if (buyMaxBtn != null)
        {
            int maxCount = shopService.CalculateMaxBuyCount(selectedUpgrade, money);
            bool canBuyMax = canBuyOne && maxCount > 0;

            buyMaxBtn.SetEnabled(canBuyMax);
            if (isMax)
            {
                buyMaxBtn.text = "MAX";
            }
            else if (maxCount > 0)
            {
                double totalCost = shopService.CalculateTotalCost(selectedUpgrade, maxCount);
                buyMaxBtn.text = $"MAX(×{maxCount})\n{totalCost:N0}";
            }
            else
            {
                buyMaxBtn.text = "MAX\n---";
            }
        }
    }

    // ========================================
    // 演出
    // ========================================

    /// <summary>
    /// 購入成功時の演出を再生
    /// </summary>
    public void PlayPurchaseEffects()
    {
        if (animationHelper == null) return;

        animationHelper.PlayFlashEffect(detailPanel);
        animationHelper.PlayIconBounce(detailIcon);
        animationHelper.PlayEffectFlash(effectPreviewContainer);
    }

    // ========================================
    // クリア
    // ========================================

    /// <summary>
    /// 詳細パネルをクリア
    /// </summary>
    public void ClearDetailPanel()
    {
        selectedUpgrade = null;

        animationHelper?.StopTypewriter();
        animationHelper?.ResetTypewriterText();

        if (detailIcon != null)
        {
            detailIcon.style.backgroundImage = null;
            detailIcon.style.display = DisplayStyle.None;
            detailIcon.RemoveFromClassList("highlight");
        }

        if (detailName != null) detailName.text = "SELECT MODULE";
        if (detailLevel != null) detailLevel.text = "--";
        if (detailCategory != null) detailCategory.text = "";
        if (detailDesc != null) detailDesc.text = "Awaiting selection... // 待機中";

        effectPreviewContainer?.Clear();

        if (detailCost != null)
        {
            detailCost.text = "";
            detailCost.RemoveFromClassList("not-enough");
        }

        detailMaterials?.Clear();

        // ボタンを無効化
        if (buyX1Btn != null) { buyX1Btn.SetEnabled(false); buyX1Btn.text = "×1"; }
        if (buyX10Btn != null) { buyX10Btn.SetEnabled(false); buyX10Btn.text = "×10"; }
        if (buyMaxBtn != null) { buyMaxBtn.SetEnabled(false); buyMaxBtn.text = "MAX"; }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        OnBuyClicked = null;
        OnBuyMaxClicked = null;
        selectedUpgrade = null;
    }
}
