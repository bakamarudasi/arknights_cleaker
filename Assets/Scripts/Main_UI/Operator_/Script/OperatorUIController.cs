using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// オペレーター画面のUIファサード
/// 単一責任: 各サブコントローラーの統合と画面全体のライフサイクル管理
/// </summary>
public class OperatorUIController : IViewController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private VisualElement characterDisplay;
    private Button btnOutfitDefault;
    private Button btnOutfitSkin1;
    private Button btnOutfitSkin2;
    private Button btnBack;

    // ========================================
    // サブコントローラー（分離された責任）
    // ========================================

    private OperatorLensController lensController;
    private OperatorGiftController giftController;
    private OperatorAffectionController affectionController;

    // ========================================
    // 状態
    // ========================================

    private int currentOutfit = 0;
    private CharacterInteractionZone[] _subscribedZones;

    // ========================================
    // コールバック参照（解除用）
    // ========================================

    private EventCallback<ClickEvent> callbackOutfit0;
    private EventCallback<ClickEvent> callbackOutfit1;
    private EventCallback<ClickEvent> callbackOutfit2;
    private EventCallback<ClickEvent> callbackBack;
    private EventCallback<ClickEvent> callbackCharacterClick;

    // ========================================
    // イベント
    // ========================================

    public event Action OnBackRequested;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        QueryElements();
        InitializeSubControllers();
        SetupCallbacks();
        SubscribeToEvents();

        // キャラ表示
        ShowCharacterOverlay();

        // 初期表示
        UpdateOutfitButtons();

        LogUIController.LogSystem("Operator View Initialized.");
    }

    private void QueryElements()
    {
        characterDisplay = root.Q<VisualElement>("character-display");
        btnOutfitDefault = root.Q<Button>("btn-outfit-default");
        btnOutfitSkin1 = root.Q<Button>("btn-outfit-skin1");
        btnOutfitSkin2 = root.Q<Button>("btn-outfit-skin2");
        btnBack = root.Q<Button>("btn-back");
    }

    private void InitializeSubControllers()
    {
        // レンズコントローラー
        lensController = new OperatorLensController();
        lensController.Initialize(root);
        lensController.OnLensModeChanged += OnLensModeChanged;

        // プレゼントコントローラー
        giftController = new OperatorGiftController();
        giftController.Initialize(root);
        giftController.OnGiftGiven += OnGiftGiven;

        // 好感度コントローラー
        affectionController = new OperatorAffectionController();
        affectionController.Initialize(root);
    }

    private void SetupCallbacks()
    {
        callbackOutfit0 = evt => SetOutfit(0);
        callbackOutfit1 = evt => SetOutfit(1);
        callbackOutfit2 = evt => SetOutfit(2);
        callbackBack = evt => OnBackRequested?.Invoke();
        callbackCharacterClick = evt => OnCharacterClicked(evt);

        btnOutfitDefault?.RegisterCallback(callbackOutfit0);
        btnOutfitSkin1?.RegisterCallback(callbackOutfit1);
        btnOutfitSkin2?.RegisterCallback(callbackOutfit2);
        btnBack?.RegisterCallback(callbackBack);
        characterDisplay?.RegisterCallback(callbackCharacterClick);
    }

    // ========================================
    // イベント購読
    // ========================================

    private void SubscribeToEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnAffectionChanged += OnAffectionChanged;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemCountChanged += OnItemCountChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnAffectionChanged -= OnAffectionChanged;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemCountChanged -= OnItemCountChanged;
        }
    }

    private void OnAffectionChanged(string characterId, int newValue, int delta)
    {
        affectionController.UpdateAffectionUI();
    }

    private void OnItemCountChanged(string itemId, int newCount)
    {
        lensController.SetupLensItemsUI();
        giftController.SetupGiftItemsUI();
    }

    private void OnLensModeChanged(int mode)
    {
        ApplyLensEffect(mode);
    }

    private void OnGiftGiven(ItemData item)
    {
        affectionController.UpdateAffectionUI();
    }

    // ========================================
    // キャラクター表示
    // ========================================

    private void ShowCharacterOverlay()
    {
        Debug.Log("[OperatorUI] ShowCharacterOverlay called (RenderTexture mode)");
        var presenter = OverlayCharacterPresenter.Instance;

        if (presenter != null)
        {
            presenter.SetDisplayArea(characterDisplay);
            presenter.Show();
            presenter.SetUpdateCallback(lensController.UpdateBattery);
            SubscribeToInteractionZones(presenter);
        }
        else
        {
            Debug.LogWarning("[OperatorUI] OverlayCharacterPresenter.Instance is NULL!");
            LogUIController.LogSystem("OverlayCharacterPresenter not found in scene.");
        }
    }

    private void HideCharacterOverlay()
    {
        UnsubscribeFromInteractionZones();

        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter != null)
        {
            presenter.ClearUpdateCallback();
            presenter.Hide();
        }
    }

    private void SubscribeToInteractionZones(OverlayCharacterPresenter presenter)
    {
        UnsubscribeFromInteractionZones();

        _subscribedZones = presenter.GetInteractionZones();
        foreach (var zone in _subscribedZones)
        {
            zone.OnZoneTouched += OnInteractionZoneTouched;
        }

        Debug.Log($"[OperatorUI] Subscribed to {_subscribedZones.Length} interaction zones");
    }

    private void UnsubscribeFromInteractionZones()
    {
        if (_subscribedZones == null) return;

        foreach (var zone in _subscribedZones)
        {
            if (zone != null)
            {
                zone.OnZoneTouched -= OnInteractionZoneTouched;
            }
        }
        _subscribedZones = null;
    }

    private void OnInteractionZoneTouched(CharacterInteractionZone.ZoneType zoneType, int comboCount)
    {
        affectionController.UpdateAffectionUI();

        switch (zoneType)
        {
            case CharacterInteractionZone.ZoneType.Head:
                if (comboCount >= 5)
                {
                    LogUIController.Msg("<color=#FF69B4>♪♪♪</color>");
                }
                break;
        }
    }

    // ========================================
    // 着せ替え
    // ========================================

    private void SetOutfit(int outfitIndex)
    {
        currentOutfit = outfitIndex;
        UpdateOutfitButtons();

        LogUIController.Msg($"Outfit changed to: {GetOutfitName(outfitIndex)}");
    }

    private void UpdateOutfitButtons()
    {
        btnOutfitDefault?.RemoveFromClassList("active");
        btnOutfitSkin1?.RemoveFromClassList("active");
        btnOutfitSkin2?.RemoveFromClassList("active");

        switch (currentOutfit)
        {
            case 0: btnOutfitDefault?.AddToClassList("active"); break;
            case 1: btnOutfitSkin1?.AddToClassList("active"); break;
            case 2: btnOutfitSkin2?.AddToClassList("active"); break;
        }
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

    // ========================================
    // レンズ効果
    // ========================================

    private void ApplyLensEffect(int lensMode)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        var lensItem = lensController.CurrentLensItem;
        if (lensItem != null && lensItem.lensSpecs.isLens)
        {
            Debug.Log($"[Lens] Filter: {lensItem.lensSpecs.filterMode}, Level: {lensMode}");
        }
    }

    // ========================================
    // キャラクターインタラクション
    // ========================================

    private void OnCharacterClicked(ClickEvent evt)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null || characterDisplay == null) return;

        Vector2 localPos = evt.localPosition;
        Rect contentRect = characterDisplay.contentRect;
        if (contentRect.width <= 0 || contentRect.height <= 0) return;

        Vector2 normalizedPos = new Vector2(
            localPos.x / contentRect.width,
            localPos.y / contentRect.height
        );

        var zone = presenter.GetInteractionZoneAt(normalizedPos);
        if (zone != null)
        {
            zone.HandleTouch();
            Debug.Log($"[OperatorUI] Touched zone: {zone.Type}");
        }
        else
        {
            if (AffectionManager.Instance != null)
            {
                AffectionManager.Instance.OnCharacterClicked();
            }
            Debug.Log("[OperatorUI] Character clicked (no zone)");
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        UnsubscribeFromEvents();
        UnsubscribeFromInteractionZones();
        HideCharacterOverlay();

        // サブコントローラーの解放
        if (lensController != null)
        {
            lensController.OnLensModeChanged -= OnLensModeChanged;
            lensController.Dispose();
        }

        if (giftController != null)
        {
            giftController.OnGiftGiven -= OnGiftGiven;
            giftController.Dispose();
        }

        affectionController?.Dispose();

        // コールバック解除
        if (callbackOutfit0 != null) btnOutfitDefault?.UnregisterCallback(callbackOutfit0);
        if (callbackOutfit1 != null) btnOutfitSkin1?.UnregisterCallback(callbackOutfit1);
        if (callbackOutfit2 != null) btnOutfitSkin2?.UnregisterCallback(callbackOutfit2);
        if (callbackBack != null) btnBack?.UnregisterCallback(callbackBack);
        if (callbackCharacterClick != null) characterDisplay?.UnregisterCallback(callbackCharacterClick);

        callbackOutfit0 = null;
        callbackOutfit1 = null;
        callbackOutfit2 = null;
        callbackBack = null;
        callbackCharacterClick = null;

        LogUIController.LogSystem("Operator View Disposed.");
    }
}
