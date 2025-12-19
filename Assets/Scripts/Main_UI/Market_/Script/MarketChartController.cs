using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;

/// <summary>
/// チャート表示を担当するコントローラ
/// 価格チャート描画、統計情報表示を管理
/// </summary>
public class MarketChartController : IDisposable
{
    // ========================================
    // 依存
    // ========================================
    private readonly IMarketFacade facade;
    private readonly VisualElement root;

    // ========================================
    // UI要素
    // ========================================
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

    // ========================================
    // 状態
    // ========================================
    private string selectedStockId;
    private StockData selectedStock;
    private bool isInsiderActive;

    // ========================================
    // イベント
    // ========================================
    public event Action<string> OnStockSelected;

    // ========================================
    // 初期化
    // ========================================

    public MarketChartController(VisualElement root, IMarketFacade facade)
    {
        this.root = root;
        this.facade = facade;

        QueryElements();
        BindChartDrawing();
    }

    private void QueryElements()
    {
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
    }

    private void BindChartDrawing()
    {
        if (chartLine != null)
        {
            chartLine.generateVisualContent -= OnGenerateChartVisual;
            chartLine.generateVisualContent += OnGenerateChartVisual;
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

        OnStockSelected?.Invoke(selectedStockId);
    }

    public void SelectStock(string stockId)
    {
        var stock = facade.GetStockData(stockId);
        if (stock != null)
        {
            SelectStock(stock);
        }
    }

    public string SelectedStockId => selectedStockId;
    public StockData SelectedStock => selectedStock;

    // ========================================
    // インサイダーモード
    // ========================================

    public void SetInsiderActive(bool active)
    {
        isInsiderActive = active;
        RequestRepaint();
    }

    // ========================================
    // 更新
    // ========================================

    public void OnPriceUpdated(StockPriceSnapshot snapshot)
    {
        if (snapshot.stockId == selectedStockId)
        {
            UpdatePriceDisplay(snapshot.price, snapshot.changeRate);
            RefreshChartStats();
        }
    }

    public void RequestRepaint()
    {
        chartLine?.MarkDirtyRepaint();
    }

    private void RefreshChartHeader()
    {
        if (selectedStock == null) return;

        if (chartStockCode != null) chartStockCode.text = selectedStock.stockId;
        if (chartStockName != null) chartStockName.text = selectedStock.companyName;

        var state = facade.GetStockState(selectedStockId);
        if (state != null)
        {
            UpdatePriceDisplay(state.currentPrice, state.ChangeRate);
        }
    }

    private void RefreshChartStats()
    {
        if (selectedStock == null) return;

        var state = facade.GetStockState(selectedStockId);
        if (state == null) return;

        if (statOpen != null) statOpen.text = facade.FormatPrice(state.openPrice);
        if (statHigh != null) statHigh.text = facade.FormatPrice(state.highPrice);
        if (statLow != null) statLow.text = facade.FormatPrice(state.lowPrice);
        if (statVolatility != null) statVolatility.text = $"{selectedStock.volatility:F2}";
    }

    private void UpdatePriceDisplay(double price, double changeRate)
    {
        if (chartCurrentPrice != null)
        {
            chartCurrentPrice.text = facade.FormatPrice(price);
            chartCurrentPrice.RemoveFromClassList("negative");
            if (changeRate < 0) chartCurrentPrice.AddToClassList("negative");
        }

        if (chartChange != null)
        {
            chartChange.text = facade.FormatChangeRate(changeRate);
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

        var history = facade.GetPriceHistory(selectedStockId);
        if (history == null || history.Length < 2) return;

        var painter = ctx.painter2D;
        var rect = chartLine.contentRect;

        if (rect.width <= 0 || rect.height <= 0) return;

        // 価格の範囲を計算
        double minPrice = history.Min();
        double maxPrice = history.Max();
        double priceRange = maxPrice - minPrice;
        if (priceRange < 0.01) priceRange = 1;

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

        // グラデーション塗りつぶし
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
        double lastPrice = history[^1];
        int futurePoints = 10;

        painter.strokeColor = new Color(0.66f, 0.33f, 0.97f, 0.6f);
        painter.lineWidth = 1.5f;

        float startX = padding + chartWidth;
        float startY = padding + (float)((maxPrice - lastPrice) / priceRange) * chartHeight;

        double predictedPrice = lastPrice;
        for (int i = 1; i <= futurePoints; i++)
        {
            predictedPrice *= (1 + selectedStock.drift * 0.001f);

            float x = startX + (i * 5f);
            float y = padding + (float)((maxPrice - predictedPrice) / priceRange) * chartHeight;
            y = Mathf.Clamp(y, padding, padding + chartHeight);

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
    // 破棄
    // ========================================

    public void Dispose()
    {
        if (chartLine != null)
        {
            chartLine.generateVisualContent -= OnGenerateChartVisual;
        }

        selectedStock = null;
        selectedStockId = null;
    }
}
