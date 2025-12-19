using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// マーケットオーバーレイUIのコントローラ
/// Bloomberg風の株式市場画面を管理
///
/// リファクタリング後: ファサードパターン + 分離コントローラ構成
/// - MarketChartController: チャート描画
/// - MarketTradeController: 売買パネル
/// - MarketSkillController: スキルパネル
///
/// UIレイヤー分離:
/// - mainRoot: チャート、演出、リスト（Sort Order = 0）
/// - tradeRoot: ボタン、入力欄（Sort Order = 1、常に最前面）
/// </summary>
public class MarketUIController : IViewController
{
    // ========================================
    // 定数
    // ========================================
    private const string TRADE_VIEW_PATH = "Main_UI/Market_/UI/MarketTradeView";
    private const string TRADE_PANEL_SETTINGS_PATH = "UI/TradePanelSettings";
    private const string PANEL_SETTINGS_PATH = "UI/PanelSettings";
    private const int TRADE_LAYER_SORT_ORDER = 100;

    // ========================================
    // 依存（ファサード経由）
    // ========================================
    private readonly IMarketFacade facade;
    private readonly MarketEventHub eventHub;

    // ========================================
    // 分離コントローラ
    // ========================================
    private MarketChartController chartController;
    private MarketTradeController tradeController;
    private MarketSkillController skillController;
    private MarketPVEUIController pveUIController;

    // ========================================
    // UI要素
    // ========================================
    private VisualElement root;
    private VisualElement overlayRoot;

    // 売買レイヤー（動的生成）
    private GameObject tradeLayerObject;
    private UIDocument tradeLayerDocument;
    private VisualElement tradeRoot;

    // ヘッダー
    private Label marketTimeLabel;
    private Button closeButton;

    // 左パネル（資産情報）
    private Label lmdValueLabel;
    private Label totalValueLabel;
    private Label totalPnlLabel;
    private Label rhodosPriceLabel;
    private Label rhodosRankLabel;
    private Label dividendTimerLabel;
    private ScrollView portfolioList;
    private VisualElement emptyPortfolio;

    // 右パネル（銘柄リスト）
    private ScrollView stockList;

    // 演出用
    private VisualElement cutInOverlay;
    private Label cutInText;
    private VisualElement crashOverlay;
    private VisualElement lossCutOverlay;

    // ========================================
    // 状態
    // ========================================
    private IVisualElementScheduledItem updateTimer;

    // ========================================
    // コンストラクタ
    // ========================================

    public MarketUIController() : this(MarketFacade.Instance) { }

    public MarketUIController(IMarketFacade facade)
    {
        this.facade = facade;
        this.eventHub = new MarketEventHub();
    }

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        this.root = root;

        // 売買レイヤーを動的生成
        CreateTradeLayer();

        QueryElements();
        InitializeSubControllers();
        BindUIEvents();
        BindMarketEvents();

        // 更新ループ開始（30fps）
        updateTimer = root.schedule.Execute(OnUpdateTick).Every(33);

        // メニューから開いた時は表示状態にする
        if (overlayRoot != null)
        {
            overlayRoot.AddToClassList("visible");
        }

        // 最初の銘柄を選択
        var unlockedStocks = facade.GetUnlockedStocks();
        if (unlockedStocks != null && unlockedStocks.Count > 0)
        {
            SelectStock(unlockedStocks[0]);
        }

        RefreshStockList();
        RefreshAssetPanel();
        RefreshPortfolioList();

        // 初回オープン時にチュートリアルを開始
        facade.TryStartTutorial("market_basic", root);
    }

    /// <summary>
    /// 売買操作専用レイヤーを動的に生成
    /// Sort Order を高く設定することで、常に最前面に表示される
    /// </summary>
    private void CreateTradeLayer()
    {
        // UXMLアセットをロード
        var tradeViewAsset = Resources.Load<VisualTreeAsset>(TRADE_VIEW_PATH);
        if (tradeViewAsset == null)
        {
            Debug.LogError($"[MarketUIController] Failed to load {TRADE_VIEW_PATH}");
            return;
        }

        // 売買レイヤー専用のPanelSettingsをロード
        var panelSettings = Resources.Load<PanelSettings>(TRADE_PANEL_SETTINGS_PATH);
        if (panelSettings == null)
        {
            // フォールバック: 通常のPanelSettingsを使用
            panelSettings = Resources.Load<PanelSettings>(PANEL_SETTINGS_PATH);
            Debug.LogWarning($"[MarketUIController] TradePanelSettings not found, using default PanelSettings");
        }

        if (panelSettings == null)
        {
            // フォールバック: 既存のUIDocumentから取得
            var existingUIDoc = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            if (existingUIDoc != null)
            {
                panelSettings = existingUIDoc.panelSettings;
            }
        }

        if (panelSettings == null)
        {
            Debug.LogError($"[MarketUIController] Failed to load PanelSettings. Trade layer will not work correctly.");
            return;
        }

        // 新しいGameObjectを作成
        tradeLayerObject = new GameObject("Market_Trade_Layer");

        // UIDocumentコンポーネントを追加
        tradeLayerDocument = tradeLayerObject.AddComponent<UIDocument>();
        tradeLayerDocument.panelSettings = panelSettings;
        tradeLayerDocument.sortingOrder = TRADE_LAYER_SORT_ORDER;

        // UXMLを適用
        tradeLayerDocument.visualTreeAsset = tradeViewAsset;

        // ルート要素を取得
        tradeRoot = tradeLayerDocument.rootVisualElement;

        // rootVisualElement自体もクリック透過に設定（これがないと下のレイヤーにクリックが届かない）
        tradeRoot.pickingMode = PickingMode.Ignore;

        // trade-layer-rootも念のためIgnoreに設定
        var tradeLayerRoot = tradeRoot.Q<VisualElement>("trade-layer-root");
        if (tradeLayerRoot != null)
        {
            tradeLayerRoot.pickingMode = PickingMode.Ignore;
        }

        Debug.Log($"[MarketUIController] Trade layer created with Sort Order: {TRADE_LAYER_SORT_ORDER}, PanelSettings: {panelSettings.name}");
    }

    private void QueryElements()
    {
        overlayRoot = root.Q<VisualElement>("market-overlay-root");

        // ヘッダー
        marketTimeLabel = root.Q<Label>("market-time");
        closeButton = root.Q<Button>("close-btn");

        // 左パネル
        lmdValueLabel = root.Q<Label>("lmd-value");
        totalValueLabel = root.Q<Label>("total-value");
        totalPnlLabel = root.Q<Label>("total-pnl");
        rhodosPriceLabel = root.Q<Label>("rhodos-price");
        rhodosRankLabel = root.Q<Label>("rhodos-rank");
        dividendTimerLabel = root.Q<Label>("dividend-timer");
        portfolioList = root.Q<ScrollView>("portfolio-list");
        emptyPortfolio = root.Q<VisualElement>("empty-portfolio");

        // 右パネル
        stockList = root.Q<ScrollView>("stock-list");

        // 演出
        cutInOverlay = root.Q<VisualElement>("cut-in-overlay");
        cutInText = root.Q<Label>("cut-in-text");
        crashOverlay = root.Q<VisualElement>("crash-overlay");
        lossCutOverlay = root.Q<VisualElement>("loss-cut-overlay");
    }

    private void InitializeSubControllers()
    {
        // チャートコントローラ（メインレイヤー）
        chartController = new MarketChartController(root, facade);
        chartController.OnStockSelected += OnStockSelectedFromChart;

        // 売買コントローラ（売買レイヤー - 最前面）
        // tradeRoot が null の場合は root にフォールバック
        var tradeControllerRoot = tradeRoot ?? root;
        tradeController = new MarketTradeController(tradeControllerRoot, facade);
        tradeController.OnTradeExecuted += OnTradeExecuted;
        tradeController.OnLossCutExecuted += _ => PlayLossCutEffect();

        // スキルコントローラ（売買レイヤー - 最前面）
        var skillControllerRoot = tradeRoot ?? root;
        skillController = new MarketSkillController(skillControllerRoot, facade);
        skillController.OnInsiderStateChanged += chartController.SetInsiderActive;

        // PVE UIコントローラ（メインレイヤー）
        pveUIController = new MarketPVEUIController();
        pveUIController.Initialize(root);
    }

    private void BindUIEvents()
    {
        if (closeButton != null)
        {
            closeButton.clicked += Hide;
        }
    }

    private void BindMarketEvents()
    {
        eventHub.Subscribe();

        eventHub.OnPriceUpdated += OnPriceUpdated;
        eventHub.OnStockBought += OnStockBought;
        eventHub.OnStockSold += OnStockSold;
        eventHub.OnPriceCrash += OnPriceCrash;
        eventHub.OnNewsGenerated += OnNewsGenerated;
        eventHub.OnMoneyChanged += OnMoneyChanged;
        eventHub.OnPortfolioUpdated += OnPortfolioUpdated;
        eventHub.OnDividendPaid += OnDividendPaid;
    }

    private void UnbindMarketEvents()
    {
        eventHub.OnPriceUpdated -= OnPriceUpdated;
        eventHub.OnStockBought -= OnStockBought;
        eventHub.OnStockSold -= OnStockSold;
        eventHub.OnPriceCrash -= OnPriceCrash;
        eventHub.OnNewsGenerated -= OnNewsGenerated;
        eventHub.OnMoneyChanged -= OnMoneyChanged;
        eventHub.OnPortfolioUpdated -= OnPortfolioUpdated;
        eventHub.OnDividendPaid -= OnDividendPaid;

        eventHub.Unsubscribe();
    }

    // ========================================
    // 表示/非表示
    // ========================================

    public void Show()
    {
        overlayRoot?.AddToClassList("visible");
    }

    public void Hide()
    {
        overlayRoot?.RemoveFromClassList("visible");
    }

    public bool IsVisible => overlayRoot?.ClassListContains("visible") ?? false;

    public void Toggle()
    {
        if (IsVisible) Hide();
        else Show();
    }

    // ========================================
    // 更新ループ
    // ========================================

    private void OnUpdateTick()
    {
        // 時刻更新
        if (marketTimeLabel != null)
        {
            marketTimeLabel.text = DateTime.Now.ToString("HH:mm:ss");
        }

        // スキルクールダウン更新
        skillController?.UpdateCooldowns(0.033f);

        // チャート再描画
        chartController?.RequestRepaint();

        // 売買ボタン状態更新
        tradeController?.UpdateTradeButtons();

        // ロドス株パネル更新
        RefreshRhodosStockPanel();
    }

    // ========================================
    // 銘柄選択
    // ========================================

    public void SelectStock(StockData stock)
    {
        if (stock == null) return;

        chartController?.SelectStock(stock);
        tradeController?.SetSelectedStock(stock.stockId);
        skillController?.SetSelectedStock(stock.stockId);

        RefreshStockListSelection();
    }

    private void OnStockSelectedFromChart(string stockId)
    {
        tradeController?.SetSelectedStock(stockId);
        skillController?.SetSelectedStock(stockId);
        RefreshStockListSelection();
    }

    // ========================================
    // 銘柄リスト
    // ========================================

    private void RefreshStockList()
    {
        if (stockList == null) return;

        stockList.Clear();

        var stocks = facade.GetUnlockedStocks();
        if (stocks == null) return;

        foreach (var stock in stocks)
        {
            var item = CreateStockListItem(stock);
            stockList.Add(item);
        }
    }

    private VisualElement CreateStockListItem(StockData stock)
    {
        var item = new VisualElement();
        item.AddToClassList("stock-item");
        item.userData = stock.stockId;

        // ロゴ
        var logo = new VisualElement();
        logo.AddToClassList("stock-logo");
        if (stock.logo != null)
        {
            logo.style.backgroundImage = new StyleBackground(stock.logo);
        }

        // 情報
        var info = new VisualElement();
        info.AddToClassList("stock-info");

        var code = new Label { text = stock.stockId };
        code.AddToClassList("stock-code");

        var name = new Label { text = stock.companyName };
        name.AddToClassList("stock-name");

        info.Add(code);
        info.Add(name);

        // 価格エリア
        var priceArea = new VisualElement();
        priceArea.AddToClassList("stock-price-area");

        var state = facade.GetStockState(stock.stockId);
        double price = state?.currentPrice ?? stock.initialPrice;
        double change = state?.ChangeRate ?? 0;

        var priceLabel = new Label { text = facade.FormatPrice(price) };
        priceLabel.AddToClassList("stock-price");
        priceLabel.name = $"price-{stock.stockId}";

        var changeLabel = new Label { text = facade.FormatChangeRate(change) };
        changeLabel.AddToClassList("stock-change");
        changeLabel.AddToClassList(change >= 0 ? "positive" : "negative");
        changeLabel.name = $"change-{stock.stockId}";

        priceArea.Add(priceLabel);
        priceArea.Add(changeLabel);

        item.Add(logo);
        item.Add(info);
        item.Add(priceArea);

        // クリックイベント
        item.RegisterCallback<ClickEvent>(evt =>
        {
            SelectStock(stock);
        });

        return item;
    }

    private void RefreshStockListSelection()
    {
        if (stockList == null) return;

        string selectedId = chartController?.SelectedStockId;

        foreach (var child in stockList.Children())
        {
            child.RemoveFromClassList("selected");
            if (child.userData as string == selectedId)
            {
                child.AddToClassList("selected");
            }
        }
    }

    private void UpdateStockListItem(string stockId, double price, double changeRate)
    {
        var priceLabel = stockList?.Q<Label>($"price-{stockId}");
        var changeLabel = stockList?.Q<Label>($"change-{stockId}");

        if (priceLabel != null)
        {
            priceLabel.text = facade.FormatPrice(price);
        }

        if (changeLabel != null)
        {
            changeLabel.text = facade.FormatChangeRate(changeRate);
            changeLabel.RemoveFromClassList("positive");
            changeLabel.RemoveFromClassList("negative");
            changeLabel.AddToClassList(changeRate >= 0 ? "positive" : "negative");
        }
    }

    // ========================================
    // 資産パネル
    // ========================================

    private void RefreshAssetPanel()
    {
        double money = facade.Money;
        double totalValue = facade.TotalPortfolioValue;
        double totalPnl = facade.TotalUnrealizedPnL;

        if (lmdValueLabel != null)
        {
            lmdValueLabel.text = $"{money:N0}";
        }

        if (totalValueLabel != null)
        {
            totalValueLabel.text = $"{totalValue:N0}";
        }

        if (totalPnlLabel != null)
        {
            string sign = totalPnl >= 0 ? "+" : "";
            totalPnlLabel.text = $"{sign}{totalPnl:N0}";
            totalPnlLabel.RemoveFromClassList("positive");
            totalPnlLabel.RemoveFromClassList("negative");
            totalPnlLabel.AddToClassList(totalPnl >= 0 ? "positive" : "negative");
        }
    }

    // ========================================
    // ポートフォリオリスト
    // ========================================

    private void RefreshPortfolioList()
    {
        if (portfolioList == null) return;

        portfolioList.Clear();

        var holdings = facade.GetHoldingSummaries();
        bool hasHoldings = holdings != null && holdings.Count > 0;

        if (emptyPortfolio != null)
        {
            emptyPortfolio.style.display = hasHoldings ? DisplayStyle.None : DisplayStyle.Flex;
        }

        if (!hasHoldings) return;

        foreach (var holding in holdings)
        {
            var item = new VisualElement();
            item.AddToClassList("portfolio-item");

            var nameLabel = new Label { text = holding.companyName };
            nameLabel.AddToClassList("portfolio-stock-name");

            var qtyLabel = new Label { text = $"×{holding.quantity}" };
            qtyLabel.AddToClassList("portfolio-quantity");

            var pnlLabel = new Label();
            pnlLabel.AddToClassList("portfolio-pnl");
            string sign = holding.unrealizedPnL >= 0 ? "+" : "";
            pnlLabel.text = $"{sign}{holding.unrealizedPnL:N0}";
            pnlLabel.AddToClassList(holding.unrealizedPnL >= 0 ? "profit" : "loss");

            item.Add(nameLabel);
            item.Add(qtyLabel);
            item.Add(pnlLabel);

            // クリックでその銘柄を選択
            string stockId = holding.stockId;
            item.RegisterCallback<ClickEvent>(evt =>
            {
                var stock = facade.GetUnlockedStocks()?.Find(s => s.stockId == stockId);
                if (stock != null) SelectStock(stock);
            });

            portfolioList.Add(item);
        }
    }

    // ========================================
    // 演出
    // ========================================

    private void OnTradeExecuted(bool isBuy)
    {
        PlayCutInEffect(isBuy);
    }

    private void PlayCutInEffect(bool isBuy)
    {
        if (cutInOverlay == null || cutInText == null) return;

        cutInText.text = isBuy ? "BUY!" : "SELL!";
        cutInText.RemoveFromClassList("buy");
        cutInText.RemoveFromClassList("sell");
        cutInText.AddToClassList(isBuy ? "buy" : "sell");

        cutInOverlay.AddToClassList("visible");

        root.schedule.Execute(() =>
        {
            cutInOverlay.RemoveFromClassList("visible");
        }).ExecuteLater(500);
    }

    private void PlayLossCutEffect()
    {
        if (lossCutOverlay == null) return;

        lossCutOverlay.AddToClassList("active");

        root.schedule.Execute(() =>
        {
            lossCutOverlay.RemoveFromClassList("active");
        }).ExecuteLater(800);
    }

    private void PlayCrashEffect()
    {
        if (crashOverlay == null) return;

        crashOverlay.AddToClassList("active");

        root.schedule.Execute(() =>
        {
            crashOverlay.RemoveFromClassList("active");
        }).ExecuteLater(300);
    }

    // ========================================
    // イベントハンドラ
    // ========================================

    private void OnPriceUpdated(StockPriceSnapshot snapshot)
    {
        UpdateStockListItem(snapshot.stockId, snapshot.price, snapshot.changeRate);
        chartController?.OnPriceUpdated(snapshot);
        RefreshAssetPanel();
    }

    private void OnStockBought(string stockId, int quantity, double totalCost)
    {
        RefreshPortfolioList();
        RefreshAssetPanel();
        LogUIController.Msg($"📈 {stockId} を {quantity} 株購入 (-{totalCost:N0} LMD)");
    }

    private void OnStockSold(string stockId, int quantity, double totalReturn, double profitLoss)
    {
        RefreshPortfolioList();
        RefreshAssetPanel();

        string resultText = profitLoss >= 0
            ? $"利確 +{profitLoss:N0} 🚀"
            : $"損切り {profitLoss:N0} 💀";
        LogUIController.Msg($"📉 {stockId} を {quantity} 株売却 ({resultText})");
    }

    private void OnPriceCrash(string stockId, double changeRate)
    {
        if (stockId == chartController?.SelectedStockId)
        {
            PlayCrashEffect();
        }

        // 銘柄リストの該当アイテムをフラッシュ
        var item = stockList?.Children().FirstOrDefault(c => c.userData as string == stockId);
        if (item != null)
        {
            item.AddToClassList("flash-red");
            root.schedule.Execute(() =>
            {
                item.RemoveFromClassList("flash-red");
            }).ExecuteLater(500);
        }
    }

    private void OnNewsGenerated(MarketNews news)
    {
        string prefix = news.type switch
        {
            MarketNewsType.Positive => "📈",
            MarketNewsType.Negative => "📉",
            MarketNewsType.Breaking => "🔴",
            _ => "📰"
        };
        LogUIController.Msg($"{prefix} {news.text}");
    }

    private void OnMoneyChanged(double amount)
    {
        RefreshAssetPanel();
    }

    private void OnPortfolioUpdated()
    {
        RefreshPortfolioList();
        RefreshAssetPanel();
    }

    private void OnDividendPaid(DividendPayment payment)
    {
        PlayDividendEffect(payment);
        RefreshAssetPanel();
    }

    // ========================================
    // ロドス株パネル
    // ========================================

    private void RefreshRhodosStockPanel()
    {
        if (rhodosPriceLabel != null)
        {
            rhodosPriceLabel.text = facade.GetRhodosPriceText();
        }

        if (rhodosRankLabel != null)
        {
            var rank = facade.GetRhodosRank();
            rhodosRankLabel.text = RhodosStockManager.GetRankDisplayName(rank);

            rhodosRankLabel.RemoveFromClassList("rank-high");
            rhodosRankLabel.RemoveFromClassList("rank-super");
            rhodosRankLabel.RemoveFromClassList("rank-god");

            string rankClass = RhodosStockManager.GetRankClassName(rank);
            if (!string.IsNullOrEmpty(rankClass))
            {
                rhodosRankLabel.AddToClassList(rankClass);
            }
        }

        if (dividendTimerLabel != null)
        {
            dividendTimerLabel.text = facade.GetRhodosDividendTimerText();
        }
    }

    private void PlayDividendEffect(DividendPayment payment)
    {
        if (cutInOverlay == null || cutInText == null) return;

        string rankName = RhodosStockManager.GetRankDisplayName(payment.rank);
        cutInText.text = $"💰 配当 [{rankName}]";
        cutInText.RemoveFromClassList("buy");
        cutInText.RemoveFromClassList("sell");
        cutInText.AddToClassList("buy");

        cutInOverlay.AddToClassList("visible");

        root.schedule.Execute(() =>
        {
            cutInOverlay.RemoveFromClassList("visible");
        }).ExecuteLater(1000);
    }

    // ========================================
    // 破棄
    // ========================================

    public void Dispose()
    {
        UnbindMarketEvents();

        chartController?.Dispose();
        tradeController?.Dispose();
        skillController?.Dispose();
        pveUIController?.Dispose();

        chartController = null;
        tradeController = null;
        skillController = null;
        pveUIController = null;

        if (updateTimer != null)
        {
            updateTimer.Pause();
            updateTimer = null;
        }

        // 動的に生成した売買レイヤーを破棄
        DestroyTradeLayer();
    }

    /// <summary>
    /// 動的に生成した売買レイヤーを破棄
    /// </summary>
    private void DestroyTradeLayer()
    {
        if (tradeLayerObject != null)
        {
            UnityEngine.Object.Destroy(tradeLayerObject);
            tradeLayerObject = null;
            tradeLayerDocument = null;
            tradeRoot = null;
            Debug.Log("[MarketUIController] Trade layer destroyed");
        }
    }
}
