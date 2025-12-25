using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// ショップ/強化画面のUIファサード
/// 単一責任: 各サブコントローラーの統合と画面全体のライフサイクル管理
/// </summary>
public class ShopUIController : IViewController
{
    private const string LOG_TAG = "[ShopUIController]";
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private Label moneyLabel;
    private Label certLabel;
    private ListView upgradeListView;
    private Label listCountLabel;

    // ========================================
    // サブコントローラー（分離された責任）
    // ========================================

    private ShopTabController tabController;
    private ShopDetailPanelController detailPanelController;
    private ShopAnimationHelper animationHelper;

    // ========================================
    // ビジネスロジック
    // ========================================

    private ShopService shopService;

    // ========================================
    // データ
    // ========================================

    private UpgradeDatabase database;
    private List<UpgradeData> currentList = new();

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        if (root == null)
        {
            Debug.LogError($"{LOG_TAG} Initialize: root VisualElement is null");
            return;
        }

        this.root = root;

        try
        {
            var database = GameController.Instance?.Upgrade?.Database;

            if (database == null)
            {
                Debug.LogWarning($"{LOG_TAG} UpgradeDatabase not found in UpgradeManager!");
                return;
            }
            this.database = database;

            // ビジネスロジック層の初期化
            var gc = GameController.Instance;
            if (gc == null)
            {
                Debug.LogError($"{LOG_TAG} GameController.Instance is null");
                return;
            }

            shopService = new ShopService(gc);
            shopService.OnPurchaseSuccess += OnPurchaseSuccess;

            // UI要素取得
            QueryElements();

            // UI要素検証
            if (!ValidateUIElements())
            {
                Debug.LogWarning($"{LOG_TAG} Some UI elements are missing, shop may not function correctly");
            }

            // サブコントローラーの初期化
            InitializeSubControllers();

            // ListView設定
            SetupListView();

            // イベント登録
            BindEvents();

            // 初期表示
            tabController?.SwitchCategory(UpgradeData.UpgradeCategory.Click);

            // 初期カテゴリのリストを読み込む
            // ※ SwitchCategoryは同一カテゴリへの切り替えを無視するため、
            //    初期値がClickの場合はOnCategoryChangedが発火しない。
            //    そのため明示的にRefreshListを呼び出す必要がある。
            RefreshList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LOG_TAG} Initialize failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private bool ValidateUIElements()
    {
        bool isValid = true;

        if (moneyLabel == null)
        {
            Debug.LogWarning($"{LOG_TAG} UI element 'money-label' not found");
            isValid = false;
        }
        if (certLabel == null)
        {
            Debug.LogWarning($"{LOG_TAG} UI element 'cert-label' not found");
            isValid = false;
        }
        if (upgradeListView == null)
        {
            Debug.LogWarning($"{LOG_TAG} UI element 'upgrade-list' not found");
            isValid = false;
        }

        return isValid;
    }

    private void QueryElements()
    {
        moneyLabel = root.Q<Label>("money-label");
        certLabel = root.Q<Label>("cert-label");
        upgradeListView = root.Q<ListView>("upgrade-list");
        listCountLabel = root.Q<Label>("list-count");
    }

    private void InitializeSubControllers()
    {
        // アニメーションヘルパー
        animationHelper = new ShopAnimationHelper();
        var detailDesc = root.Q<Label>("detail-desc");
        animationHelper.Initialize(root, moneyLabel, certLabel, detailDesc);
        animationHelper.SetInitialCurrencyValues(shopService.GetMoney(), shopService.GetCertificates());
        animationHelper.StartCurrencyAnimation();

        // タブコントローラー
        tabController = new ShopTabController();
        var tabContainer = root.Q<VisualElement>("tab-container");
        tabController.Initialize(tabContainer);
        tabController.OnCategoryChanged += OnCategoryChanged;

        // 詳細パネルコントローラー
        detailPanelController = new ShopDetailPanelController();
        detailPanelController.Initialize(root, shopService, animationHelper);
        detailPanelController.OnBuyClicked += OnBulkBuyClicked;
        detailPanelController.OnBuyMaxClicked += OnBuyMaxClicked;
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
        upgradeListView.fixedItemHeight = ShopUIConstants.LIST_ITEM_HEIGHT;
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
        if (element == null) return;

        try
        {
            if (element.userData is ShopItemView itemView)
            {
                var upgrade = currentList[index];
                if (upgrade != null)
                {
                    itemView.Bind(upgrade);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"{LOG_TAG} BindItem failed at index {index}: {ex.Message}");
        }
    }

    // ========================================
    // イベントハンドラー
    // ========================================

    private void OnCategoryChanged(UpgradeData.UpgradeCategory category)
    {
        RefreshList();
        detailPanelController.ClearDetailPanel();
    }

    private void OnSelectionChanged(IEnumerable<object> selection)
    {
        UpgradeData selected = null;

        foreach (var item in selection)
        {
            if (item is UpgradeData data)
            {
                selected = data;
                break;
            }
        }

        detailPanelController.SelectItem(selected);
    }

    private void OnBulkBuyClicked(UpgradeData upgrade, int count)
    {
        if (upgrade == null) return;
        shopService.ExecuteBulkPurchase(upgrade, count);
    }

    private void OnBuyMaxClicked(UpgradeData upgrade)
    {
        if (upgrade == null) return;
        shopService.ExecuteMaxPurchase(upgrade);
    }

    private void OnPurchaseSuccess(UpgradeData upgrade, int count)
    {
        LogUIController.Msg($"{upgrade.displayName} を {count} 回強化しました！");
        detailPanelController.PlayPurchaseEffects();
        detailPanelController.RefreshDetailPanel();
        upgradeListView?.RefreshItems();
    }

    // ========================================
    // 通貨イベント
    // ========================================

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

    private void OnMoneyChanged(double amount)
    {
        animationHelper.SetTargetMoney(amount);
        detailPanelController.RefreshBulkBuyButtons();
    }

    private void OnCertChanged(double amount)
    {
        animationHelper.SetTargetCert(amount);
    }

    private void OnUpgradePurchased(UpgradeData data, int level)
    {
        if (detailPanelController.SelectedUpgrade != data)
        {
            RefreshList();
        }
        else
        {
            upgradeListView?.RefreshItems();
        }
    }

    // ========================================
    // リスト更新
    // ========================================

    private void RefreshList()
    {
        currentList.Clear();

        try
        {
            var gc = GameController.Instance;
            if (gc == null || gc.Upgrade == null || database == null || tabController == null)
            {
                Debug.LogWarning($"{LOG_TAG} RefreshList: Required dependencies not available");
                return;
            }

            var currentCategory = tabController.CurrentCategory;
            var allItems = database.GetSorted(currentCategory);

            if (allItems == null)
            {
                Debug.LogWarning($"{LOG_TAG} RefreshList: No items found for category {currentCategory}");
                return;
            }

            foreach (var item in allItems)
            {
                if (item != null && gc.Upgrade.GetState(item) != UpgradeState.Locked)
                {
                    currentList.Add(item);
                }
            }

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
        catch (Exception ex)
        {
            Debug.LogError($"{LOG_TAG} RefreshList failed: {ex.Message}");
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        UnbindEvents();

        // ShopServiceのイベント解除
        if (shopService != null)
        {
            shopService.OnPurchaseSuccess -= OnPurchaseSuccess;
        }

        // サブコントローラーの解放
        if (tabController != null)
        {
            tabController.OnCategoryChanged -= OnCategoryChanged;
            tabController.Dispose();
        }

        if (detailPanelController != null)
        {
            detailPanelController.OnBuyClicked -= OnBulkBuyClicked;
            detailPanelController.OnBuyMaxClicked -= OnBuyMaxClicked;
            detailPanelController.Dispose();
        }

        animationHelper?.Dispose();

        if (upgradeListView != null)
        {
            upgradeListView.selectionChanged -= OnSelectionChanged;
        }

        currentList.Clear();
        shopService = null;
    }
}
