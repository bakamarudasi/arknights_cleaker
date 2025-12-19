using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ガチャバナーの表示と切り替えを担当するコントローラー
/// 単一責任: バナー情報の表示と切り替え
/// </summary>
public class GachaBannerController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private VisualElement bannerImage;
    private Label bannerNameLabel;
    private Label bannerDescLabel;
    private Label costLabel;
    private Label pityLabel;
    private Label currencyLabel;
    private Label progressLabel;
    private VisualElement bannerIndicator;
    private Button prevBannerBtn;
    private Button nextBannerBtn;
    private VisualElement lockOverlay;
    private Label lockReasonLabel;

    // ========================================
    // データ
    // ========================================

    private List<GachaBannerData> bannerList = new();
    private int currentBannerIndex = 0;

    // ========================================
    // イベント
    // ========================================

    /// <summary>バナーが変更された時に発火</summary>
    public event Action<GachaBannerData> OnBannerChanged;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// コントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement rootElement, List<GachaBannerData> banners)
    {
        root = rootElement;
        bannerList = banners ?? new List<GachaBannerData>();

        QueryElements();
        BindButtons();
        BuildBannerIndicator();
    }

    private void QueryElements()
    {
        bannerImage = root.Q<VisualElement>("banner-image");
        bannerNameLabel = root.Q<Label>("banner-name");
        bannerDescLabel = root.Q<Label>("banner-desc");
        costLabel = root.Q<Label>("cost-label");
        pityLabel = root.Q<Label>("pity-label");
        currencyLabel = root.Q<Label>("currency-label");
        progressLabel = root.Q<Label>("progress-label");
        bannerIndicator = root.Q<VisualElement>("banner-indicator");
        prevBannerBtn = root.Q<Button>("btn-prev-banner");
        nextBannerBtn = root.Q<Button>("btn-next-banner");
    }

    private void BindButtons()
    {
        prevBannerBtn?.RegisterCallback<ClickEvent>(_ => ChangeBanner(-1));
        nextBannerBtn?.RegisterCallback<ClickEvent>(_ => ChangeBanner(1));
    }

    // ========================================
    // バナー切り替え
    // ========================================

    private void ChangeBanner(int direction)
    {
        if (bannerList == null || bannerList.Count <= 1) return;

        currentBannerIndex += direction;
        if (currentBannerIndex < 0) currentBannerIndex = bannerList.Count - 1;
        if (currentBannerIndex >= bannerList.Count) currentBannerIndex = 0;

        RefreshBannerDisplay();
        OnBannerChanged?.Invoke(GetCurrentBanner());
    }

    /// <summary>
    /// 現在のバナーを取得
    /// </summary>
    public GachaBannerData GetCurrentBanner()
    {
        if (bannerList == null || bannerList.Count == 0) return null;
        currentBannerIndex = Mathf.Clamp(currentBannerIndex, 0, bannerList.Count - 1);
        return bannerList[currentBannerIndex];
    }

    // ========================================
    // 表示更新
    // ========================================

    /// <summary>
    /// バナー表示を更新
    /// </summary>
    public void RefreshBannerDisplay()
    {
        var banner = GetCurrentBanner();
        if (banner == null) return;

        bool isLocked = !GachaManager.Instance.IsBannerUnlocked(banner);

        // バナー情報
        if (bannerNameLabel != null) bannerNameLabel.text = banner.bannerName;
        if (bannerDescLabel != null) bannerDescLabel.text = banner.description;

        // バナー画像
        if (bannerImage != null && banner.bannerSprite != null)
        {
            bannerImage.style.backgroundImage = new StyleBackground(banner.bannerSprite);
        }

        // 限定バナー
        bannerNameLabel?.EnableInClassList("limited-banner", banner.isLimited);

        // 各種表示
        UpdateCostDisplay(banner);
        UpdatePityDisplay(banner);
        UpdateProgressDisplay(banner);
        UpdateCurrencyDisplay();
        UpdateLockDisplay(banner, isLocked);
        UpdateBannerIndicator();

        // ナビゲーションボタン
        bool showNav = bannerList.Count > 1;
        prevBannerBtn?.SetEnabled(showNav);
        nextBannerBtn?.SetEnabled(showNav);
    }

    private void UpdateCostDisplay(GachaBannerData banner)
    {
        if (costLabel == null) return;

        string currencyName = GetCurrencyName(banner.currencyType);
        costLabel.text = $"1回: {banner.costSingle:N0} {currencyName} | 10回: {banner.costTen:N0} {currencyName}";

        AdjustFontSizeToFit(costLabel, 20, 12, 30);
    }

    /// <summary>
    /// 天井表示を更新
    /// </summary>
    public void UpdatePityDisplay(GachaBannerData banner = null)
    {
        if (pityLabel == null) return;
        banner ??= GetCurrentBanner();
        if (banner == null) return;

        if (banner.hasPity)
        {
            int currentPity = GachaManager.Instance.GetPityCount(banner.bannerId);
            pityLabel.text = $"天井まで: {currentPity} / {banner.pityCount}";
            pityLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            pityLabel.style.display = DisplayStyle.None;
        }
    }

    private void UpdateProgressDisplay(GachaBannerData banner)
    {
        if (progressLabel == null)
        {
            progressLabel = root.Q<Label>("progress-label");
        }
        if (progressLabel == null) return;

        var (owned, total) = GachaManager.Instance.GetPoolProgress(banner);

        if (total > 0 && owned < total)
        {
            progressLabel.text = $"コンプリート: {owned} / {total}";
            progressLabel.style.display = DisplayStyle.Flex;
        }
        else if (total > 0 && owned >= total)
        {
            progressLabel.text = "★ コンプリート！ ★";
            progressLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            progressLabel.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// 通貨表示を更新
    /// </summary>
    public void UpdateCurrencyDisplay()
    {
        if (currencyLabel == null) return;

        var banner = GetCurrentBanner();
        if (banner == null) return;

        var gc = GameController.Instance;
        double balance = banner.currencyType switch
        {
            CurrencyType.LMD => gc.Wallet.Money,
            CurrencyType.Certificate => gc.Wallet.Certificates,
            _ => 0
        };

        string currencyName = GetCurrencyName(banner.currencyType);
        currencyLabel.text = $"所持: {balance:N0} {currencyName}";

        AdjustFontSizeToFit(currencyLabel, 26, 14, 20);
    }

    private void UpdateLockDisplay(GachaBannerData banner, bool isLocked)
    {
        // ロックオーバーレイの動的生成
        if (lockOverlay == null && bannerImage != null)
        {
            lockOverlay = new VisualElement();
            lockOverlay.name = "lock-overlay";
            lockOverlay.AddToClassList("lock-overlay");

            lockReasonLabel = new Label();
            lockReasonLabel.name = "lock-reason";
            lockReasonLabel.AddToClassList("lock-reason");
            lockOverlay.Add(lockReasonLabel);

            bannerImage.parent?.Add(lockOverlay);
        }

        if (lockOverlay == null) return;

        if (isLocked)
        {
            lockOverlay.style.display = DisplayStyle.Flex;
            string reason = GetUnlockReasonText(banner);
            if (lockReasonLabel != null) lockReasonLabel.text = reason;
        }
        else
        {
            lockOverlay.style.display = DisplayStyle.None;
        }
    }

    // ========================================
    // インジケーター
    // ========================================

    private void BuildBannerIndicator()
    {
        if (bannerIndicator == null) return;
        bannerIndicator.Clear();

        for (int i = 0; i < bannerList.Count; i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("indicator-dot");
            bannerIndicator.Add(dot);
        }

        UpdateBannerIndicator();
    }

    private void UpdateBannerIndicator()
    {
        if (bannerIndicator == null) return;

        for (int i = 0; i < bannerIndicator.childCount; i++)
        {
            var dot = bannerIndicator[i];
            if (i == currentBannerIndex)
                dot.AddToClassList("active");
            else
                dot.RemoveFromClassList("active");
        }
    }

    // ========================================
    // ヘルパー
    // ========================================

    private string GetCurrencyName(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.LMD => "龍門幣",
            CurrencyType.Certificate => "資格証",
            _ => "???"
        };
    }

    private string GetUnlockReasonText(GachaBannerData banner)
    {
        if (banner.prerequisiteBanner != null)
        {
            var (owned, total) = GachaManager.Instance.GetPoolProgress(banner.prerequisiteBanner);
            return $"🔒 解放条件\n「{banner.prerequisiteBanner.bannerName}」\nをコンプリート\n({owned}/{total})";
        }

        if (banner.requiredUnlockItem != null)
        {
            return $"🔒 解放条件\n「{banner.requiredUnlockItem.displayName}」\nを入手";
        }

        return "🔒 ロック中";
    }

    private void AdjustFontSizeToFit(Label label, int maxSize, int minSize, int thresholdLength)
    {
        if (label == null) return;

        int textLength = label.text?.Length ?? 0;

        if (textLength <= thresholdLength)
        {
            label.style.fontSize = maxSize;
        }
        else
        {
            float ratio = (float)thresholdLength / textLength;
            int newSize = Mathf.Clamp(Mathf.RoundToInt(maxSize * ratio), minSize, maxSize);
            label.style.fontSize = newSize;
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        OnBannerChanged = null;
        bannerList.Clear();
    }
}
