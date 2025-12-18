using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// マーケットオーバーレイUIのコントローラ
/// Bloomberg風の株式市場画面を管理
/// </summary>
public class MarketUIController : IViewController
{
    // ========================================
    // UI要素
    // ========================================
    private VisualElement root;
    private VisualElement overlayRoot;

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

    // 中央パネル（チャート）
    private Label chartStockCode;
    private Label chartStockName;
    private Label chartCurrentPrice;
    private Label chartChange;
    private VisualElement chartCanvas;
    private VisualElement chartLine;
    private Label statOpen;
    private Label statHigh;
    private Label statLow;
    private Label statVolatility;

    // 売買パネル
    private TextField tradeQuantityInput;
    private Button buyButton;
    private Button sellButton;

    // 右パネル（銘柄リスト）
    private ScrollView stockList;

    // スキルパネル
    private Button skillBuySupport;
    private Button skillInsider;
    private Label skillCooldownLabel;

    // 演出用
    private VisualElement cutInOverlay;
    private Label cutInText;
    private VisualElement crashOverlay;
    private VisualElement lossCutOverlay;

    // PVE UIコントローラ
    private MarketPVEUIController pveUIController;

    // ========================================
    // 状態
    // ========================================
    private StockData selectedStock;
    private string selectedStockId;
    private IVisualElementScheduledItem updateTimer;
    private IVisualElementScheduledItem chartAnimTimer;

    // スキルクールダウン
    private float buySupportCooldown = 0f;
    private float insiderCooldown = 0f;
    private bool isInsiderActive = false;

    // ========================================
    // 定数
    // ========================================
    private const float BUY_SUPPORT_COOLDOWN = 10f;
    private const float INSIDER_COOLDOWN = 60f;
    private const float INSIDER_DURATION = 5f;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root, StockDatabase database)
    {
        this.root = root;

        QueryElements();
        BindUIEvents();
        BindMarketEvents();

        // PVE UIコントローラを初期化
        pveUIController = new MarketPVEUIController();
        pveUIController.Initialize(root);

        // 更新ループ開始（30fps）
        updateTimer = root.schedule.Execute(OnUpdateTick).Every(33);

        // 初期状態：非表示
        if (overlayRoot != null)
        {
            overlayRoot.RemoveFromClassList("visible");
        }

        // 最初の銘柄を選択
        var unlockedStocks = MarketManager.Instance?.GetUnlockedStocks();
        if (unlockedStocks != null && unlockedStocks.Count > 0)
        {
            SelectStock(unlockedStocks[0]);
        }

        RefreshStockList();
        RefreshAssetPanel();
        RefreshPortfolioList();
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

        // 中央パネル
        chartStockCode = root.Q<Label>("chart-stock-code");
        chartStockName = root.Q<Label>("chart-stock-name");
        chartCurrentPrice = root.Q<Label>("chart-current-price");
        chartChange = root.Q<Label>("chart-change");
        chartCanvas = root.Q<VisualElement>("chart-canvas");
        chartLine = root.Q<VisualElement>("chart-line");
        statOpen = root.Q<Label>("stat-open");
        statHigh = root.Q<Label>("stat-high");
        statLow = root.Q<Label>("stat-low");
        statVolatility = root.Q<Label>("stat-volatility");

        // 売買パネル
        tradeQuantityInput = root.Q<TextField>("trade-quantity");
        buyButton = root.Q<Button>("buy-btn");
        sellButton = root.Q<Button>("sell-btn");

        // 右パネル
        stockList = root.Q<ScrollView>("stock-list");

        // スキルパネル
        skillBuySupport = root.Q<Button>("skill-buy-support");
        skillInsider = root.Q<Button>("skill-insider");
        skillCooldownLabel = root.Q<Label>("skill-cooldown");

        // 演出
        cutInOverlay = root.Q<VisualElement>("cut-in-overlay");
        cutInText = root.Q<Label>("cut-in-text");
        crashOverlay = root.Q<VisualElement>("crash-overlay");
        lossCutOverlay = root.Q<VisualElement>("loss-cut-overlay");

        // チャート描画設定
        if (chartLine != null)
        {
            chartLine.generateVisualContent += OnGenerateChartVisual;
        }
    }

    private void BindUIEvents()
    {
        // 閉じるボタン
        if (closeButton != null)
        {
            closeButton.clicked += Hide;
        }

        // 数量クイックボタン
        var qty10Btn = root.Q<Button>("qty-10");
        var qty100Btn = root.Q<Button>("qty-100");
        var qtyMaxBtn = root.Q<Button>("qty-max");

        if (qty10Btn != null) qty10Btn.clicked += () => SetTradeQuantity(10);
        if (qty100Btn != null) qty100Btn.clicked += () => SetTradeQuantity(100);
        if (qtyMaxBtn != null) qtyMaxBtn.clicked += SetMaxQuantity;

        // 売買ボタン
        if (buyButton != null) buyButton.clicked += OnBuyClicked;
        if (sellButton != null) sellButton.clicked += OnSellClicked;

        // スキルボタン
        if (skillBuySupport != null) skillBuySupport.clicked += OnBuySupportClicked;
        if (skillInsider != null) skillInsider.clicked += OnInsiderClicked;
    }

    private void BindMarketEvents()
    {
        MarketEventBus.OnPriceUpdated += OnPriceUpdated;
        MarketEventBus.OnStockBought += OnStockBought;
        MarketEventBus.OnStockSold += OnStockSold;
        MarketEventBus.OnPriceCrash += OnPriceCrash;
        MarketEventBus.OnNewsGenerated += OnNewsGenerated;
        MarketEventBus.OnDividendPaid += OnDividendPaid;

        // Wallet変更
        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }

        // Portfolio変更
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += OnPortfolioUpdated;
        }
    }

    private void UnbindMarketEvents()
    {
        MarketEventBus.OnPriceUpdated -= OnPriceUpdated;
        MarketEventBus.OnStockBought -= OnStockBought;
        MarketEventBus.OnStockSold -= OnStockSold;
        MarketEventBus.OnPriceCrash -= OnPriceCrash;
        MarketEventBus.OnNewsGenerated -= OnNewsGenerated;
        MarketEventBus.OnDividendPaid -= OnDividendPaid;

        if (WalletManager.Instance != null)
        {
            WalletManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }

        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated -= OnPortfolioUpdated;
        }
    }

    // ========================================
    // 表示/非表示
    // ========================================

    public void Show()
    {
        if (overlayRoot != null)
        {
            overlayRoot.AddToClassList("visible");
        }
    }

    public void Hide()
    {
        if (overlayRoot != null)
        {
            overlayRoot.RemoveFromClassList("visible");
        }
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

        // クールダウン更新
        UpdateCooldowns();

        // チャート再描画
        chartLine?.MarkDirtyRepaint();

        // 売買ボタン状態更新
        UpdateTradeButtons();

        // ロドス株パネル更新
        RefreshRhodosStockPanel();
    }

    private void UpdateCooldowns()
    {
        float dt = 0.033f; // 約30fps

        if (buySupportCooldown > 0)
        {
            buySupportCooldown -= dt;
            if (skillBuySupport != null)
            {
                skillBuySupport.SetEnabled(buySupportCooldown <= 0);
            }
        }

        if (insiderCooldown > 0)
        {
            insiderCooldown -= dt;
            if (skillInsider != null)
            {
                skillInsider.SetEnabled(insiderCooldown <= 0);
            }
        }

        // クールダウン表示
        if (skillCooldownLabel != null)
        {
            if (buySupportCooldown > 0 || insiderCooldown > 0)
            {
                float maxCd = Mathf.Max(buySupportCooldown, insiderCooldown);
                skillCooldownLabel.text = $"CD: {maxCd:F1}s";
            }
            else
            {
                skillCooldownLabel.text = "";
            }
        }
    }

    // ========================================
    // 銘柄選択
    // ========================================

    public void SelectStock(StockData stock)
    {
        if (stock == null) return;

        selectedStock = stock;
        selectedStockId = stock.stockId;

        RefreshChartHeader();
        RefreshChartStats();
        RefreshStockListSelection();
    }

    private void RefreshChartHeader()
    {
        if (selectedStock == null) return;

        if (chartStockCode != null) chartStockCode.text = selectedStock.stockId;
        if (chartStockName != null) chartStockName.text = selectedStock.companyName;

        var state = MarketManager.Instance?.GetStockState(selectedStockId);
        if (state != null)
        {
            UpdatePriceDisplay(state.currentPrice, state.ChangeRate);
        }
    }

    private void RefreshChartStats()
    {
        if (selectedStock == null) return;

        var state = MarketManager.Instance?.GetStockState(selectedStockId);
        if (state == null) return;

        if (statOpen != null) statOpen.text = StockPriceEngine.FormatPrice(state.openPrice);
        if (statHigh != null) statHigh.text = StockPriceEngine.FormatPrice(state.highPrice);
        if (statLow != null) statLow.text = StockPriceEngine.FormatPrice(state.lowPrice);
        if (statVolatility != null) statVolatility.text = $"{selectedStock.volatility:F2}";
    }

    private void UpdatePriceDisplay(double price, double changeRate)
    {
        if (chartCurrentPrice != null)
        {
            chartCurrentPrice.text = StockPriceEngine.FormatPrice(price);
            chartCurrentPrice.RemoveFromClassList("negative");
            if (changeRate < 0) chartCurrentPrice.AddToClassList("negative");
        }

        if (chartChange != null)
        {
            chartChange.text = StockPriceEngine.FormatChangeRate(changeRate);
            chartChange.RemoveFromClassList("negative");
            if (changeRate < 0) chartChange.AddToClassList("negative");
        }
    }

    // ========================================
    // チャート描画
    // ========================================

    private void OnGenerateChartVisual(MeshGenerationContext ctx)
    {
        if (selectedStockId == null) return;

        var history = MarketManager.Instance?.GetPriceHistory(selectedStockId);
        if (history == null || history.Length < 2) return;

        var painter = ctx.painter2D;
        var rect = chartLine.contentRect;

        if (rect.width <= 0 || rect.height <= 0) return;

        // 価格の範囲を計算
        double minPrice = history.Min();
        double maxPrice = history.Max();
        double priceRange = maxPrice - minPrice;
        if (priceRange < 0.01) priceRange = 1; // ゼロ除算防止

        // パディング
        float padding = 10f;
        float chartWidth = rect.width - padding * 2;
        float chartHeight = rect.height - padding * 2;

        // 色決定（上昇/下降）
        double lastPrice = history[^1];
        double firstPrice = history[0];
        Color lineColor = lastPrice >= firstPrice
            ? new Color(0.29f, 0.87f, 0.5f, 1f)  // 緑
            : new Color(0.94f, 0.27f, 0.27f, 1f); // 赤

        // ライン描画
        painter.strokeColor = lineColor;
        painter.lineWidth = 2f;
        painter.lineCap = LineCap.Round;
        painter.lineJoin = LineJoin.Round;

        painter.BeginPath();

        for (int i = 0; i < history.Length; i++)
        {
            float x = padding + (i / (float)(history.Length - 1)) * chartWidth;
            float y = padding + (float)((maxPrice - history[i]) / priceRange) * chartHeight;

            if (i == 0)
                painter.MoveTo(new Vector2(x, y));
            else
                painter.LineTo(new Vector2(x, y));
        }

        painter.Stroke();

        // グラデーション塗りつぶし（オプション）
        Color fillColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.1f);
        painter.fillColor = fillColor;

        painter.BeginPath();
        painter.MoveTo(new Vector2(padding, padding + chartHeight));

        for (int i = 0; i < history.Length; i++)
        {
            float x = padding + (i / (float)(history.Length - 1)) * chartWidth;
            float y = padding + (float)((maxPrice - history[i]) / priceRange) * chartHeight;
            painter.LineTo(new Vector2(x, y));
        }

        painter.LineTo(new Vector2(padding + chartWidth, padding + chartHeight));
        painter.ClosePath();
        painter.Fill();

        // インサイダーモード時は未来の点線を表示
        if (isInsiderActive && selectedStock != null)
        {
            DrawFuturePrediction(painter, history, minPrice, maxPrice, priceRange, padding, chartWidth, chartHeight);
        }
    }

    private void DrawFuturePrediction(Painter2D painter, double[] history, double minPrice, double maxPrice, double priceRange, float padding, float chartWidth, float chartHeight)
    {
        // 最後の価格から予測線を描画
        double lastPrice = history[^1];
        int futurePoints = 10;

        painter.strokeColor = new Color(0.66f, 0.33f, 0.97f, 0.6f); // 紫
        painter.lineWidth = 1.5f;

        // 点線風に描画
        float startX = padding + chartWidth;
        float startY = padding + (float)((maxPrice - lastPrice) / priceRange) * chartHeight;

        // 簡易予測（ドリフト方向に）
        double predictedPrice = lastPrice;
        for (int i = 1; i <= futurePoints; i++)
        {
            predictedPrice *= (1 + selectedStock.drift * 0.001f);

            float x = startX + (i * 5f);
            float y = padding + (float)((maxPrice - predictedPrice) / priceRange) * chartHeight;
            y = Mathf.Clamp(y, padding, padding + chartHeight);

            // 点線として描画
            if (i % 2 == 1)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(startX + ((i - 1) * 5f), startY));
                painter.LineTo(new Vector2(x, y));
                painter.Stroke();
            }

            startX = padding + chartWidth + ((i - 1) * 5f);
            startY = y;
        }
    }

    // ========================================
    // 銘柄リスト
    // ========================================

    private void RefreshStockList()
    {
        if (stockList == null) return;

        stockList.Clear();

        var stocks = MarketManager.Instance?.GetUnlockedStocks();
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

        var state = MarketManager.Instance?.GetStockState(stock.stockId);
        double price = state?.currentPrice ?? stock.initialPrice;
        double change = state?.ChangeRate ?? 0;

        var priceLabel = new Label { text = StockPriceEngine.FormatPrice(price) };
        priceLabel.AddToClassList("stock-price");
        priceLabel.name = $"price-{stock.stockId}";

        var changeLabel = new Label { text = StockPriceEngine.FormatChangeRate(change) };
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

        foreach (var child in stockList.Children())
        {
            child.RemoveFromClassList("selected");
            if (child.userData as string == selectedStockId)
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
            priceLabel.text = StockPriceEngine.FormatPrice(price);
        }

        if (changeLabel != null)
        {
            changeLabel.text = StockPriceEngine.FormatChangeRate(changeRate);
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
        double money = WalletManager.Instance?.Money ?? 0;
        double totalValue = PortfolioManager.Instance?.TotalValue ?? 0;
        double totalPnl = PortfolioManager.Instance?.TotalUnrealizedProfitLoss ?? 0;

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

        var holdings = PortfolioManager.Instance?.GetHoldingSummaries();
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
                var stock = MarketManager.Instance?.GetUnlockedStocks()?.Find(s => s.stockId == stockId);
                if (stock != null) SelectStock(stock);
            });

            portfolioList.Add(item);
        }
    }

    // ========================================
    // 売買
    // ========================================

    private void SetTradeQuantity(int qty)
    {
        if (tradeQuantityInput != null)
        {
            tradeQuantityInput.value = qty.ToString();
        }
    }

    private void SetMaxQuantity()
    {
        if (selectedStockId == null) return;

        int max = PortfolioManager.Instance?.GetMaxBuyableQuantity(selectedStockId) ?? 0;
        SetTradeQuantity(max);
    }

    private int GetTradeQuantity()
    {
        if (tradeQuantityInput == null) return 0;
        return int.TryParse(tradeQuantityInput.value, out int qty) ? qty : 0;
    }

    private void UpdateTradeButtons()
    {
        if (selectedStockId == null) return;

        int qty = GetTradeQuantity();
        int holdings = PortfolioManager.Instance?.GetHoldingQuantity(selectedStockId) ?? 0;
        int maxBuyable = PortfolioManager.Instance?.GetMaxBuyableQuantity(selectedStockId) ?? 0;

        // 購入ボタン
        if (buyButton != null)
        {
            buyButton.SetEnabled(qty > 0 && qty <= maxBuyable);
        }

        // 売却ボタン
        if (sellButton != null)
        {
            bool canSell = qty > 0 && qty <= holdings;
            sellButton.SetEnabled(canSell);

            // 利確/損切りモード表示
            bool hasProfit = PortfolioManager.Instance?.HasProfit(selectedStockId) ?? false;
            sellButton.RemoveFromClassList("profit-mode");
            sellButton.RemoveFromClassList("loss-mode");

            if (holdings > 0)
            {
                if (hasProfit)
                {
                    sellButton.text = "利確 🚀";
                    sellButton.AddToClassList("profit-mode");
                }
                else
                {
                    sellButton.text = "損切り 💀";
                    sellButton.AddToClassList("loss-mode");
                }
            }
            else
            {
                sellButton.text = "SELL";
            }
        }
    }

    private void OnBuyClicked()
    {
        if (selectedStockId == null) return;

        int qty = GetTradeQuantity();
        if (qty <= 0) return;

        bool success = PortfolioManager.Instance?.TryBuyStock(selectedStockId, qty) ?? false;
        if (success)
        {
            PlayCutInEffect(true);
        }
    }

    private void OnSellClicked()
    {
        if (selectedStockId == null) return;

        int qty = GetTradeQuantity();
        if (qty <= 0) return;

        bool hasLoss = PortfolioManager.Instance?.HasLoss(selectedStockId) ?? false;
        bool success = PortfolioManager.Instance?.TrySellStock(selectedStockId, qty) ?? false;

        if (success)
        {
            PlayCutInEffect(false);

            // 損切りの場合は追加演出
            if (hasLoss)
            {
                PlayLossCutEffect();
            }
        }
    }

    // ========================================
    // スキル
    // ========================================

    private void OnBuySupportClicked()
    {
        if (selectedStockId == null || buySupportCooldown > 0) return;

        // 物理買い支え（10クリック分の効果）
        MarketManager.Instance?.ApplyBuySupport(selectedStockId, 10);
        buySupportCooldown = BUY_SUPPORT_COOLDOWN;

        LogUIController.Msg($"📈 {selectedStock?.companyName} を物理買い支え！");
    }

    private void OnInsiderClicked()
    {
        if (insiderCooldown > 0) return;

        isInsiderActive = true;
        insiderCooldown = INSIDER_COOLDOWN;

        LogUIController.Msg("👁️ インサイダー情報を入手... 数秒先が見える！");

        // 一定時間後に効果終了
        root.schedule.Execute(() =>
        {
            isInsiderActive = false;
        }).ExecuteLater((long)(INSIDER_DURATION * 1000));
    }

    // ========================================
    // 演出
    // ========================================

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

        // ケルシーのため息SE（AudioManager経由で再生、あれば）
        // AudioManager.Instance?.PlaySE("kelsey_sigh");
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
        // 銘柄リストの価格更新
        UpdateStockListItem(snapshot.stockId, snapshot.price, snapshot.changeRate);

        // 選択中の銘柄なら詳細も更新
        if (snapshot.stockId == selectedStockId)
        {
            UpdatePriceDisplay(snapshot.price, snapshot.changeRate);
            RefreshChartStats();
        }

        // 資産パネル更新
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
        if (stockId == selectedStockId)
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
        // LogUIControllerに流す
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
        // 配当演出
        PlayDividendEffect(payment);
        RefreshAssetPanel();
    }

    // ========================================
    // ロドス株パネル
    // ========================================

    private void RefreshRhodosStockPanel()
    {
        var rhodosManager = RhodosStockManager.Instance;
        if (rhodosManager == null) return;

        // 株価表示
        if (rhodosPriceLabel != null)
        {
            rhodosPriceLabel.text = rhodosManager.GetPriceText();
        }

        // ランク表示
        if (rhodosRankLabel != null)
        {
            var rank = rhodosManager.CurrentRank;
            rhodosRankLabel.text = RhodosStockManager.GetRankDisplayName(rank);

            // ランクに応じたスタイル変更
            rhodosRankLabel.RemoveFromClassList("rank-high");
            rhodosRankLabel.RemoveFromClassList("rank-super");
            rhodosRankLabel.RemoveFromClassList("rank-god");

            string rankClass = RhodosStockManager.GetRankClassName(rank);
            if (!string.IsNullOrEmpty(rankClass))
            {
                rhodosRankLabel.AddToClassList(rankClass);
            }
        }

        // 配当タイマー表示
        if (dividendTimerLabel != null)
        {
            dividendTimerLabel.text = rhodosManager.GetDividendTimerText();
        }
    }

    private void PlayDividendEffect(DividendPayment payment)
    {
        // 配当カットイン（簡易版）
        if (cutInOverlay == null || cutInText == null) return;

        string rankName = RhodosStockManager.GetRankDisplayName(payment.rank);
        cutInText.text = $"💰 配当 [{rankName}]";
        cutInText.RemoveFromClassList("buy");
        cutInText.RemoveFromClassList("sell");
        cutInText.AddToClassList("buy"); // 緑色

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

        // PVE UIコントローラを破棄
        pveUIController?.Dispose();
        pveUIController = null;

        if (updateTimer != null)
        {
            updateTimer.Pause();
            updateTimer = null;
        }

        if (chartLine != null)
        {
            chartLine.generateVisualContent -= OnGenerateChartVisual;
        }

        selectedStock = null;
        selectedStockId = null;
    }
}
