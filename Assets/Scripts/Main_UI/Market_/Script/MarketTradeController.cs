using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// 売買パネルを担当するコントローラ
/// 数量入力、購入/売却ボタンの制御
/// </summary>
public class MarketTradeController : IDisposable
{
    // ========================================
    // 依存
    // ========================================
    private readonly IMarketFacade facade;
    private readonly VisualElement root;

    // ========================================
    // UI要素
    // ========================================
    private TextField tradeQuantityInput;
    private Button buyButton;
    private Button sellButton;
    private Button qty10Btn;
    private Button qty100Btn;
    private Button qtyMaxBtn;

    // ========================================
    // 状態
    // ========================================
    private string selectedStockId;

    // ========================================
    // イベント
    // ========================================
    public event Action<bool> OnTradeExecuted; // true = buy, false = sell
    public event Action<bool> OnLossCutExecuted; // 損切り実行時

    // ========================================
    // 初期化
    // ========================================

    public MarketTradeController(VisualElement root, IMarketFacade facade)
    {
        this.root = root;
        this.facade = facade;

        QueryElements();
        BindUIEvents();
    }

    private void QueryElements()
    {
        tradeQuantityInput = root.Q<TextField>("trade-quantity");
        buyButton = root.Q<Button>("buy-btn");
        sellButton = root.Q<Button>("sell-btn");
        qty10Btn = root.Q<Button>("qty-10");
        qty100Btn = root.Q<Button>("qty-100");
        qtyMaxBtn = root.Q<Button>("qty-max");
    }

    private void BindUIEvents()
    {
        if (qty10Btn != null) qty10Btn.clicked += () => SetTradeQuantity(10);
        if (qty100Btn != null) qty100Btn.clicked += () => SetTradeQuantity(100);
        if (qtyMaxBtn != null) qtyMaxBtn.clicked += SetMaxQuantity;

        if (buyButton != null) buyButton.clicked += OnBuyClicked;
        if (sellButton != null) sellButton.clicked += OnSellClicked;

        // 数量入力時にボタン状態を更新
        if (tradeQuantityInput != null)
        {
            tradeQuantityInput.RegisterValueChangedCallback(evt => UpdateTradeButtons());
        }
    }

    // ========================================
    // 銘柄選択
    // ========================================

    public void SetSelectedStock(string stockId)
    {
        selectedStockId = stockId;
        UpdateTradeButtons();
    }

    // ========================================
    // 数量操作
    // ========================================

    public void SetTradeQuantity(int qty)
    {
        if (tradeQuantityInput != null)
        {
            tradeQuantityInput.value = qty.ToString();
        }
        UpdateTradeButtons();
    }

    public void SetMaxQuantity()
    {
        if (selectedStockId == null) return;

        int max = facade.GetMaxBuyableQuantity(selectedStockId);
        SetTradeQuantity(max);
    }

    public int GetTradeQuantity()
    {
        if (tradeQuantityInput == null)
        {
            Debug.LogWarning("[Trade] tradeQuantityInput is null!");
            return 0;
        }

        string rawValue = tradeQuantityInput.value;

        if (int.TryParse(rawValue, out int qty))
        {
            return qty;
        }
        else
        {
            Debug.LogWarning($"[Trade] Failed to parse '{rawValue}' as int");
            return 0;
        }
    }

    // ========================================
    // ボタン状態更新
    // ========================================

    public void UpdateTradeButtons()
    {
        if (selectedStockId == null) return;

        int qty = GetTradeQuantity();
        int holdings = facade.GetHoldingQuantity(selectedStockId);
        int maxBuyable = facade.GetMaxBuyableQuantity(selectedStockId);

        // デバッグ用ログ

        // 購入ボタン
        if (buyButton != null)
        {
            bool canBuy = qty > 0 && qty <= maxBuyable;
            buyButton.SetEnabled(canBuy);
        }

        // 売却ボタン
        if (sellButton != null)
        {
            bool canSell = qty > 0 && qty <= holdings;
            sellButton.SetEnabled(canSell);

            // 利確/損切りモード表示
            bool hasProfit = facade.HasProfit(selectedStockId);
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

    // ========================================
    // 売買実行
    // ========================================

    private void OnBuyClicked()
    {
        if (selectedStockId == null) return;

        int qty = GetTradeQuantity();
        if (qty <= 0) return;

        bool success = facade.TryBuyStock(selectedStockId, qty);
        if (success)
        {
            OnTradeExecuted?.Invoke(true);
            LogUIController.Msg($"📈 {selectedStockId} を {qty} 株購入");
        }
    }

    private void OnSellClicked()
    {
        if (selectedStockId == null) return;

        int qty = GetTradeQuantity();
        if (qty <= 0) return;

        bool hasLoss = facade.HasLoss(selectedStockId);
        bool success = facade.TrySellStock(selectedStockId, qty);

        if (success)
        {
            OnTradeExecuted?.Invoke(false);

            if (hasLoss)
            {
                OnLossCutExecuted?.Invoke(true);
            }
        }
    }

    // ========================================
    // 破棄
    // ========================================

    public void Dispose()
    {
        if (qty10Btn != null) qty10Btn.clicked -= () => SetTradeQuantity(10);
        if (qty100Btn != null) qty100Btn.clicked -= () => SetTradeQuantity(100);
        if (qtyMaxBtn != null) qtyMaxBtn.clicked -= SetMaxQuantity;

        if (buyButton != null) buyButton.clicked -= OnBuyClicked;
        if (sellButton != null) sellButton.clicked -= OnSellClicked;

        selectedStockId = null;
    }
}
