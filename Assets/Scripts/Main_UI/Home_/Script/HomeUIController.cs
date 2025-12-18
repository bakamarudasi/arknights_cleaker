using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// ホーム画面（メインクリック画面）のUIコントローラー
/// キャラクターをクリックしてお金を稼ぐメイン画面
/// </summary>
public class HomeUIController : IViewController
{
    private VisualElement root;

    // UI要素
    private VisualElement clickTarget;
    private Label moneyLabel;
    private Label spLabel;
    private VisualElement spGaugeFill;
    private Label feverLabel;

    // コールバック参照（解除用）
    private EventCallback<ClickEvent> onClickCallback;
    private Action<double> onMoneyChangedCallback;
    private Action<float> onSPChangedCallback;
    private Action onFeverStartedCallback;
    private Action onFeverEndedCallback;

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        QueryElements();
        SetupCallbacks();
        BindGameEvents();
        UpdateUI();

        LogUIController.LogSystem("Home View Initialized.");
    }

    private void QueryElements()
    {
        clickTarget = root.Q<VisualElement>("click-target");
        moneyLabel = root.Q<Label>("money-label");
        spLabel = root.Q<Label>("sp-label");
        spGaugeFill = root.Q<VisualElement>("sp-gauge-fill");
        feverLabel = root.Q<Label>("fever-label");
    }

    private void SetupCallbacks()
    {
        // クリックイベント
        onClickCallback = OnClickTarget;
        clickTarget?.RegisterCallback(onClickCallback);
    }

    private void BindGameEvents()
    {
        var gc = GameController.Instance;
        if (gc == null) return;

        // コールバックをフィールドに保存
        onMoneyChangedCallback = _ => UpdateMoneyDisplay();
        onSPChangedCallback = _ => UpdateSPDisplay();
        onFeverStartedCallback = OnFeverStarted;
        onFeverEndedCallback = OnFeverEnded;

        // イベント登録
        if (gc.Wallet != null)
        {
            gc.Wallet.OnMoneyChanged += onMoneyChangedCallback;
        }

        if (gc.SP != null)
        {
            gc.SP.OnSPChanged += onSPChangedCallback;
            gc.SP.OnFeverStarted += onFeverStartedCallback;
            gc.SP.OnFeverEnded += onFeverEndedCallback;
        }
    }

    // ========================================
    // クリック処理
    // ========================================

    private void OnClickTarget(ClickEvent evt)
    {
        // GameControllerのクリック処理を呼び出し
        GameController.Instance?.ClickMainButton();

        // クリックエフェクト（視覚的フィードバック）
        PlayClickEffect();
    }

    private void PlayClickEffect()
    {
        // TODO: クリック時のエフェクト（パーティクル、数字表示など）
        // FloatingTextManagerがあればそちらで処理される
    }

    // ========================================
    // UI更新
    // ========================================

    private void UpdateUI()
    {
        UpdateMoneyDisplay();
        UpdateSPDisplay();
    }

    private void UpdateMoneyDisplay()
    {
        var gc = GameController.Instance;
        if (gc == null || moneyLabel == null) return;

        double money = gc.GetMoney();
        moneyLabel.text = $"LMD: {FormatNumber(money)}";
    }

    private void UpdateSPDisplay()
    {
        var gc = GameController.Instance;
        if (gc?.SP == null) return;

        var sp = gc.SP;

        // SPラベル更新
        if (spLabel != null)
        {
            spLabel.text = $"SP: {sp.CurrentSP:F0}/{sp.MaxSP:F0}";
        }

        // ゲージ更新
        if (spGaugeFill != null)
        {
            float fillPercent = sp.FillRate * 100f;
            spGaugeFill.style.width = new Length(fillPercent, LengthUnit.Percent);

            // フィーバー中は色を変える
            if (sp.IsFeverActive)
            {
                spGaugeFill.AddToClassList("fever");
            }
            else
            {
                spGaugeFill.RemoveFromClassList("fever");
            }
        }
    }

    private void OnFeverStarted()
    {
        // フィーバー開始
        if (feverLabel != null)
        {
            feverLabel.text = "FEVER!!";
            feverLabel.AddToClassList("active");
        }

        if (spGaugeFill != null)
        {
            spGaugeFill.AddToClassList("fever");
            spGaugeFill.style.width = new Length(100, LengthUnit.Percent);
        }

        LogUIController.Msg("<color=#FF5050>FEVER MODE ACTIVATED!</color>");
    }

    private void OnFeverEnded()
    {
        // フィーバー終了
        if (feverLabel != null)
        {
            feverLabel.RemoveFromClassList("active");
        }

        if (spGaugeFill != null)
        {
            spGaugeFill.RemoveFromClassList("fever");
        }

        LogUIController.Msg("Fever mode ended.");
    }

    // ========================================
    // ユーティリティ
    // ========================================

    private string FormatNumber(double value)
    {
        if (value >= 1_000_000_000_000) return $"{value / 1_000_000_000_000:F2}T";
        if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F2}B";
        if (value >= 1_000_000) return $"{value / 1_000_000:F2}M";
        if (value >= 1_000) return $"{value / 1_000:F2}K";
        return value.ToString("N0");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        // クリックイベント解除
        if (onClickCallback != null)
        {
            clickTarget?.UnregisterCallback(onClickCallback);
            onClickCallback = null;
        }

        // GameControllerイベント解除
        var gc = GameController.Instance;
        if (gc != null)
        {
            if (gc.Wallet != null && onMoneyChangedCallback != null)
            {
                gc.Wallet.OnMoneyChanged -= onMoneyChangedCallback;
            }

            if (gc.SP != null)
            {
                if (onSPChangedCallback != null)
                    gc.SP.OnSPChanged -= onSPChangedCallback;
                if (onFeverStartedCallback != null)
                    gc.SP.OnFeverStarted -= onFeverStartedCallback;
                if (onFeverEndedCallback != null)
                    gc.SP.OnFeverEnded -= onFeverEndedCallback;
            }
        }

        // 参照クリア
        onMoneyChangedCallback = null;
        onSPChangedCallback = null;
        onFeverStartedCallback = null;
        onFeverEndedCallback = null;

        LogUIController.LogSystem("Home View Disposed.");
    }
}
