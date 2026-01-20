using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ガチャ画面のUIファサード
/// 単一責任: 各サブコントローラーの統合と画面全体のライフサイクル管理
/// </summary>
public class GachaUIController : BaseUIController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement mainScreen;
    private VisualElement bagScreen;
    private VisualElement resultPanel;
    private Button pullSingleBtn;
    private Button pullTenBtn;
    private Button rateInfoBtn;
    private Button resultCloseBtn;
    private Button skipBtn;

    // ========================================
    // サブコントローラー（分離された責任）
    // ========================================

    private GachaBannerController bannerController;
    private GachaVisualEffectController visualController;
    private GachaResultAnimator resultAnimator;
    private ZipperManipulator zipper;

    // ========================================
    // データ
    // ========================================

    private GachaDatabase database;
    private List<GachaResultItem> pendingResults;
    private int maxRarityInResult = 3;

    // ========================================
    // 状態
    // ========================================

    private bool isAnimating = false;

    // ========================================
    // 初期化 (BaseUIControllerテンプレートメソッド)
    // ========================================

    protected override void OnPreInitialize()
    {
        this.database = GachaManager.Instance?.Database;

        if (database == null)
        {
            Debug.LogWarning($"{LogTag} GachaDatabase not found in GachaManager!");
        }
    }

    protected override void QueryElements()
    {
        mainScreen = root.Q<VisualElement>("main-screen");
        bagScreen = root.Q<VisualElement>("BagScreen");

        pullSingleBtn = root.Q<Button>("btn-pull-single");
        pullTenBtn = root.Q<Button>("btn-pull-ten");
        rateInfoBtn = root.Q<Button>("btn-rate-info");
        resultCloseBtn = root.Q<Button>("btn-result-close");
        skipBtn = root.Q<Button>("btn-skip");

        resultPanel = root.Q<VisualElement>("result-panel");
    }

    protected override void InitializeSubControllers()
    {
        if (database == null) return;

        // バナーコントローラー
        bannerController = new GachaBannerController();
        bannerController.Initialize(root, database.GetAllBanners());

        // ビジュアルエフェクトコントローラー
        visualController = new GachaVisualEffectController();
        var lightBeam = root.Q<VisualElement>("LightBeam");
        var ambientLight = root.Q<VisualElement>("AmbientLight");
        var particleContainer = root.Q<VisualElement>("ParticleContainer");
        visualController.Initialize(root, bagScreen, lightBeam, ambientLight, particleContainer);

        // 結果アニメーター
        resultAnimator = new GachaResultAnimator();
        var resultContainer = root.Q<VisualElement>("result-container");
        resultAnimator.Initialize(root, resultContainer);
        resultAnimator.OnAnimationCompleted += OnResultAnimationCompleted;

        // ジッパー
        var zipperSlider = root.Q<VisualElement>("ZipperSlider");
        var zipperRail = root.Q<VisualElement>("ZipperRail");
        if (zipperSlider != null && zipperRail != null)
        {
            zipper = new ZipperManipulator(zipperSlider, zipperRail);
            zipper.OnProgressChanged += OnZipperProgressChanged;
            zipper.OnUnzipCompleted += OnZipperOpened;
        }
    }

    protected override void BindUIEvents()
    {
        pullSingleBtn?.RegisterCallback<ClickEvent>(_ => TryPullGacha(1));
        pullTenBtn?.RegisterCallback<ClickEvent>(_ => TryPullGacha(10));
        rateInfoBtn?.RegisterCallback<ClickEvent>(_ => ShowRateInfo());
        resultCloseBtn?.RegisterCallback<ClickEvent>(_ => HideResults());
        skipBtn?.RegisterCallback<ClickEvent>(_ => SkipZipperAnimation());

        // 結果パネル背景クリックでも閉じる
        resultPanel?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == resultPanel) HideResults();
        });
    }

    protected override void BindGameEvents()
    {
        var gc = GameController.Instance;
        if (gc?.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged += OnCurrencyChanged;
            gc.Wallet.OnCertificateChanged += OnCurrencyChanged;
        }
    }

    protected override void OnPostInitialize()
    {
        if (database == null) return;

        // 初期表示状態リセット
        HideResults();
        if (bagScreen != null) bagScreen.AddToClassList("hidden");
        if (mainScreen != null) mainScreen.RemoveFromClassList("hidden");

        // バナー表示
        bannerController.RefreshBannerDisplay();
    }

    // ========================================
    // 通貨イベント
    // ========================================

    private void OnCurrencyChanged(double _)
    {
        UpdateButtonStates();
        bannerController.UpdateCurrencyDisplay();
    }

    // ========================================
    // ガチャ実行
    // ========================================

    private void TryPullGacha(int count)
    {
        if (isAnimating) return;

        var banner = bannerController.GetCurrentBanner();
        if (banner == null) return;

        if (!CanAffordPull(banner, count))
        {
            ShowNotEnoughCurrency(banner.currencyType);
            return;
        }

        // GachaManager経由で実行
        var results = GachaManager.Instance.PullGacha(banner, count);
        if (results != null && results.Count > 0)
        {
            pendingResults = results;
            maxRarityInResult = CalculateMaxRarity(results);
            StartZipperSequence();
        }
    }

    private bool CanAffordPull(GachaBannerData banner, int count)
    {
        double cost = banner.GetCost(count);
        return GameController.Instance.Wallet.CanAfford(cost, banner.currencyType);
    }

    private int CalculateMaxRarity(List<GachaResultItem> results)
    {
        int maxRarity = 0;
        foreach (var r in results)
        {
            if (r.rarity > maxRarity) maxRarity = r.rarity;
        }
        return maxRarity;
    }

    private void UpdateButtonStates()
    {
        var banner = bannerController.GetCurrentBanner();
        if (banner == null) return;

        bool isLocked = !GachaManager.Instance.IsBannerUnlocked(banner);
        bool canPullSingle = !isLocked && CanAffordPull(banner, 1);
        bool canPullTen = !isLocked && CanAffordPull(banner, 10);

        pullSingleBtn?.SetEnabled(canPullSingle && !isAnimating);
        pullTenBtn?.SetEnabled(canPullTen && !isAnimating);

        pullSingleBtn?.EnableInClassList("btn-disabled", !canPullSingle || isAnimating);
        pullTenBtn?.EnableInClassList("btn-disabled", !canPullTen || isAnimating);
    }

    // ========================================
    // ジッパー演出
    // ========================================

    private void StartZipperSequence()
    {
        isAnimating = true;
        UpdateButtonStates();

        // ビジュアルリセット
        visualController.Reset();
        visualController.SetMaxRarity(maxRarityInResult);
        zipper?.Reset();

        // 画面切り替え
        if (mainScreen != null) mainScreen.AddToClassList("hidden");
        if (bagScreen != null) bagScreen.RemoveFromClassList("hidden");
    }

    private void OnZipperProgressChanged(float progress)
    {
        visualController.UpdateVisuals(progress);
    }

    private void OnZipperOpened()
    {
        // 余韻を持たせてから結果画面へ
        root.schedule.Execute(() =>
        {
            if (bagScreen != null) bagScreen.AddToClassList("hidden");
            StartResultAnimation();
        }).ExecuteLater(GachaUIConstants.ZIPPER_COMPLETE_DELAY_MS);
    }

    private void SkipZipperAnimation()
    {
        if (!isAnimating) return;

        zipper?.ForceComplete();
        visualController.ForceComplete();

        root.schedule.Execute(() =>
        {
            OnZipperOpened();
        }).ExecuteLater(GachaUIConstants.SKIP_RESULT_DELAY_MS);
    }

    // ========================================
    // 結果表示
    // ========================================

    private void StartResultAnimation()
    {
        resultPanel?.RemoveFromClassList("hidden");
        resultAnimator.StartAnimation(pendingResults);
    }

    private void OnResultAnimationCompleted()
    {
        UpdateButtonStates();
        bannerController.UpdatePityDisplay();
    }

    private void HideResults()
    {
        isAnimating = false;

        resultPanel?.AddToClassList("hidden");

        // メイン画面に戻す
        if (mainScreen != null) mainScreen.RemoveFromClassList("hidden");

        // バナー表示を更新
        bannerController.RefreshBannerDisplay();
        UpdateButtonStates();
    }

    // ========================================
    // 確率表示
    // ========================================

    private void ShowRateInfo()
    {
        var banner = bannerController.GetCurrentBanner();
        if (banner == null) return;

        string rateText = GachaManager.Instance.GetRateDisplayText(banner);
        LogUIController.Msg(rateText);
    }

    // ========================================
    // 通貨不足表示
    // ========================================

    private void ShowNotEnoughCurrency(CurrencyType type)
    {
        string currencyName = type switch
        {
            CurrencyType.LMD => "龍門幣",
            CurrencyType.Certificate => "資格証",
            _ => "???"
        };
        LogUIController.Msg($"{currencyName}が足りません！");
    }

    // ========================================
    // クリーンアップ (BaseUIControllerテンプレートメソッド)
    // ========================================

    protected override void UnbindGameEvents()
    {
        var gc = GameController.Instance;
        if (gc?.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged -= OnCurrencyChanged;
            gc.Wallet.OnCertificateChanged -= OnCurrencyChanged;
        }
    }

    protected override void DisposeSubControllers()
    {
        // 結果アニメーター
        if (resultAnimator != null)
        {
            resultAnimator.OnAnimationCompleted -= OnResultAnimationCompleted;
            resultAnimator.Dispose();
        }

        bannerController?.Dispose();

        // ジッパーイベントの購読解除
        if (zipper != null)
        {
            zipper.OnProgressChanged -= OnZipperProgressChanged;
            zipper.OnUnzipCompleted -= OnZipperOpened;
            zipper = null;
        }
    }

    protected override void OnPostDispose()
    {
        pendingResults = null;
    }
}
