using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// ショップUIのアニメーション処理を担当するヘルパークラス
/// - タイプライターエフェクト
/// - 通貨ドラムロールアニメーション
/// - フラッシュ演出
/// </summary>
public class ShopAnimationHelper
{
    // ========================================
    // タイプライター
    // ========================================

    private IVisualElementScheduledItem typewriterTimer;
    private string targetDescriptionText = "";
    private int currentCharIndex;
    private Label descriptionLabel;
    private VisualElement rootElement;

    // ========================================
    // 通貨ドラムロール
    // ========================================

    private IVisualElementScheduledItem currencyTimer;
    private double currentDisplayMoney = -1;
    private double targetMoney = 0;
    private double currentDisplayCert = -1;
    private double targetCert = 0;

    private Label moneyLabel;
    private Label certLabel;

    // ========================================
    // コールバック
    // ========================================

    /// <summary>通貨表示が更新された時に発火</summary>
    public System.Action OnCurrencyUpdated;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// ヘルパーを初期化
    /// </summary>
    /// <param name="root">ルートビジュアル要素</param>
    /// <param name="moneyLbl">所持金ラベル</param>
    /// <param name="certLbl">資格証ラベル</param>
    /// <param name="descLbl">説明文ラベル（タイプライター用）</param>
    public void Initialize(VisualElement root, Label moneyLbl, Label certLbl, Label descLbl)
    {
        rootElement = root;
        moneyLabel = moneyLbl;
        certLabel = certLbl;
        descriptionLabel = descLbl;
    }

    /// <summary>
    /// 通貨アニメーションの初期値を設定
    /// </summary>
    public void SetInitialCurrencyValues(double money, double cert)
    {
        currentDisplayMoney = money;
        targetMoney = money;
        currentDisplayCert = cert;
        targetCert = cert;
        UpdateCurrencyLabels();
    }

    /// <summary>
    /// 通貨アニメーションループを開始
    /// </summary>
    public void StartCurrencyAnimation()
    {
        if (rootElement == null) return;
        currencyTimer = rootElement.schedule.Execute(OnCurrencyTick).Every(ShopUIConstants.CURRENCY_ANIMATION_INTERVAL_MS);
    }

    // ========================================
    // タイプライターエフェクト
    // ========================================

    /// <summary>
    /// タイプライターエフェクトを開始
    /// </summary>
    /// <param name="text">表示するテキスト</param>
    public void StartTypewriterEffect(string text)
    {
        if (descriptionLabel == null || rootElement == null) return;
        if (text == targetDescriptionText) return;

        StopTypewriter();

        targetDescriptionText = text;
        currentCharIndex = 0;
        descriptionLabel.text = "";

        typewriterTimer = rootElement.schedule.Execute(OnTypewriterTick).Every(ShopUIConstants.TYPEWRITER_INTERVAL_MS);
    }

    private void OnTypewriterTick()
    {
        if (descriptionLabel == null) return;

        if (currentCharIndex >= targetDescriptionText.Length)
        {
            descriptionLabel.text = targetDescriptionText;
            StopTypewriter();
            return;
        }

        currentCharIndex++;
        descriptionLabel.text = targetDescriptionText.Substring(0, currentCharIndex);
    }

    /// <summary>
    /// タイプライターエフェクトを停止
    /// </summary>
    public void StopTypewriter()
    {
        if (typewriterTimer != null)
        {
            typewriterTimer.Pause();
            typewriterTimer = null;
        }
    }

    /// <summary>
    /// タイプライターの対象テキストをリセット
    /// </summary>
    public void ResetTypewriterText()
    {
        targetDescriptionText = "";
    }

    // ========================================
    // 通貨ドラムロールアニメーション
    // ========================================

    /// <summary>
    /// 目標通貨額を設定（アニメーション開始）
    /// </summary>
    public void SetTargetMoney(double amount)
    {
        targetMoney = amount;
    }

    /// <summary>
    /// 目標資格証額を設定（アニメーション開始）
    /// </summary>
    public void SetTargetCert(double amount)
    {
        targetCert = amount;
    }

    private void OnCurrencyTick()
    {
        bool moneyChanged = AnimateCurrencyValue(ref currentDisplayMoney, targetMoney);
        bool certChanged = AnimateCurrencyValue(ref currentDisplayCert, targetCert);

        if (moneyChanged || certChanged)
        {
            UpdateCurrencyLabels();
        }
    }

    /// <summary>
    /// 通貨値のアニメーション処理（汎用メソッド）
    /// </summary>
    private bool AnimateCurrencyValue(ref double currentValue, double targetValue)
    {
        if (System.Math.Abs(currentValue - targetValue) > ShopUIConstants.CURRENCY_ANIMATION_THRESHOLD)
        {
            double diff = targetValue - currentValue;
            double step = diff * ShopUIConstants.CURRENCY_ANIMATION_SMOOTHING;

            if (System.Math.Abs(step) < ShopUIConstants.CURRENCY_ANIMATION_MIN_STEP)
            {
                step = diff > 0 ? ShopUIConstants.CURRENCY_ANIMATION_MIN_STEP : -ShopUIConstants.CURRENCY_ANIMATION_MIN_STEP;
            }

            currentValue += step;

            if ((step > 0 && currentValue > targetValue) || (step < 0 && currentValue < targetValue))
            {
                currentValue = targetValue;
            }
            return true;
        }
        else
        {
            currentValue = targetValue;
            return false;
        }
    }

    private void UpdateCurrencyLabels()
    {
        if (moneyLabel != null) moneyLabel.text = $"LMD: {currentDisplayMoney:N0}";
        if (certLabel != null) certLabel.text = $"資格証: {currentDisplayCert:N0}";
        OnCurrencyUpdated?.Invoke();
    }

    // ========================================
    // フラッシュ演出
    // ========================================

    /// <summary>
    /// パネルにフラッシュ演出を再生
    /// </summary>
    /// <param name="panel">フラッシュを表示するパネル</param>
    public void PlayFlashEffect(VisualElement panel)
    {
        if (panel == null) return;

        var flashOverlay = new VisualElement();
        flashOverlay.style.position = Position.Absolute;
        flashOverlay.style.top = 0;
        flashOverlay.style.bottom = 0;
        flashOverlay.style.left = 0;
        flashOverlay.style.right = 0;
        flashOverlay.style.backgroundColor = new Color(1f, 1f, 1f, ShopUIConstants.FLASH_OVERLAY_OPACITY);
        flashOverlay.pickingMode = PickingMode.Ignore;

        panel.Add(flashOverlay);

        // フェードアウトアニメーション
        panel.schedule.Execute(() =>
        {
            flashOverlay.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
            flashOverlay.style.transitionDuration = new List<TimeValue> { new TimeValue(ShopUIConstants.FLASH_FADE_DURATION_MS, TimeUnit.Millisecond) };
            flashOverlay.style.opacity = 0f;
        }).ExecuteLater(ShopUIConstants.FLASH_FADE_START_DELAY_MS);

        // 削除
        panel.schedule.Execute(() =>
        {
            if (panel.Contains(flashOverlay))
            {
                panel.Remove(flashOverlay);
            }
        }).ExecuteLater(ShopUIConstants.FLASH_REMOVE_DELAY_MS);
    }

    /// <summary>
    /// アイコンにバウンスアニメーションを再生
    /// </summary>
    public void PlayIconBounce(VisualElement icon)
    {
        if (icon == null) return;

        icon.AddToClassList("icon-bounce");
        icon.schedule.Execute(() =>
        {
            icon.RemoveFromClassList("icon-bounce");
        }).ExecuteLater(ShopUIConstants.ICON_BOUNCE_DURATION_MS);
    }

    /// <summary>
    /// エフェクトコンテナにフラッシュアニメーションを再生
    /// </summary>
    public void PlayEffectFlash(VisualElement container)
    {
        if (container == null) return;

        container.AddToClassList("effect-flash");
        container.schedule.Execute(() =>
        {
            container.RemoveFromClassList("effect-flash");
        }).ExecuteLater(ShopUIConstants.EFFECT_FLASH_DURATION_MS);
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        StopTypewriter();

        if (currencyTimer != null)
        {
            currencyTimer.Pause();
            currencyTimer = null;
        }

        OnCurrencyUpdated = null;
    }
}
