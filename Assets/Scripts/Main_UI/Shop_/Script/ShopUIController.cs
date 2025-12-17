using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections; // コルーチン用に追加したが、今回はScheduleを使用

/// <summary>
/// ショップ/強化画面のロジック
/// </summary>
public class ShopUIController : IViewController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private Label moneyLabel;
    private Label certLabel;
    private VisualElement tabContainer;
    private ListView upgradeListView;
    private ScrollView detailScrollView;
    private VisualElement detailIcon;
    private Label detailName;
    private Label detailLevel;
    private Label detailCategory;
    private Label detailDesc;
    private Label detailCost;
    private VisualElement detailMaterials;

    // 一括購入ボタン
    private Button buyX1Btn;
    private Button buyX10Btn;
    private Button buyMaxBtn;

    // パネル全体（フラッシュ演出用）
    private VisualElement detailPanel;

    // 次レベルプレビュー用
    private VisualElement effectPreviewContainer;

    // リスト件数表示
    private Label listCountLabel;

    // ========================================
    // 演出用変数
    // ========================================
    
    // 文字送り
    private IVisualElementScheduledItem typewriterTimer;
    private string targetDescriptionText;
    private int currentCharIndex;

    // 通貨ドラムロール
    private IVisualElementScheduledItem currencyTimer;
    private double currentDisplayMoney = -1;
    private double targetMoney = 0;
    private double currentDisplayCert = -1;
    private double targetCert = 0;

    // ========================================
    // データ
    // ========================================

    private UpgradeDatabase database;
    private List<UpgradeData> currentList = new();
    private UpgradeData.UpgradeCategory currentCategory = UpgradeData.UpgradeCategory.Click;
    private UpgradeData selectedUpgrade;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root, UpgradeDatabase database)
    {
        this.root = root;
        this.database = database;

        QueryElements();
        SetupTabs();
        SetupListView();
        BindEvents();

        // 初期表示のために現在の値をセット（アニメなしで即反映）
        var gc = GameController.Instance;
        currentDisplayMoney = gc.GetMoney();
        targetMoney = currentDisplayMoney;
        currentDisplayCert = gc.GetCertificates();
        targetCert = currentDisplayCert;
        
        UpdateCurrencyLabels(); // ラベル直接更新
        
        SwitchCategory(UpgradeData.UpgradeCategory.Click);
        ClearDetailPanel();

        // 通貨アニメーションループ開始 (30fps程度で更新)
        currencyTimer = root.schedule.Execute(OnCurrencyTick).Every(30);
    }

    private void QueryElements()
    {
        moneyLabel = root.Q<Label>("money-label");
        certLabel = root.Q<Label>("cert-label");
        tabContainer = root.Q<VisualElement>("tab-container");
        upgradeListView = root.Q<ListView>("upgrade-list");
        
        // 詳細パネルの親要素を取得（フラッシュ用）
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
        listCountLabel = root.Q<Label>("list-count");

        // 一括購入ボタン
        buyX1Btn = root.Q<Button>("buy-x1-btn");
        buyX10Btn = root.Q<Button>("buy-x10-btn");
        buyMaxBtn = root.Q<Button>("buy-max-btn");

        SetupBulkBuyButtons();
    }

    private void SetupBulkBuyButtons()
    {
        if (buyX1Btn != null)
        {
            buyX1Btn.AddToClassList("buy-x1");
            buyX1Btn.clicked += () => OnBulkBuyClicked(1);
            buyX1Btn.AddManipulator(new HoldButtonManipulator(() => OnBulkBuyClicked(1), 400, 80));
        }

        if (buyX10Btn != null)
        {
            buyX10Btn.AddToClassList("buy-x10");
            buyX10Btn.clicked += () => OnBulkBuyClicked(10);
            buyX10Btn.AddManipulator(new HoldButtonManipulator(() => OnBulkBuyClicked(10), 400, 50));
        }

        if (buyMaxBtn != null)
        {
            buyMaxBtn.clicked += OnBuyMaxClicked;
        }
    }

    // ========================================
    // タブ
    // ========================================

    private void SetupTabs()
    {
        if (tabContainer == null) return;

        tabContainer.Clear();

        // カテゴリ、ラベル、アイコンの定義
        var categories = new[]
        {
            (UpgradeData.UpgradeCategory.Click, "クリック", "⚔"),
            (UpgradeData.UpgradeCategory.Income, "収入", "💰"),
            (UpgradeData.UpgradeCategory.Critical, "クリティカル", "⚡"),
            (UpgradeData.UpgradeCategory.Skill, "スキル", "🎯"),
            (UpgradeData.UpgradeCategory.Special, "特殊", "⭐")
        };

        foreach (var (category, label, icon) in categories)
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
            tabContainer.Add(tab);
        }
    }

    private void SwitchCategory(UpgradeData.UpgradeCategory category)
    {
        currentCategory = category;
        UpdateTabStyles();
        RefreshList();
        ClearDetailPanel();
    }

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

    // ========================================
    // ListView
    // ========================================

    private void SetupListView()
    {
        if (upgradeListView == null) return;

        upgradeListView.makeItem = MakeItem;
        upgradeListView.bindItem = BindItem;
        upgradeListView.itemsSource = currentList;
        upgradeListView.fixedItemHeight = 64;
        upgradeListView.selectionType = SelectionType.Single;
        upgradeListView.selectionChanged += OnSelectionChanged;
    }

    private VisualElement MakeItem()
    {
        var itemView = new ShopItemView();
        return itemView.Root;
    }

    private void BindItem(VisualElement element, int index)
    {
        if (index < 0 || index >= currentList.Count) return;

        if (element.userData is ShopItemView itemView)
        {
            itemView.Bind(currentList[index]);
        }
    }

    // ========================================
    // 選択 → 詳細パネル表示
    // ========================================

    private void OnSelectionChanged(IEnumerable<object> selection)
    {
        selectedUpgrade = null;

        foreach (var item in selection)
        {
            if (item is UpgradeData data)
            {
                selectedUpgrade = data;
                break;
            }
        }

        if (selectedUpgrade != null)
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

    private void RefreshDetailPanel()
    {
        if (selectedUpgrade == null) return;

        var gc = GameController.Instance;
        int level = gc.GetUpgradeLevel(selectedUpgrade.id);
        double cost = selectedUpgrade.GetCostAtLevel(level);
        UpgradeState state = gc.GetUpgradeState(selectedUpgrade);
        bool isMax = selectedUpgrade.IsMaxLevel(level);
        double money = gc.GetMoney();
        bool canAfford = money >= cost;

        // アイコン表示制御
        if (detailIcon != null)
        {
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

        if (detailName != null) detailName.text = selectedUpgrade.displayName;

        // レベル表示（無限アップグレードの場合は∞表示）
        if (detailLevel != null)
        {
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

        if (detailCategory != null) detailCategory.text = selectedUpgrade.GetCategoryDisplayName().ToUpper();

        StartTypewriterEffect(selectedUpgrade.description ?? "");

        // 効果プレビュー（現在 → 次レベル）を更新
        RefreshEffectPreview(level, isMax);

        // コスト表示
        if (detailCost != null)
        {
            detailCost.text = isMax ? "---" : $"{cost:N0}";
            detailCost.RemoveFromClassList("not-enough");
            if (!isMax && !canAfford) detailCost.AddToClassList("not-enough");
        }

        RefreshMaterialsDisplay();

        // 一括購入ボタンの状態更新
        UpdateBulkBuyButtons(state, isMax, cost);
    }

    /// <summary>
    /// 一括購入ボタンの有効/無効を更新
    /// </summary>
    private void UpdateBulkBuyButtons(UpgradeState state, bool isMax, double singleCost)
    {
        var gc = GameController.Instance;
        double money = gc.GetMoney();
        bool canBuyOne = state == UpgradeState.ReadyToUpgrade;

        // ×1 ボタン
        if (buyX1Btn != null)
        {
            buyX1Btn.SetEnabled(canBuyOne);
            buyX1Btn.text = isMax ? "-" : $"×1\n{singleCost:N0}";
        }

        // ×10 ボタン: 10回分のコストを計算
        if (buyX10Btn != null)
        {
            int maxBuyCount = CalculateMaxBuyCount(money);
            int buyCount = System.Math.Min(10, maxBuyCount);
            double totalCost = CalculateTotalCost(buyCount);
            bool canBuy10 = canBuyOne && buyCount > 0;

            buyX10Btn.SetEnabled(canBuy10);
            buyX10Btn.text = isMax ? "-" : $"×{buyCount}\n{totalCost:N0}";
        }

        // MAX ボタン
        if (buyMaxBtn != null)
        {
            int maxCount = CalculateMaxBuyCount(money);
            bool canBuyMax = canBuyOne && maxCount > 0;

            buyMaxBtn.SetEnabled(canBuyMax);
            if (isMax)
            {
                buyMaxBtn.text = "MAX";
            }
            else if (maxCount > 0)
            {
                double totalCost = CalculateTotalCost(maxCount);
                buyMaxBtn.text = $"MAX(×{maxCount})\n{totalCost:N0}";
            }
            else
            {
                buyMaxBtn.text = "MAX\n---";
            }
        }
    }

    /// <summary>
    /// 通貨変動時にボタン状態を再計算（選択中のアイテムがある場合のみ）
    /// </summary>
    private void RefreshBulkBuyButtons()
    {
        if (selectedUpgrade == null) return;

        var gc = GameController.Instance;
        int level = gc.GetUpgradeLevel(selectedUpgrade.id);
        bool isMax = selectedUpgrade.IsMaxLevel(level);
        double singleCost = selectedUpgrade.GetCostAtLevel(level);
        UpgradeState state = gc.GetUpgradeState(selectedUpgrade);

        UpdateBulkBuyButtons(state, isMax, singleCost);
    }

    /// <summary>
    /// 所持金で買える最大回数を計算
    /// </summary>
    private int CalculateMaxBuyCount(double money)
    {
        if (selectedUpgrade == null) return 0;

        var gc = GameController.Instance;
        int currentLevel = gc.GetUpgradeLevel(selectedUpgrade.id);
        int maxLevel = selectedUpgrade.maxLevel;
        bool isUnlimited = maxLevel <= 0;

        int count = 0;
        double totalCost = 0;
        int level = currentLevel;

        // 最大100回まで（無限ループ防止）
        int safetyLimit = isUnlimited ? 100 : (maxLevel - currentLevel);

        while (count < safetyLimit)
        {
            double nextCost = selectedUpgrade.GetCostAtLevel(level);
            if (totalCost + nextCost > money) break;

            totalCost += nextCost;
            level++;
            count++;

            // 有限の場合、MAXに達したら終了
            if (!isUnlimited && level >= maxLevel) break;
        }

        return count;
    }

    /// <summary>
    /// 指定回数購入時の合計コストを計算
    /// </summary>
    private double CalculateTotalCost(int count)
    {
        if (selectedUpgrade == null || count <= 0) return 0;

        var gc = GameController.Instance;
        int currentLevel = gc.GetUpgradeLevel(selectedUpgrade.id);
        double total = 0;

        for (int i = 0; i < count; i++)
        {
            total += selectedUpgrade.GetCostAtLevel(currentLevel + i);
        }

        return total;
    }

    /// <summary>
    /// 一括購入（指定回数）
    /// </summary>
    private void OnBulkBuyClicked(int requestedCount)
    {
        if (selectedUpgrade == null) return;

        var gc = GameController.Instance;
        double money = gc.GetMoney();
        int maxBuyable = CalculateMaxBuyCount(money);
        int buyCount = System.Math.Min(requestedCount, maxBuyable);

        if (buyCount <= 0) return;

        int successCount = 0;
        for (int i = 0; i < buyCount; i++)
        {
            bool success = gc.PurchaseUpgrade(selectedUpgrade);
            if (success)
            {
                successCount++;
            }
            else
            {
                break;
            }
        }

        if (successCount > 0)
        {
            LogUIController.Msg($"{selectedUpgrade.displayName} を {successCount} 回強化しました！");
            PlayFlashEffect();
            PlayIconBounce();
            PlayEffectFlash();
            RefreshDetailPanel();
            upgradeListView?.RefreshItems();
        }
    }

    /// <summary>
    /// MAX購入（買えるだけ買う）
    /// </summary>
    private void OnBuyMaxClicked()
    {
        if (selectedUpgrade == null) return;

        var gc = GameController.Instance;
        double money = gc.GetMoney();
        int maxBuyable = CalculateMaxBuyCount(money);

        if (maxBuyable <= 0) return;

        OnBulkBuyClicked(maxBuyable);
    }

    /// <summary>
    /// アイコンバウンスアニメーション
    /// </summary>
    private void PlayIconBounce()
    {
        if (detailIcon == null) return;

        // バウンスクラスを追加
        detailIcon.AddToClassList("icon-bounce");

        // アニメーション終了後にクラスを削除
        detailIcon.schedule.Execute(() =>
        {
            detailIcon.RemoveFromClassList("icon-bounce");
        }).ExecuteLater(300);
    }

    /// <summary>
    /// 効果プレビューの緑フラッシュ
    /// </summary>
    private void PlayEffectFlash()
    {
        if (effectPreviewContainer == null) return;

        effectPreviewContainer.AddToClassList("effect-flash");

        effectPreviewContainer.schedule.Execute(() =>
        {
            effectPreviewContainer.RemoveFromClassList("effect-flash");
        }).ExecuteLater(400);
    }

    /// <summary>
    /// 効果プレビュー（現在値 → 次レベル値）の表示を更新
    /// </summary>
    private void RefreshEffectPreview(int currentLevel, bool isMax)
    {
        if (effectPreviewContainer == null) return;
        effectPreviewContainer.Clear();

        if (selectedUpgrade == null) return;

        // 現在の効果値
        double currentEffect = selectedUpgrade.GetTotalEffectAtLevel(currentLevel);
        // 次レベルの効果値
        double nextEffect = selectedUpgrade.GetTotalEffectAtLevel(currentLevel + 1);

        // メイン効果行を作成
        var effectRow = new VisualElement();
        effectRow.AddToClassList("effect-row");
        // MAXでない場合は下にもう1行あるのでボーダーを追加
        if (!isMax) effectRow.AddToClassList("effect-row-border");

        // 効果ラベル
        var effectLabel = new Label();
        effectLabel.AddToClassList("effect-label");
        effectLabel.text = GetEffectTypeName(selectedUpgrade.upgradeType);

        // 現在値
        var currentLabel = new Label();
        currentLabel.AddToClassList("effect-current");
        currentLabel.text = FormatEffectValue(currentEffect, selectedUpgrade.isPercentDisplay);

        // 矢印
        var arrowLabel = new Label();
        arrowLabel.AddToClassList("effect-arrow");
        arrowLabel.text = isMax ? "" : "▶";

        // 次レベル値
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
        effectPreviewContainer.Add(effectRow);

        // 増加量の表示（MAXでない場合）
        if (!isMax)
        {
            var diffRow = new VisualElement();
            diffRow.AddToClassList("effect-row");

            var diffLabel = new Label();
            diffLabel.AddToClassList("effect-label");
            diffLabel.text = "増加量";

            var diffValue = new Label();
            diffValue.AddToClassList("effect-next");
            double diff = nextEffect - currentEffect;
            diffValue.text = $"+{FormatEffectValue(diff, selectedUpgrade.isPercentDisplay)}";

            diffRow.Add(diffLabel);
            diffRow.Add(new VisualElement { style = { flexGrow = 1 } }); // スペーサー
            diffRow.Add(diffValue);
            effectPreviewContainer.Add(diffRow);
        }
    }

    /// <summary>
    /// 効果タイプの表示名を取得
    /// </summary>
    private string GetEffectTypeName(UpgradeData.UpgradeType type)
    {
        return type switch
        {
            UpgradeData.UpgradeType.Click_FlatAdd => "クリック威力",
            UpgradeData.UpgradeType.Click_PercentAdd => "クリック倍率",
            UpgradeData.UpgradeType.Income_FlatAdd => "自動収入",
            UpgradeData.UpgradeType.Income_PercentAdd => "収入倍率",
            UpgradeData.UpgradeType.Critical_ChanceAdd => "クリティカル率",
            UpgradeData.UpgradeType.Critical_PowerAdd => "クリティカル倍率",
            UpgradeData.UpgradeType.SP_ChargeAdd => "SPチャージ",
            UpgradeData.UpgradeType.Fever_PowerAdd => "フィーバー倍率",
            _ => "効果"
        };
    }

    /// <summary>
    /// 効果値をフォーマット
    /// </summary>
    private string FormatEffectValue(double value, bool isPercent)
    {
        if (isPercent)
        {
            return $"{value * 100:F1}%";
        }
        return value >= 1000 ? $"{value:N0}" : $"{value:F1}";
    }

    // ========================================
    // 文字送りエフェクト
    // ========================================

    private void StartTypewriterEffect(string text)
    {
        if (detailDesc == null) return;
        if (text == targetDescriptionText) return;

        if (typewriterTimer != null)
        {
            typewriterTimer.Pause();
            typewriterTimer = null;
        }

        targetDescriptionText = text;
        currentCharIndex = 0;
        detailDesc.text = ""; 

        typewriterTimer = root.schedule.Execute(OnTypewriterTick).Every(20);
    }

    private void OnTypewriterTick()
    {
        if (detailDesc == null) return;

        if (currentCharIndex >= targetDescriptionText.Length)
        {
            detailDesc.text = targetDescriptionText; 
            if (typewriterTimer != null)
            {
                typewriterTimer.Pause(); 
                typewriterTimer = null;
            }
            return;
        }

        currentCharIndex++;
        detailDesc.text = targetDescriptionText.Substring(0, currentCharIndex);
    }

    // ========================================
    // 通貨ドラムロールアニメーション
    // ========================================

    private void OnCurrencyTick()
    {
        bool changed = false;

        // Moneyのアニメーション
        if (System.Math.Abs(currentDisplayMoney - targetMoney) > 0.1)
        {
            // 現在値と目標値の差分の10%ずつ近づける（Lerp的挙動）
            double diff = targetMoney - currentDisplayMoney;
            
            // 最小変化量を設定して、最後がダラダラしないようにする
            double step = diff * 0.2; 
            if (System.Math.Abs(step) < 1.0) step = diff > 0 ? 1.0 : -1.0;

            currentDisplayMoney += step;

            // 行き過ぎ補正
            if ((step > 0 && currentDisplayMoney > targetMoney) || (step < 0 && currentDisplayMoney < targetMoney))
            {
                currentDisplayMoney = targetMoney;
            }
            changed = true;
        }
        else
        {
            currentDisplayMoney = targetMoney;
        }

        // Certのアニメーション
        if (System.Math.Abs(currentDisplayCert - targetCert) > 0.1)
        {
            double diff = targetCert - currentDisplayCert;
            double step = diff * 0.2;
            if (System.Math.Abs(step) < 1.0) step = diff > 0 ? 1.0 : -1.0;
            
            currentDisplayCert += step;
            if ((step > 0 && currentDisplayCert > targetCert) || (step < 0 && currentDisplayCert < targetCert))
            {
                currentDisplayCert = targetCert;
            }
            changed = true;
        }
        else
        {
            currentDisplayCert = targetCert;
        }

        if (changed)
        {
            UpdateCurrencyLabels();
        }
    }

    private void UpdateCurrencyLabels()
    {
        if (moneyLabel != null) moneyLabel.text = $"LMD: {currentDisplayMoney:N0}";
        if (certLabel != null) certLabel.text = $"資格証: {currentDisplayCert:N0}";
    }

    // ========================================
    // フラッシュ演出
    // ========================================
    
    private void PlayFlashEffect()
    {
        if (detailPanel == null) return;

        // フラッシュ用の白い膜を動的に生成
        var flashOverlay = new VisualElement();
        flashOverlay.style.position = Position.Absolute;
        flashOverlay.style.top = 0;
        flashOverlay.style.bottom = 0;
        flashOverlay.style.left = 0;
        flashOverlay.style.right = 0;
        flashOverlay.style.backgroundColor = new Color(1f, 1f, 1f, 0.4f); // 半透明の白
        flashOverlay.pickingMode = PickingMode.Ignore; // クリック透過
        
        detailPanel.Add(flashOverlay);

        // フェードアウトアニメーション
        // 50ms後にフェード開始
        detailPanel.schedule.Execute(() => {
            flashOverlay.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            flashOverlay.style.transitionDuration = new List<TimeValue> { new TimeValue(200, TimeUnit.Millisecond) };
            flashOverlay.style.opacity = 0f;
        }).ExecuteLater(50);

        // アニメーションが終わった頃に要素を削除
        detailPanel.schedule.Execute(() => {
            if (detailPanel.Contains(flashOverlay))
            {
                detailPanel.Remove(flashOverlay);
            }
        }).ExecuteLater(300);
    }

    // ========================================
    // その他
    // ========================================

    private void RefreshMaterialsDisplay()
    {
        if (detailMaterials == null) return;
        detailMaterials.Clear();

        if (selectedUpgrade?.requiredMaterials == null) return;

        foreach (var mat in selectedUpgrade.requiredMaterials)
        {
            if (mat.item == null) continue;

            var gc = GameController.Instance;
            int owned = gc.GetItemCount(mat.item.id);
            bool enough = owned >= mat.amount;

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
            detailMaterials.Add(matElement);
        }
    }

    private void ClearDetailPanel()
    {
        selectedUpgrade = null;

        if (typewriterTimer != null)
        {
            typewriterTimer.Pause();
            typewriterTimer = null;
        }
        targetDescriptionText = "";

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

        // 効果プレビューをクリア
        effectPreviewContainer?.Clear();

        if (detailCost != null)
        {
            detailCost.text = "";
            detailCost.RemoveFromClassList("not-enough");
        }

        detailMaterials?.Clear();

        // 一括購入ボタンを無効化
        if (buyX1Btn != null)
        {
            buyX1Btn.SetEnabled(false);
            buyX1Btn.text = "×1";
        }
        if (buyX10Btn != null)
        {
            buyX10Btn.SetEnabled(false);
            buyX10Btn.text = "×10";
        }
        if (buyMaxBtn != null)
        {
            buyMaxBtn.SetEnabled(false);
            buyMaxBtn.text = "MAX";
        }
    }

    private void BindEvents()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        if (gc.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged += OnMoneyChanged;
            gc.Wallet.OnCertificateChanged += OnCertChanged;
        }

        if (gc.Upgrade != null)
        {
            gc.Upgrade.OnUpgradePurchased += OnUpgradePurchased;
        }
    }

    private void UnbindEvents()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        if (gc.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged -= OnMoneyChanged;
            gc.Wallet.OnCertificateChanged -= OnCertChanged;
        }

        if (gc.Upgrade != null)
        {
            gc.Upgrade.OnUpgradePurchased -= OnUpgradePurchased;
        }
    }

    // ここはターゲット値を更新するだけにする（表示更新はTickで行う）
    private void OnMoneyChanged(double amount)
    {
        targetMoney = amount;
        // 詳細パネルのボタン状態も更新（お金が貯まった時に購入可能になるように）
        RefreshBulkBuyButtons();
    }

    private void OnCertChanged(double amount)
    {
        targetCert = amount;
    }

    private void OnUpgradePurchased(UpgradeData data, int level)
    {
        if (selectedUpgrade != data)
        {
            RefreshList();
        }
        else
        {
            upgradeListView?.RefreshItems();
        }
    }

    // 削除（アニメーションループで更新するため）
    private void RefreshCurrencyDisplay()
    {
        // OnCurrencyTickで処理するのでここは空でOK、もしくは初期化時のみ使用
        // 初期化以外では呼ばないようにする
    }

    private void RefreshList()
    {
        currentList.Clear();
        currentList.AddRange(database.GetSorted(currentCategory));

        // リスト件数を更新
        if (listCountLabel != null)
        {
            listCountLabel.text = $"{currentList.Count} items";
        }

        if (upgradeListView != null)
        {
            upgradeListView.ClearSelection();
            upgradeListView.Rebuild();
        }
    }

    public void Dispose()
    {
        UnbindEvents();

        // タイマー停止
        if (typewriterTimer != null) typewriterTimer.Pause();
        if (currencyTimer != null) currencyTimer.Pause();

        if (upgradeListView != null)
        {
            upgradeListView.selectionChanged -= OnSelectionChanged;
        }

        // 一括購入ボタンのイベント解除（ボタン自体がrootと一緒に破棄されるので省略可）
        // ラムダ式で登録したclickedは同じインスタンスで解除できないため、
        // rootごと破棄される前提で明示的な解除は行わない

        currentList.Clear();
        selectedUpgrade = null;
    }
}