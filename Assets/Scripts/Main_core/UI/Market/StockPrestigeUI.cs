using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 株式周回（プレステージ）情報を表示するUI
/// </summary>
public class StockPrestigeUI : MonoBehaviour
{
    // ========================================
    // 参照
    // ========================================
    [Header("パネル参照")]
    [SerializeField] private GameObject prestigePanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject stockPrestigeItemPrefab;

    [Header("サマリー表示")]
    [SerializeField] private TextMeshProUGUI totalBonusText;
    [SerializeField] private TextMeshProUGUI totalPrestigeLevelText;

    [Header("買収完了ポップアップ")]
    [SerializeField] private GameObject acquisitionPopup;
    [SerializeField] private TextMeshProUGUI acquisitionTitleText;
    [SerializeField] private TextMeshProUGUI acquisitionMessageText;
    [SerializeField] private TextMeshProUGUI acquisitionBonusText;
    [SerializeField] private Button acquisitionCloseButton;
    [SerializeField] private float popupAutoCloseTime = 5f;

    // ========================================
    // ランタイム
    // ========================================
    private List<StockPrestigeItemUI> itemUIs = new();
    private float popupTimer;

    // ========================================
    // 初期化
    // ========================================

    private void Start()
    {
        // イベント購読
        if (StockPrestigeManager.Instance != null)
        {
            StockPrestigeManager.Instance.OnAcquisitionComplete += OnAcquisitionComplete;
            StockPrestigeManager.Instance.OnPrestigeDataChanged += RefreshUI;
        }

        // 閉じるボタン
        if (acquisitionCloseButton != null)
        {
            acquisitionCloseButton.onClick.AddListener(CloseAcquisitionPopup);
        }

        // 初期非表示
        if (acquisitionPopup != null)
        {
            acquisitionPopup.SetActive(false);
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (StockPrestigeManager.Instance != null)
        {
            StockPrestigeManager.Instance.OnAcquisitionComplete -= OnAcquisitionComplete;
            StockPrestigeManager.Instance.OnPrestigeDataChanged -= RefreshUI;
        }
    }

    private void Update()
    {
        // ポップアップ自動クローズ
        if (acquisitionPopup != null && acquisitionPopup.activeSelf)
        {
            popupTimer -= Time.deltaTime;
            if (popupTimer <= 0)
            {
                CloseAcquisitionPopup();
            }
        }
    }

    // ========================================
    // UI更新
    // ========================================

    public void RefreshUI()
    {
        if (StockPrestigeManager.Instance == null) return;

        var summaries = StockPrestigeManager.Instance.GetPrestigeSummaries();

        // アイテムUIを更新
        UpdateItemList(summaries);

        // サマリー更新
        UpdateSummary();
    }

    private void UpdateItemList(List<PrestigeSummary> summaries)
    {
        // 既存のアイテムをクリア
        foreach (var item in itemUIs)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        itemUIs.Clear();

        if (contentParent == null || stockPrestigeItemPrefab == null) return;

        // 新しいアイテムを生成
        foreach (var summary in summaries)
        {
            var go = Instantiate(stockPrestigeItemPrefab, contentParent);
            var itemUI = go.GetComponent<StockPrestigeItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(summary);
                itemUIs.Add(itemUI);
            }
        }
    }

    private void UpdateSummary()
    {
        if (StockPrestigeManager.Instance == null) return;

        // 総周回レベル
        int totalLevel = 0;
        var summaries = StockPrestigeManager.Instance.GetPrestigeSummaries();
        foreach (var s in summaries)
        {
            totalLevel += s.prestigeLevel;
        }

        if (totalPrestigeLevelText != null)
        {
            totalPrestigeLevelText.text = $"総周回: {totalLevel}";
        }

        // 総ボーナス（クリック効率の例）
        if (totalBonusText != null)
        {
            float clickBonus = StockPrestigeManager.Instance.GetTotalPrestigeBonus(PrestigeBonusType.ClickEfficiency);
            float incomeBonus = StockPrestigeManager.Instance.GetTotalPrestigeBonus(PrestigeBonusType.AutoIncome);
            float critBonus = StockPrestigeManager.Instance.GetTotalPrestigeBonus(PrestigeBonusType.CriticalRate);

            var bonusLines = new List<string>();
            if (clickBonus > 0) bonusLines.Add($"クリック +{clickBonus * 100:F1}%");
            if (incomeBonus > 0) bonusLines.Add($"自動収入 +{incomeBonus * 100:F1}%");
            if (critBonus > 0) bonusLines.Add($"クリティカル +{critBonus * 100:F1}%");

            totalBonusText.text = bonusLines.Count > 0
                ? string.Join("\n", bonusLines)
                : "ボーナスなし";
        }
    }

    // ========================================
    // 買収完了ポップアップ
    // ========================================

    private void OnAcquisitionComplete(string stockId, int newLevel)
    {
        ShowAcquisitionPopup(stockId, newLevel);
        RefreshUI();
    }

    public void ShowAcquisitionPopup(string stockId, int prestigeLevel)
    {
        if (acquisitionPopup == null) return;

        var prestigeData = StockPrestigeManager.Instance?.GetPrestigeData(stockId);
        string companyName = prestigeData?.targetStock?.companyName ?? stockId;

        // タイトル
        if (acquisitionTitleText != null)
        {
            acquisitionTitleText.text = "買収完了！";
        }

        // メッセージ
        if (acquisitionMessageText != null)
        {
            acquisitionMessageText.text = $"{companyName} を完全買収しました！\n周回レベル: {prestigeLevel}";
        }

        // ボーナス表示
        if (acquisitionBonusText != null && prestigeData != null)
        {
            var bonusLines = new List<string>();
            bonusLines.Add("【獲得ボーナス】");

            foreach (var bonus in prestigeData.prestigeBonuses)
            {
                float totalValue = bonus.valuePerLevel * prestigeLevel;
                bonusLines.Add($"  {bonus.GetDisplayText(prestigeLevel)}");
            }

            // 次周回の難易度
            long nextShares = prestigeData.CalculateTotalShares(prestigeLevel);
            bonusLines.Add("");
            bonusLines.Add($"次周回の発行株数: {nextShares:N0}");

            acquisitionBonusText.text = string.Join("\n", bonusLines);
        }

        // 表示
        acquisitionPopup.SetActive(true);
        popupTimer = popupAutoCloseTime;
    }

    public void CloseAcquisitionPopup()
    {
        if (acquisitionPopup != null)
        {
            acquisitionPopup.SetActive(false);
        }
    }

    // ========================================
    // パネル開閉
    // ========================================

    public void TogglePanel()
    {
        if (prestigePanel != null)
        {
            prestigePanel.SetActive(!prestigePanel.activeSelf);
            if (prestigePanel.activeSelf)
            {
                RefreshUI();
            }
        }
    }

    public void OpenPanel()
    {
        if (prestigePanel != null)
        {
            prestigePanel.SetActive(true);
            RefreshUI();
        }
    }

    public void ClosePanel()
    {
        if (prestigePanel != null)
        {
            prestigePanel.SetActive(false);
        }
    }
}
