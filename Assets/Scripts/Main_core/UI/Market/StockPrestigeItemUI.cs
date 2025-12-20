using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 周回情報リストの1アイテム
/// </summary>
public class StockPrestigeItemUI : MonoBehaviour
{
    // ========================================
    // 参照
    // ========================================
    [Header("基本情報")]
    [SerializeField] private Image logoImage;
    [SerializeField] private TextMeshProUGUI companyNameText;
    [SerializeField] private TextMeshProUGUI prestigeLevelText;

    [Header("詳細情報")]
    [SerializeField] private TextMeshProUGUI totalSharesText;
    [SerializeField] private TextMeshProUGUI bonusText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("状態表示")]
    [SerializeField] private GameObject maxLevelBadge;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color maxLevelColor = new Color(1f, 0.85f, 0.2f);

    // ========================================
    // データ
    // ========================================
    private string stockId;

    // ========================================
    // セットアップ
    // ========================================

    public void Setup(PrestigeSummary summary)
    {
        stockId = summary.stockId;

        // 企業名
        if (companyNameText != null)
        {
            companyNameText.text = summary.companyName;
        }

        // 周回レベル
        if (prestigeLevelText != null)
        {
            prestigeLevelText.text = summary.prestigeLevel > 0
                ? $"Lv.{summary.prestigeLevel}"
                : "未周回";
        }

        // 発行株数
        if (totalSharesText != null)
        {
            totalSharesText.text = $"発行株数: {summary.currentTotalShares:N0}";
        }

        // ボーナス表示
        UpdateBonusText(summary);

        // 進捗（現在の保有率）
        UpdateProgress(summary.stockId);

        // 最大レベルバッジ
        if (maxLevelBadge != null)
        {
            maxLevelBadge.SetActive(summary.isMaxLevel);
        }

        // 背景色
        if (backgroundImage != null)
        {
            backgroundImage.color = summary.isMaxLevel ? maxLevelColor : normalColor;
        }

        // ロゴ
        UpdateLogo(summary.stockId);
    }

    private void UpdateBonusText(PrestigeSummary summary)
    {
        if (bonusText == null) return;

        if (summary.prestigeLevel <= 0)
        {
            bonusText.text = "ボーナスなし";
            return;
        }

        var prestigeData = StockPrestigeManager.Instance?.GetPrestigeData(stockId);
        if (prestigeData == null || prestigeData.prestigeBonuses.Count == 0)
        {
            bonusText.text = "ボーナスなし";
            return;
        }

        // ボーナス一覧
        var lines = new System.Collections.Generic.List<string>();
        foreach (var bonus in prestigeData.prestigeBonuses)
        {
            lines.Add(bonus.GetDisplayText(summary.prestigeLevel));
        }

        bonusText.text = string.Join("\n", lines);
    }

    private void UpdateProgress(string stockId)
    {
        float holdingRate = StockHoldingBonusManager.Instance?.GetHoldingRate(stockId) ?? 0f;

        if (progressSlider != null)
        {
            progressSlider.value = holdingRate;
        }

        if (progressText != null)
        {
            progressText.text = $"{holdingRate * 100:F1}%";
        }
    }

    private void UpdateLogo(string stockId)
    {
        if (logoImage == null) return;

        var stockData = MarketManager.Instance?.GetStockData(stockId);
        if (stockData?.logo != null)
        {
            logoImage.sprite = stockData.logo;
            logoImage.enabled = true;
        }
        else
        {
            logoImage.enabled = false;
        }
    }

    // ========================================
    // 更新
    // ========================================

    private void OnEnable()
    {
        // 保有率の変更を監視
        if (StockHoldingBonusManager.Instance != null)
        {
            StockHoldingBonusManager.Instance.OnBonusesChanged += OnBonusesChanged;
        }
    }

    private void OnDisable()
    {
        if (StockHoldingBonusManager.Instance != null)
        {
            StockHoldingBonusManager.Instance.OnBonusesChanged -= OnBonusesChanged;
        }
    }

    private void OnBonusesChanged(string changedStockId, System.Collections.Generic.List<StockHoldingBonus> bonuses)
    {
        if (changedStockId == stockId)
        {
            UpdateProgress(stockId);
        }
    }
}
