using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ガチャ画面のロジック（IViewController実装）
/// ジッパー演出統合版
/// </summary>
public class GachaUIController : IViewController
{
    // ========================================
    // 設定値 (HDRカラー: Bloom演出用)
    // ========================================
    private readonly Color hdrColorUR = new Color(3f, 1f, 0f, 1f);   // Orange
    private readonly Color hdrColorSSR = new Color(3f, 2.5f, 0.5f, 1f); // Yellow
    private readonly Color hdrColorSR = new Color(1.5f, 1f, 2f, 1f);    // Purple

    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;

    // メイン画面（表示切替用）
    private VisualElement mainScreen;

    private VisualElement bannerImage;
    private Label bannerNameLabel;
    private Label bannerDescLabel;
    private Label costLabel;
    private Label pityLabel;
    private Label currencyLabel;
    private Label progressLabel;
    private Button pullSingleBtn;
    private Button pullTenBtn;
    private Button prevBannerBtn;
    private Button nextBannerBtn;
    private Button rateInfoBtn;
    private VisualElement bannerIndicator;

    private VisualElement resultPanel;
    private VisualElement resultContainer;
    private Button resultCloseBtn;

    private VisualElement lockOverlay;
    private Label lockReasonLabel;

    // ★追加: 演出画面要素 (BagScreen)
    private VisualElement bagScreen;
    private VisualElement lightBeam;
    private VisualElement ambientLight;
    private VisualElement zipperSlider;
    private VisualElement zipperRail;
    private VisualElement particleContainer;
    private Button skipBtn;

    // ========================================
    // データ
    // ========================================

    private GachaDatabase database;
    private List<GachaBannerData> bannerList = new();
    private int currentBannerIndex = 0;

    // ========================================
    // 状態
    // ========================================

    private bool isAnimating = false;
    private bool hasShaken = false; // シェイク済みフラグ

    // ★追加: ジッパー操作ロジック
    private ZipperManipulator zipper;
    private int maxRarityInResult = 3; // 演出の色決定用

    // ========================================
    // 演出用
    // ========================================

    private IVisualElementScheduledItem resultAnimTimer;
    private List<GachaResultItem> pendingResults;
    private int resultAnimIndex;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        this.root = root;
        this.database = GachaManager.Instance?.Database;

        if (database == null)
        {
            Debug.LogWarning("[GachaUIController] GachaDatabase not found in GachaManager!");
            return;
        }

        bannerList = database.GetAllBanners();

        QueryElements();
        BindButtons();
        BindEvents();
        SetupZipper(); // ★追加: ジッパー初期化

        BuildBannerIndicator();
        RefreshBannerDisplay();

        // 初期表示状態リセット
        HideResults();
        if (bagScreen != null) bagScreen.AddToClassList("hidden");
        if (mainScreen != null) mainScreen.RemoveFromClassList("hidden");
    }

    private void QueryElements()
    {
        // メイン画面のルートを取得（UXML構造によるが、全体を隠すために取得）
        mainScreen = root.Q<VisualElement>("main-screen");

        bannerImage = root.Q<VisualElement>("banner-image");
        bannerNameLabel = root.Q<Label>("banner-name");
        bannerDescLabel = root.Q<Label>("banner-desc");
        costLabel = root.Q<Label>("cost-label");
        pityLabel = root.Q<Label>("pity-label");
        currencyLabel = root.Q<Label>("currency-label");
        pullSingleBtn = root.Q<Button>("btn-pull-single");
        pullTenBtn = root.Q<Button>("btn-pull-ten");
        prevBannerBtn = root.Q<Button>("btn-prev-banner");
        nextBannerBtn = root.Q<Button>("btn-next-banner");
        rateInfoBtn = root.Q<Button>("btn-rate-info");
        bannerIndicator = root.Q<VisualElement>("banner-indicator");

        resultPanel = root.Q<VisualElement>("result-panel");
        resultContainer = root.Q<VisualElement>("result-container");
        resultCloseBtn = root.Q<Button>("btn-result-close");

        // ★追加: 演出用要素の取得
        bagScreen = root.Q<VisualElement>("BagScreen");
        lightBeam = root.Q<VisualElement>("LightBeam");
        ambientLight = root.Q<VisualElement>("AmbientLight");
        zipperSlider = root.Q<VisualElement>("ZipperSlider");
        zipperRail = root.Q<VisualElement>("ZipperRail");
        particleContainer = root.Q<VisualElement>("ParticleContainer");
        skipBtn = root.Q<Button>("btn-skip");
    }

    // ★追加: ジッパーロジックのセットアップ
    private void SetupZipper()
    {
        if (zipperSlider != null && zipperRail != null)
        {
            zipper = new ZipperManipulator(zipperSlider, zipperRail);
            zipper.OnProgressChanged += UpdateGachaVisuals;
            zipper.OnUnzipCompleted += OnZipperOpened;
        }
    }

    private void BindButtons()
    {
        pullSingleBtn?.RegisterCallback<ClickEvent>(_ => TryPullGacha(1));
        pullTenBtn?.RegisterCallback<ClickEvent>(_ => TryPullGacha(10));
        prevBannerBtn?.RegisterCallback<ClickEvent>(_ => ChangeBanner(-1));
        nextBannerBtn?.RegisterCallback<ClickEvent>(_ => ChangeBanner(1));
        rateInfoBtn?.RegisterCallback<ClickEvent>(_ => ShowRateInfo());
        resultCloseBtn?.RegisterCallback<ClickEvent>(_ => HideResults());

        // 結果パネル背景クリックでも閉じる
        resultPanel?.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == resultPanel) HideResults();
        });

        // スキップボタン
        skipBtn?.RegisterCallback<ClickEvent>(_ => SkipZipperAnimation());
    }

    private void BindEvents()
    {
        var gc = GameController.Instance;
        if (gc?.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged += OnCurrencyChanged;
            gc.Wallet.OnCertificateChanged += OnCurrencyChanged;
        }
    }

    private void UnbindEvents()
    {
        var gc = GameController.Instance;
        if (gc?.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged -= OnCurrencyChanged;
            gc.Wallet.OnCertificateChanged -= OnCurrencyChanged;
        }
    }

    private void OnCurrencyChanged(double _)
    {
        UpdateButtonStates();
        UpdateCurrencyDisplay();
    }

    // ========================================
    // バナー表示
    // ========================================

    private void RefreshBannerDisplay()
    {
        var banner = GetCurrentBanner();
        if (banner == null) return;

        // ロック状態チェック
        bool isLocked = !GachaManager.Instance.IsBannerUnlocked(banner);

        // バナー情報更新
        if (bannerNameLabel != null) bannerNameLabel.text = banner.bannerName;
        if (bannerDescLabel != null) bannerDescLabel.text = banner.description;

        // バナー画像
        if (bannerImage != null && banner.bannerSprite != null)
        {
            bannerImage.style.backgroundImage = new StyleBackground(banner.bannerSprite);
        }

        // 限定バナー表示
        bannerNameLabel?.EnableInClassList("limited-banner", banner.isLimited);

        // コスト表示
        UpdateCostDisplay(banner);

        // 天井表示
        UpdatePityDisplay(banner);

        // 進捗表示（初心者バナー等）
        UpdateProgressDisplay(banner);

        // 通貨表示
        UpdateCurrencyDisplay();

        // ロック表示
        UpdateLockDisplay(banner, isLocked);

        // ボタン状態
        UpdateButtonStates();

        // インジケーター更新
        UpdateBannerIndicator();

        // バナー切替ボタン
        bool showNav = bannerList.Count > 1;
        prevBannerBtn?.SetEnabled(showNav);
        nextBannerBtn?.SetEnabled(showNav);
    }

    private void UpdateCostDisplay(GachaBannerData banner)
    {
        if (costLabel == null) return;

        string currencyName = GetCurrencyName(banner.currencyType);
        costLabel.text = $"1回: {banner.costSingle:N0} {currencyName} | 10回: {banner.costTen:N0} {currencyName}";

        // フォントサイズを文字数に応じて調整
        AdjustFontSizeToFit(costLabel, 20, 12, 30);
    }

    private void UpdatePityDisplay(GachaBannerData banner)
    {
        if (pityLabel == null) return;

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
            // 動的生成
            progressLabel = root.Q<Label>("progress-label");
        }

        if (progressLabel == null) return;

        // このバナーが他バナーの解放条件になっているかチェック
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

    private void UpdateLockDisplay(GachaBannerData banner, bool isLocked)
    {
        // ロックオーバーレイを動的生成（bannerImage上に）
        if (lockOverlay == null && bannerImage != null)
        {
            lockOverlay = new VisualElement();
            lockOverlay.name = "lock-overlay";
            lockOverlay.AddToClassList("lock-overlay");

            lockReasonLabel = new Label();
            lockReasonLabel.name = "lock-reason";
            lockReasonLabel.AddToClassList("lock-reason");
            lockOverlay.Add(lockReasonLabel);

            // bannerImageの親に追加
            bannerImage.parent?.Add(lockOverlay);
        }

        if (lockOverlay == null) return;

        if (isLocked)
        {
            lockOverlay.style.display = DisplayStyle.Flex;

            // 解放条件テキスト
            string reason = GetUnlockReasonText(banner);
            if (lockReasonLabel != null) lockReasonLabel.text = reason;
        }
        else
        {
            lockOverlay.style.display = DisplayStyle.None;
        }
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

    private void UpdateCurrencyDisplay()
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

        // フォントサイズを文字数に応じて調整
        AdjustFontSizeToFit(currencyLabel, 26, 14, 20);
    }

    /// <summary>
    /// テキストの長さに応じてフォントサイズを調整
    /// </summary>
    /// <param name="label">対象ラベル</param>
    /// <param name="maxSize">最大フォントサイズ</param>
    /// <param name="minSize">最小フォントサイズ</param>
    /// <param name="thresholdLength">この文字数を超えたら縮小開始</param>
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
            // 文字数が増えるほどサイズを縮小
            float ratio = (float)thresholdLength / textLength;
            int newSize = Mathf.Clamp(Mathf.RoundToInt(maxSize * ratio), minSize, maxSize);
            label.style.fontSize = newSize;
        }
    }

    private void UpdateButtonStates()
    {
        var banner = GetCurrentBanner();
        if (banner == null) return;

        bool isLocked = !GachaManager.Instance.IsBannerUnlocked(banner);
        bool canPullSingle = !isLocked && CanAffordPull(banner, 1);
        bool canPullTen = !isLocked && CanAffordPull(banner, 10);

        pullSingleBtn?.SetEnabled(canPullSingle && !isAnimating);
        pullTenBtn?.SetEnabled(canPullTen && !isAnimating);

        pullSingleBtn?.EnableInClassList("btn-disabled", !canPullSingle || isAnimating);
        pullTenBtn?.EnableInClassList("btn-disabled", !canPullTen || isAnimating);
    }

    private string GetCurrencyName(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.LMD => "龍門幣",
            CurrencyType.Certificate => "資格証",
            _ => "???"
        };
    }

    // ========================================
    // バナー切り替え
    // ========================================

    private void ChangeBanner(int direction)
    {
        if (bannerList.Count <= 1) return;

        currentBannerIndex += direction;
        if (currentBannerIndex < 0) currentBannerIndex = bannerList.Count - 1;
        if (currentBannerIndex >= bannerList.Count) currentBannerIndex = 0;

        RefreshBannerDisplay();
    }

    private GachaBannerData GetCurrentBanner()
    {
        if (bannerList == null || bannerList.Count == 0) return null;
        return bannerList[Mathf.Clamp(currentBannerIndex, 0, bannerList.Count - 1)];
    }

    // ========================================
    // バナーインジケーター（ドット）
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
    // ガチャ実行（修正：ジッパー演出を挟む）
    // ========================================

    private void TryPullGacha(int count)
    {
        if (isAnimating) return;

        var banner = GetCurrentBanner();
        if (banner == null) return;

        if (!CanAffordPull(banner, count))
        {
            ShowNotEnoughCurrency(banner.currencyType);
            return;
        }

        // GachaManager経由で実行（ここで通貨消費・結果確定される）
        var results = GachaManager.Instance.PullGacha(banner, count);
        if (results != null && results.Count > 0)
        {
            // 結果を一時保存
            pendingResults = results;

            // 演出のために最高レア度を計算
            maxRarityInResult = 0;
            foreach (var r in results)
            {
                if (r.rarity > maxRarityInResult) maxRarityInResult = r.rarity;
            }

            // ★修正: 結果アニメではなく、まずジッパー演出へ
            StartZipperSequence();
        }
    }

    private bool CanAffordPull(GachaBannerData banner, int count)
    {
        double cost = banner.GetCost(count);
        return GameController.Instance.Wallet.CanAfford(cost, banner.currencyType);
    }

    // ========================================
    // ★追加: ジッパー演出ロジック
    // ========================================

    private void StartZipperSequence()
    {
        isAnimating = true;
        hasShaken = false; // シェイクフラグリセット
        UpdateButtonStates();

        // 1. ジッパー画面の初期化
        zipper?.Reset();
        if (lightBeam != null) { lightBeam.style.width = 0; lightBeam.style.opacity = 0; }
        if (ambientLight != null) { ambientLight.style.backgroundColor = new StyleColor(Color.clear); }

        // パーティクルコンテナをクリア
        particleContainer?.Clear();

        // 2. 画面切り替え：メイン画面を隠し、バッグを表示
        if (mainScreen != null) mainScreen.AddToClassList("hidden");
        if (bagScreen != null) bagScreen.RemoveFromClassList("hidden");
    }

    // ZipperManipulatorからのコールバック
    private void UpdateGachaVisuals(float progress)
    {
        if (lightBeam == null) return;

        // 光のビームを伸ばす
        float maxBeamWidth = 600f;
        lightBeam.style.width = maxBeamWidth * progress;

        // 最初は弱く、開くにつれて強くなる
        lightBeam.style.opacity = Mathf.Clamp01(progress * 1.5f);

        // 進行度が半分を超えたら、最高レア度に応じた色を漏れ出させる
        if (progress > 0.2f)
        {
            Color baseColor = Color.white;
            Color targetColor = GetHDRColor(maxRarityInResult);
            float t = (progress - 0.2f) / 0.8f;

            // ビーム色の遷移
            lightBeam.style.unityBackgroundImageTintColor = Color.Lerp(baseColor, targetColor, t);

            // 周囲の環境光
            if (ambientLight != null)
            {
                ambientLight.style.backgroundColor = targetColor;
                ambientLight.style.opacity = t * 0.8f;
            }

            // パーティクル発生（進行度に応じて）
            SpawnParticles(progress);
        }

        // 高レア時：完了間近でシェイク（1回だけ）
        if (progress > 0.9f && maxRarityInResult >= 5 && !hasShaken)
        {
            hasShaken = true;
            PlayScreenShake(maxRarityInResult);
        }
    }

    // ジッパーが開けきった時のコールバック
    private void OnZipperOpened()
    {
        // 余韻を持たせてから結果画面へ
        root.schedule.Execute(() =>
        {
            if (bagScreen != null) bagScreen.AddToClassList("hidden");
            StartResultAnimation(pendingResults);
        }).ExecuteLater(200); // 200ms待機
    }

    private Color GetHDRColor(int rarity)
    {
        switch (rarity)
        {
            case 6: return hdrColorUR;
            case 5: return hdrColorSSR;
            default: return hdrColorSR;
        }
    }

    // ========================================
    // ★追加: パーティクル演出
    // ========================================

    private void SpawnParticles(float progress)
    {
        if (particleContainer == null || progress < 0.3f) return;

        // 進行度に応じてパーティクル数を増やす
        int particleCount = (int)((progress - 0.3f) * 8);

        for (int i = 0; i < particleCount; i++)
        {
            var particle = new VisualElement();
            particle.AddToClassList("particle");

            // 高レアリティなら大きいパーティクル
            if (maxRarityInResult >= 6 && UnityEngine.Random.value > 0.7f)
            {
                particle.AddToClassList("particle-large");
            }

            // レアリティに応じた色
            Color particleColor = GetHDRColor(maxRarityInResult);
            particle.style.backgroundColor = particleColor;

            // ランダム位置（ジッパー付近から発生）
            float railWidth = 650f;
            float openWidth = railWidth * progress;
            float xPos = UnityEngine.Random.Range(0f, openWidth) + 50f;
            float yPos = 200f + UnityEngine.Random.Range(-30f, 30f);

            particle.style.position = Position.Absolute;
            particle.style.left = xPos;
            particle.style.top = yPos;
            particle.style.opacity = 1f;

            particleContainer.Add(particle);

            // アニメーション：上に舞い上がって消える
            float targetY = yPos - UnityEngine.Random.Range(80f, 200f);
            float targetX = xPos + UnityEngine.Random.Range(-50f, 50f);
            int duration = UnityEngine.Random.Range(400, 800);

            root.schedule.Execute(() =>
            {
                particle.style.translate = new Translate(targetX - xPos, targetY - yPos, 0);
                particle.style.opacity = 0f;
                particle.style.transitionProperty = new List<StylePropertyName>
                {
                    new StylePropertyName("translate"),
                    new StylePropertyName("opacity")
                };
                particle.style.transitionDuration = new List<TimeValue>
                {
                    new TimeValue(duration, TimeUnit.Millisecond)
                };
            }).ExecuteLater(10);

            // 削除
            root.schedule.Execute(() =>
            {
                if (particleContainer.Contains(particle))
                    particleContainer.Remove(particle);
            }).ExecuteLater(duration + 50);
        }
    }

    // ========================================
    // ★追加: 画面シェイク演出
    // ========================================

    private void PlayScreenShake(int intensity)
    {
        if (bagScreen == null) return;

        int shakeCount = intensity >= 6 ? 6 : (intensity >= 5 ? 4 : 2);
        float magnitude = intensity >= 6 ? 12f : (intensity >= 5 ? 8f : 4f);
        int delay = 0;

        for (int i = 0; i < shakeCount; i++)
        {
            int currentDelay = delay;
            float offsetX = (i % 2 == 0 ? 1 : -1) * magnitude * (1f - (float)i / shakeCount);
            float offsetY = UnityEngine.Random.Range(-magnitude * 0.3f, magnitude * 0.3f);

            root.schedule.Execute(() =>
            {
                bagScreen.style.translate = new Translate(offsetX, offsetY, 0);
            }).ExecuteLater(currentDelay);

            delay += 50;
        }

        // 元に戻す
        root.schedule.Execute(() =>
        {
            bagScreen.style.translate = new Translate(0, 0, 0);
        }).ExecuteLater(delay);
    }

    // ========================================
    // ★追加: スキップ機能
    // ========================================

    private void SkipZipperAnimation()
    {
        if (!isAnimating) return;

        // ジッパーを即座に開いた状態に
        zipper?.ForceComplete();

        // 演出を即座に完了させる
        if (lightBeam != null)
        {
            lightBeam.style.width = 600f;
            lightBeam.style.opacity = 1f;
            lightBeam.style.unityBackgroundImageTintColor = GetHDRColor(maxRarityInResult);
        }

        // 画面シェイク（高レア時）
        if (maxRarityInResult >= 5)
        {
            PlayScreenShake(maxRarityInResult);
        }

        // 少し待ってから結果画面へ
        root.schedule.Execute(() =>
        {
            OnZipperOpened();
        }).ExecuteLater(100);
    }

    // ========================================
    // 結果表示（schedule使用）
    // ========================================

    private void StartResultAnimation(List<GachaResultItem> results)
    {
        // isAnimating = true; // 既にTrueのはずだが念のため

        // 結果パネル表示
        resultPanel?.RemoveFromClassList("hidden");
        resultContainer?.Clear();

        // アニメーション開始
        pendingResults = results;
        resultAnimIndex = 0;

        resultAnimTimer = root.schedule.Execute(OnResultAnimTick).Every(150);
    }

    private void OnResultAnimTick()
    {
        if (pendingResults == null || resultAnimIndex >= pendingResults.Count)
        {
            // アニメーション完了
            resultAnimTimer?.Pause();
            resultAnimTimer = null;
            // 閉じるまで isAnimating は true のままにする（閉じるボタン押下でfalse）
            UpdateButtonStates();
            UpdatePityDisplay(GetCurrentBanner());
            return;
        }

        var item = pendingResults[resultAnimIndex];
        var itemElement = CreateResultItemElement(item);
        resultContainer?.Add(itemElement);

        // 高レアは遅延を長く
        int delay = item.rarity switch
        {
            >= 6 => 500,
            5 => 350,
            4 => 200,
            _ => 100
        };

        // 次のアイテムの遅延を調整
        resultAnimTimer?.Pause();
        resultAnimTimer = null;
        resultAnimIndex++;

        if (resultAnimIndex < pendingResults.Count)
        {
            root.schedule.Execute(OnResultAnimTick).ExecuteLater(delay);
        }
        else
        {
            // 最後のアイテム後の完了処理
            root.schedule.Execute(() =>
            {
                // isAnimating = false; // ここではまだ操作させない
                UpdateButtonStates();
                UpdatePityDisplay(GetCurrentBanner());
            }).ExecuteLater(delay);
        }
    }

    private VisualElement CreateResultItemElement(GachaResultItem item)
    {
        var container = new VisualElement();
        container.AddToClassList("result-item");
        container.AddToClassList($"rarity-{item.rarity}");

        // ★追加: レアリティ別のアニメーションクラスを適用
        string animClass = item.rarity switch
        {
            >= 6 => "result-item-spin-6",
            5 => "result-item-spin-5",
            4 => "result-item-spin-4",
            _ => "result-item-appear"
        };
        container.AddToClassList(animClass);

        if (item.isNew) container.AddToClassList("is-new");
        if (item.isPickup) container.AddToClassList("is-pickup");

        // アイコン
        var icon = new VisualElement();
        icon.AddToClassList("result-item-icon");
        if (item.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(item.icon);
        }
        container.Add(icon);

        // NEWバッジ
        if (item.isNew)
        {
            var newBadge = new Label("NEW");
            newBadge.AddToClassList("new-badge");
            container.Add(newBadge);
        }

        // ピックアップバッジ
        if (item.isPickup)
        {
            var pickupBadge = new Label("PICK UP");
            pickupBadge.AddToClassList("pickup-badge");
            container.Add(pickupBadge);
        }

        // 名前
        var nameLabel = new Label(item.itemName ?? "???");
        nameLabel.AddToClassList("result-item-name");
        container.Add(nameLabel);

        // レアリティ表示（星）
        var rarityLabel = new Label(new string('★', item.rarity));
        rarityLabel.AddToClassList("result-item-rarity");
        container.Add(rarityLabel);

        // ★追加: 少し遅延してからvisibleクラスを追加（アニメーション発火）
        root.schedule.Execute(() =>
        {
            container.AddToClassList("visible");
        }).ExecuteLater(50);

        return container;
    }

    private void HideResults()
    {
        // 閉じる処理（ここで初めて操作ロック解除）
        isAnimating = false;

        resultPanel?.AddToClassList("hidden");

        // メイン画面に戻す
        if (mainScreen != null) mainScreen.RemoveFromClassList("hidden");

        // ★修正: バナー表示を更新（コンプリート進捗などを反映）
        RefreshBannerDisplay();
    }

    // ========================================
    // 確率表示
    // ========================================

    private void ShowRateInfo()
    {
        var banner = GetCurrentBanner();
        if (banner == null) return;

        string rateText = GachaManager.Instance.GetRateDisplayText(banner);
        LogUIController.Msg(rateText);
        // TODO: ポップアップUIに表示
    }

    // ========================================
    // 通貨不足表示
    // ========================================

    private void ShowNotEnoughCurrency(CurrencyType type)
    {
        string currencyName = GetCurrencyName(type);
        LogUIController.Msg($"{currencyName}が足りません！");
        // TODO: ポップアップUIに表示
    }

    // ========================================
    // Dispose
    // ========================================

    public void Dispose()
    {
        UnbindEvents();

        // タイマー停止
        resultAnimTimer?.Pause();
        resultAnimTimer = null;

        pendingResults = null;
        bannerList.Clear();
    }
}