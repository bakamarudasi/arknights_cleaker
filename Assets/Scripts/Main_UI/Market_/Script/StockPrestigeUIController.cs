using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// 株式周回（プレステージ）情報を表示するUIコントローラ
/// UI Toolkit版
/// </summary>
public class StockPrestigeUIController : IViewController
{
    // ========================================
    // UI要素
    // ========================================
    private VisualElement root;
    private VisualElement prestigePanel;
    private VisualElement prestigeList;

    // サマリー
    private Label totalPrestigeLevelLabel;
    private Label totalBonusLabel;

    // 買収完了ポップアップ
    private VisualElement acquisitionPopup;
    private Label acquisitionTitleLabel;
    private Label acquisitionMessageLabel;
    private Label acquisitionBonusLabel;
    private Button acquisitionCloseButton;

    // ========================================
    // 状態
    // ========================================
    private IVisualElementScheduledItem popupTimer;
    private const float POPUP_DURATION = 5000f; // ミリ秒

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        this.root = root;

        QueryElements();
        BindEvents();
        RefreshUI();
    }

    private void QueryElements()
    {
        // パネル
        prestigePanel = root.Q<VisualElement>("prestige-panel");
        prestigeList = root.Q<VisualElement>("prestige-list");

        // サマリー
        totalPrestigeLevelLabel = root.Q<Label>("total-prestige-level");
        totalBonusLabel = root.Q<Label>("total-prestige-bonus");

        // ポップアップ
        acquisitionPopup = root.Q<VisualElement>("acquisition-popup");
        acquisitionTitleLabel = root.Q<Label>("acquisition-title");
        acquisitionMessageLabel = root.Q<Label>("acquisition-message");
        acquisitionBonusLabel = root.Q<Label>("acquisition-bonus");
        acquisitionCloseButton = root.Q<Button>("acquisition-close-btn");

        // 初期非表示
        if (acquisitionPopup != null)
        {
            acquisitionPopup.style.display = DisplayStyle.None;
        }
    }

    private void BindEvents()
    {
        // 閉じるボタン
        if (acquisitionCloseButton != null)
        {
            acquisitionCloseButton.clicked += CloseAcquisitionPopup;
        }

        // StockPrestigeManagerのイベント購読
        if (StockPrestigeManager.Instance != null)
        {
            StockPrestigeManager.Instance.OnAcquisitionComplete += OnAcquisitionComplete;
            StockPrestigeManager.Instance.OnPrestigeDataChanged += RefreshUI;
        }
    }

    // ========================================
    // UI更新
    // ========================================

    public void RefreshUI()
    {
        if (StockPrestigeManager.Instance == null) return;

        RefreshSummary();
        RefreshPrestigeList();
    }

    private void RefreshSummary()
    {
        var summaries = StockPrestigeManager.Instance.GetPrestigeSummaries();

        // 総周回レベル
        int totalLevel = 0;
        foreach (var s in summaries)
        {
            totalLevel += s.prestigeLevel;
        }

        if (totalPrestigeLevelLabel != null)
        {
            totalPrestigeLevelLabel.text = $"総周回: {totalLevel}";
        }

        // 総ボーナス
        if (totalBonusLabel != null)
        {
            var lines = new List<string>();

            float clickBonus = StockPrestigeManager.Instance.GetTotalPrestigeBonus(PrestigeBonusType.ClickEfficiency);
            float incomeBonus = StockPrestigeManager.Instance.GetTotalPrestigeBonus(PrestigeBonusType.AutoIncome);
            float critBonus = StockPrestigeManager.Instance.GetTotalPrestigeBonus(PrestigeBonusType.CriticalRate);

            if (clickBonus > 0) lines.Add($"クリック +{clickBonus * 100:F1}%");
            if (incomeBonus > 0) lines.Add($"自動収入 +{incomeBonus * 100:F1}%");
            if (critBonus > 0) lines.Add($"クリティカル +{critBonus * 100:F1}%");

            totalBonusLabel.text = lines.Count > 0
                ? string.Join(" / ", lines)
                : "ボーナスなし";
        }
    }

    private void RefreshPrestigeList()
    {
        if (prestigeList == null) return;

        prestigeList.Clear();

        var summaries = StockPrestigeManager.Instance.GetPrestigeSummaries();

        foreach (var summary in summaries)
        {
            var item = CreatePrestigeItem(summary);
            prestigeList.Add(item);
        }
    }

    private VisualElement CreatePrestigeItem(PrestigeSummary summary)
    {
        var item = new VisualElement();
        item.AddToClassList("prestige-item");

        if (summary.isMaxLevel)
        {
            item.AddToClassList("max-level");
        }

        // ヘッダー（企業名 + 周回レベル）
        var header = new VisualElement();
        header.AddToClassList("prestige-item-header");

        var nameLabel = new Label { text = summary.companyName };
        nameLabel.AddToClassList("prestige-company-name");

        var levelLabel = new Label
        {
            text = summary.prestigeLevel > 0 ? $"Lv.{summary.prestigeLevel}" : "未周回"
        };
        levelLabel.AddToClassList("prestige-level");
        if (summary.prestigeLevel > 0)
        {
            levelLabel.AddToClassList("active");
        }

        header.Add(nameLabel);
        header.Add(levelLabel);

        // 発行株数
        var sharesLabel = new Label { text = $"発行株数: {summary.currentTotalShares:N0}" };
        sharesLabel.AddToClassList("prestige-shares");

        // 保有率プログレス
        var progressContainer = new VisualElement();
        progressContainer.AddToClassList("prestige-progress-container");

        float holdingRate = StockPrestigeManager.Instance?.GetHoldingRate(summary.stockId) ?? 0f;

        var progressBar = new VisualElement();
        progressBar.AddToClassList("prestige-progress-bar");

        var progressFill = new VisualElement();
        progressFill.AddToClassList("prestige-progress-fill");
        progressFill.style.width = new Length(holdingRate * 100, LengthUnit.Percent);

        progressBar.Add(progressFill);

        var progressLabel = new Label { text = $"{holdingRate * 100:F1}%" };
        progressLabel.AddToClassList("prestige-progress-text");

        progressContainer.Add(progressBar);
        progressContainer.Add(progressLabel);

        // ボーナス表示
        var bonusContainer = new VisualElement();
        bonusContainer.AddToClassList("prestige-bonus-container");

        if (summary.prestigeLevel > 0)
        {
            var prestigeData = StockPrestigeManager.Instance.GetPrestigeData(summary.stockId);
            if (prestigeData != null)
            {
                foreach (var bonus in prestigeData.prestigeBonuses)
                {
                    var bonusLabel = new Label { text = bonus.GetDisplayText(summary.prestigeLevel) };
                    bonusLabel.AddToClassList("prestige-bonus-item");
                    bonusContainer.Add(bonusLabel);
                }
            }
        }

        // 最大レベルバッジ
        if (summary.isMaxLevel)
        {
            var maxBadge = new Label { text = "MAX" };
            maxBadge.AddToClassList("prestige-max-badge");
            header.Add(maxBadge);
        }

        item.Add(header);
        item.Add(sharesLabel);
        item.Add(progressContainer);
        item.Add(bonusContainer);

        return item;
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
        if (acquisitionTitleLabel != null)
        {
            acquisitionTitleLabel.text = "🎉 買収完了！";
        }

        // メッセージ
        if (acquisitionMessageLabel != null)
        {
            acquisitionMessageLabel.text = $"{companyName} を完全買収しました！\n周回レベル: {prestigeLevel}";
        }

        // ボーナス
        if (acquisitionBonusLabel != null && prestigeData != null)
        {
            var lines = new List<string>();
            lines.Add("【獲得ボーナス】");

            foreach (var bonus in prestigeData.prestigeBonuses)
            {
                lines.Add(bonus.GetDisplayText(prestigeLevel));
            }

            long nextShares = prestigeData.CalculateTotalShares(prestigeLevel);
            lines.Add("");
            lines.Add($"次周回の発行株数: {nextShares:N0}");

            acquisitionBonusLabel.text = string.Join("\n", lines);
        }

        // 表示
        acquisitionPopup.style.display = DisplayStyle.Flex;
        acquisitionPopup.AddToClassList("visible");

        // 自動クローズ
        popupTimer?.Pause();
        popupTimer = root.schedule.Execute(CloseAcquisitionPopup);
        popupTimer.ExecuteLater((long)POPUP_DURATION);
    }

    public void CloseAcquisitionPopup()
    {
        if (acquisitionPopup != null)
        {
            acquisitionPopup.RemoveFromClassList("visible");
            acquisitionPopup.style.display = DisplayStyle.None;
        }

        popupTimer?.Pause();
        popupTimer = null;
    }

    // ========================================
    // パネル開閉
    // ========================================

    public void TogglePanel()
    {
        if (prestigePanel == null) return;

        bool isVisible = prestigePanel.ClassListContains("visible");

        if (isVisible)
        {
            prestigePanel.RemoveFromClassList("visible");
        }
        else
        {
            prestigePanel.AddToClassList("visible");
            RefreshUI();
        }
    }

    public void OpenPanel()
    {
        prestigePanel?.AddToClassList("visible");
        RefreshUI();
    }

    public void ClosePanel()
    {
        prestigePanel?.RemoveFromClassList("visible");
    }

    // ========================================
    // 破棄
    // ========================================

    public void Dispose()
    {
        if (StockPrestigeManager.Instance != null)
        {
            StockPrestigeManager.Instance.OnAcquisitionComplete -= OnAcquisitionComplete;
            StockPrestigeManager.Instance.OnPrestigeDataChanged -= RefreshUI;
        }

        popupTimer?.Pause();
        popupTimer = null;
    }
}
