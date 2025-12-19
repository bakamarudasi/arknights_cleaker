using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// ショップ/強化画面のUIファサード
/// 単一責任: 各サブコントローラーの統合と画面全体のライフサイクル管理
/// </summary>
public class ShopUIController : IViewController
{
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
        this.root = root;
        var database = GameController.Instance?.Upgrade?.Database;

        if (database == null)
        {
            Debug.LogWarning("[ShopUIController] UpgradeDatabase not found in UpgradeManager!");
            return;
        }
        this.database = database;

        // ビジネスロジック層の初期化
        var gc = GameController.Instance;
        shopService = new ShopService(gc);
        shopService.OnPurchaseSuccess += OnPurchaseSuccess;

        // UI要素取得
        QueryElements();

        // サブコントローラーの初期化
        InitializeSubControllers();

        // ListView設定
        SetupListView();

        // イベント登録
        BindEvents();

        // 初期表示
        tabController.SwitchCategory(UpgradeData.UpgradeCategory.Click);
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

        if (element.userData is ShopItemView itemView)
        {
            itemView.Bind(currentList[index]);
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

        var gc = GameController.Instance;
        var currentCategory = tabController.CurrentCategory;
        var allItems = database.GetSorted(currentCategory);

        foreach (var item in allItems)
        {
            if (gc.Upgrade.GetState(item) != UpgradeState.Locked)
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
