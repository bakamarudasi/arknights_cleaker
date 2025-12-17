using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// オペレーター画面のUIコントローラー
/// UI Toolkit（ボタン）とGameObject（キャラ表示）を連携
/// </summary>
public class OperatorUIController : IViewController
{
    private VisualElement root;

    // ボタン
    private Button btnOutfitDefault;
    private Button btnOutfitSkin1;
    private Button btnOutfitSkin2;
    private Button btnLensNormal;
    private Button btnLensClothes;
    private Button btnBack;


    // 現在の状態
    private int currentOutfit = 0;
    private int currentLensMode = 0; // 0: Normal, 1: Clothes Off

    // イベント
    public event Action OnBackRequested;

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        SetupReferences();
        SetupCallbacks();
        UpdateDisplay();

        // PSBキャラ表示
        ShowCharacterOverlay();

        LogUIController.LogSystem("Operator View Initialized.");
    }

    /// <summary>
    /// Overlay Canvasにキャラを表示
    /// </summary>
    private void ShowCharacterOverlay()
    {
        Debug.Log("[OperatorUI] ShowCharacterOverlay called");
        var presenter = OverlayCharacterPresenter.Instance;
        Debug.Log($"[OperatorUI] Presenter Instance: {(presenter != null ? presenter.name : "NULL")}");

        if (presenter != null)
        {
            presenter.EnsureCreated();
            presenter.Show();
        }
        else
        {
            Debug.LogWarning("[OperatorUI] OverlayCharacterPresenter.Instance is NULL! Is it in the scene?");
            LogUIController.LogSystem("OverlayCharacterPresenter not found in scene.");
        }
    }

    /// <summary>
    /// Overlay Canvasを非表示
    /// </summary>
    private void HideCharacterOverlay()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        presenter?.Hide();
    }

    private void SetupReferences()
    {
        // 着せ替えボタン
        btnOutfitDefault = root.Q<Button>("btn-outfit-default");
        btnOutfitSkin1 = root.Q<Button>("btn-outfit-skin1");
        btnOutfitSkin2 = root.Q<Button>("btn-outfit-skin2");

        // レンズボタン
        btnLensNormal = root.Q<Button>("btn-lens-normal");
        btnLensClothes = root.Q<Button>("btn-lens-clothes");

        // 戻るボタン
        btnBack = root.Q<Button>("btn-back");
    }

    private void SetupCallbacks()
    {
        // 着せ替えボタン
        btnOutfitDefault?.RegisterCallback<ClickEvent>(evt => SetOutfit(0));
        btnOutfitSkin1?.RegisterCallback<ClickEvent>(evt => SetOutfit(1));
        btnOutfitSkin2?.RegisterCallback<ClickEvent>(evt => SetOutfit(2));

        // レンズボタン
        btnLensNormal?.RegisterCallback<ClickEvent>(evt => SetLensMode(0));
        btnLensClothes?.RegisterCallback<ClickEvent>(evt => SetLensMode(1));

        // 戻るボタン
        btnBack?.RegisterCallback<ClickEvent>(evt => OnBackRequested?.Invoke());
    }

    /// <summary>
    /// 着せ替えを切り替え
    /// </summary>
    private void SetOutfit(int outfitIndex)
    {
        currentOutfit = outfitIndex;
        UpdateOutfitButtons();

        // TODO: 着せ替えPrefab切り替え実装予定

        LogUIController.Msg($"Outfit changed to: {GetOutfitName(outfitIndex)}");
    }

    /// <summary>
    /// レンズモード切り替え（服のON/OFF）
    /// </summary>
    private void SetLensMode(int mode)
    {
        currentLensMode = mode;
        UpdateLensButtons();

        // TODO: キャラクター表示実装時にここで切り替え

        string modeName = mode == 0 ? "Normal" : "Clothes Off";
        LogUIController.Msg($"Lens mode: {modeName}");
    }

    private void UpdateOutfitButtons()
    {
        // 全ボタンからactiveクラスを削除
        btnOutfitDefault?.RemoveFromClassList("active");
        btnOutfitSkin1?.RemoveFromClassList("active");
        btnOutfitSkin2?.RemoveFromClassList("active");

        // 選択中のボタンにactiveクラスを追加
        switch (currentOutfit)
        {
            case 0: btnOutfitDefault?.AddToClassList("active"); break;
            case 1: btnOutfitSkin1?.AddToClassList("active"); break;
            case 2: btnOutfitSkin2?.AddToClassList("active"); break;
        }
    }

    private void UpdateLensButtons()
    {
        btnLensNormal?.RemoveFromClassList("active");
        btnLensClothes?.RemoveFromClassList("active");

        switch (currentLensMode)
        {
            case 0: btnLensNormal?.AddToClassList("active"); break;
            case 1: btnLensClothes?.AddToClassList("active"); break;
        }
    }


    private void UpdateDisplay()
    {
        // ボタン状態の初期化
        UpdateOutfitButtons();
        UpdateLensButtons();
    }

    private string GetOutfitName(int index)
    {
        return index switch
        {
            0 => "Default",
            1 => "Skin 1",
            2 => "Skin 2",
            _ => "Unknown"
        };
    }

    public void Dispose()
    {
        // Overlay非表示
        HideCharacterOverlay();

        // イベント解除
        btnOutfitDefault?.UnregisterCallback<ClickEvent>(evt => SetOutfit(0));
        btnOutfitSkin1?.UnregisterCallback<ClickEvent>(evt => SetOutfit(1));
        btnOutfitSkin2?.UnregisterCallback<ClickEvent>(evt => SetOutfit(2));
        btnLensNormal?.UnregisterCallback<ClickEvent>(evt => SetLensMode(0));
        btnLensClothes?.UnregisterCallback<ClickEvent>(evt => SetLensMode(1));

        LogUIController.LogSystem("Operator View Disposed.");
    }
}
