using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// スキルパネルを担当するコントローラ
/// 物理買い支え、インサイダーなどのスキル制御
/// </summary>
public class MarketSkillController : IDisposable
{
    // ========================================
    // 依存
    // ========================================
    private readonly IMarketFacade facade;
    private readonly VisualElement root;

    // ========================================
    // UI要素
    // ========================================
    private Button skillBuySupport;
    private Button skillInsider;
    private Label skillCooldownLabel;

    // ========================================
    // 定数
    // ========================================
    private const float BUY_SUPPORT_COOLDOWN = 10f;
    private const float INSIDER_COOLDOWN = 60f;
    private const float INSIDER_DURATION = 5f;

    // ========================================
    // 状態
    // ========================================
    private string selectedStockId;
    private float buySupportCooldown;
    private float insiderCooldown;
    private bool isInsiderActive;

    // ========================================
    // イベント
    // ========================================
    public event Action OnBuySupportUsed;
    public event Action<bool> OnInsiderStateChanged; // true = 開始, false = 終了

    // ========================================
    // プロパティ
    // ========================================
    public bool IsInsiderActive => isInsiderActive;

    // ========================================
    // 初期化
    // ========================================

    public MarketSkillController(VisualElement root, IMarketFacade facade)
    {
        this.root = root;
        this.facade = facade;

        QueryElements();
        BindUIEvents();
    }

    private void QueryElements()
    {
        skillBuySupport = root.Q<Button>("skill-buy-support");
        skillInsider = root.Q<Button>("skill-insider");
        skillCooldownLabel = root.Q<Label>("skill-cooldown");
    }

    private void BindUIEvents()
    {
        if (skillBuySupport != null) skillBuySupport.clicked += OnBuySupportClicked;
        if (skillInsider != null) skillInsider.clicked += OnInsiderClicked;
    }

    // ========================================
    // 銘柄選択
    // ========================================

    public void SetSelectedStock(string stockId)
    {
        selectedStockId = stockId;
    }

    // ========================================
    // 更新ループ（毎フレーム呼び出し）
    // ========================================

    public void UpdateCooldowns(float deltaTime)
    {
        if (buySupportCooldown > 0)
        {
            buySupportCooldown -= deltaTime;
            if (skillBuySupport != null)
            {
                skillBuySupport.SetEnabled(buySupportCooldown <= 0);
            }
        }

        if (insiderCooldown > 0)
        {
            insiderCooldown -= deltaTime;
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
    // スキル実行
    // ========================================

    private void OnBuySupportClicked()
    {
        if (selectedStockId == null || buySupportCooldown > 0) return;

        // 物理買い支え（10クリック分の効果）
        facade.ApplyBuySupport(selectedStockId, 10);
        buySupportCooldown = BUY_SUPPORT_COOLDOWN;

        var stock = facade.GetStockData(selectedStockId);
        LogUIController.Msg($"📈 {stock?.companyName ?? selectedStockId} を物理買い支え！");

        OnBuySupportUsed?.Invoke();
    }

    private void OnInsiderClicked()
    {
        if (insiderCooldown > 0) return;

        isInsiderActive = true;
        insiderCooldown = INSIDER_COOLDOWN;

        LogUIController.Msg("👁️ インサイダー情報を入手... 数秒先が見える！");

        OnInsiderStateChanged?.Invoke(true);

        // 一定時間後に効果終了
        root.schedule.Execute(() =>
        {
            isInsiderActive = false;
            OnInsiderStateChanged?.Invoke(false);
        }).ExecuteLater((long)(INSIDER_DURATION * 1000));
    }

    // ========================================
    // 破棄
    // ========================================

    public void Dispose()
    {
        if (skillBuySupport != null) skillBuySupport.clicked -= OnBuySupportClicked;
        if (skillInsider != null) skillInsider.clicked -= OnInsiderClicked;

        selectedStockId = null;
    }
}
