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

    // タブUI要素
    private Button tabIconOutfit;
    private Button tabIconLens;
    private Button tabIconGift;
    private Button tabIconTalk;
    private VisualElement tabOutfit;
    private VisualElement tabLens;
    private VisualElement tabGift;
    private VisualElement tabTalk;
    private Button btnCloseOutfit;
    private Button btnCloseLens;
    private Button btnCloseGift;
    private Button btnCloseTalk;

    // ========================================
    // サブコントローラー（分離された責任）
    // ========================================

    private OperatorLensController lensController;
    private OperatorGiftController giftController;
    private OperatorAffectionController affectionController;
    private OperatorTalkController talkController;

    // ========================================
    // シーンUI（ポーズ/シーンに応じたUI切り替え）
    // ========================================

    private ISceneUI _currentSceneUI;
    private DefaultSceneUI _defaultSceneUI;
    private FullscreenSceneUI _fullscreenSceneUI;

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

    // タブ用コールバック
    private EventCallback<ClickEvent> callbackTabOutfit;
    private EventCallback<ClickEvent> callbackTabLens;
    private EventCallback<ClickEvent> callbackTabGift;
    private EventCallback<ClickEvent> callbackTabTalk;
    private EventCallback<ClickEvent> callbackCloseOutfit;
    private EventCallback<ClickEvent> callbackCloseLens;
    private EventCallback<ClickEvent> callbackCloseGift;
    private EventCallback<ClickEvent> callbackCloseTalk;

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
        InitializeSceneUIs();
        SetupCallbacks();
        SubscribeToEvents();

        // キャラ表示
        ShowCharacterOverlay();

        // 初期表示
        UpdateOutfitButtons();

        // 初期シーンUIを表示
        ApplyCurrentPoseUI();

        // 会話リストの初期化
        UpdateTalkControllerForCurrentPose();

        LogUIController.LogSystem("Operator View Initialized.");
    }

    private void QueryElements()
    {
        characterDisplay = root.Q<VisualElement>("character-display");
        btnOutfitDefault = root.Q<Button>("btn-outfit-default");
        btnOutfitSkin1 = root.Q<Button>("btn-outfit-skin1");
        btnOutfitSkin2 = root.Q<Button>("btn-outfit-skin2");
        btnBack = root.Q<Button>("btn-back");

        // タブ要素
        tabIconOutfit = root.Q<Button>("tab-icon-outfit");
        tabIconLens = root.Q<Button>("tab-icon-lens");
        tabIconGift = root.Q<Button>("tab-icon-gift");
        tabIconTalk = root.Q<Button>("tab-icon-talk");
        tabOutfit = root.Q<VisualElement>("tab-outfit");
        tabLens = root.Q<VisualElement>("tab-lens");
        tabGift = root.Q<VisualElement>("tab-gift");
        tabTalk = root.Q<VisualElement>("tab-talk");
        btnCloseOutfit = root.Q<Button>("btn-close-outfit");
        btnCloseLens = root.Q<Button>("btn-close-lens");
        btnCloseGift = root.Q<Button>("btn-close-gift");
        btnCloseTalk = root.Q<Button>("btn-close-talk");
    }

    private void InitializeSubControllers()
    {
        // レンズコントローラー
        lensController = new OperatorLensController();
        lensController.Initialize(root);
        lensController.OnLensModeChanged += OnLensModeChanged;
        lensController.OnLensShapeChanged += OnLensShapeChanged;
        lensController.OnLensSizeChanged += OnLensSizeChanged;

        // プレゼントコントローラー
        giftController = new OperatorGiftController();
        giftController.Initialize(root);
        giftController.OnGiftGiven += OnGiftGiven;

        // 好感度コントローラー
        affectionController = new OperatorAffectionController();
        affectionController.Initialize(root);

        // 会話コントローラー
        talkController = new OperatorTalkController();
        talkController.Initialize(root);
        talkController.OnConversationStarted += OnConversationStarted;
        talkController.OnConversationEnded += OnConversationEnded;
    }

    private void InitializeSceneUIs()
    {
        // デフォルトシーンUI
        _defaultSceneUI = new DefaultSceneUI();
        _defaultSceneUI.Initialize(root);

        // フルスクリーンシーンUI
        _fullscreenSceneUI = new FullscreenSceneUI();
        _fullscreenSceneUI.Initialize(root);

        // 初期状態はデフォルト
        _currentSceneUI = _defaultSceneUI;
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

        // タブ用コールバック
        callbackTabOutfit = evt => ToggleTab("outfit");
        callbackTabLens = evt => ToggleTab("lens");
        callbackTabGift = evt => ToggleTab("gift");
        callbackTabTalk = evt => ToggleTab("talk");
        callbackCloseOutfit = evt => CloseTab("outfit");
        callbackCloseLens = evt => CloseTab("lens");
        callbackCloseGift = evt => CloseTab("gift");
        callbackCloseTalk = evt => CloseTab("talk");

        tabIconOutfit?.RegisterCallback(callbackTabOutfit);
        tabIconLens?.RegisterCallback(callbackTabLens);
        tabIconGift?.RegisterCallback(callbackTabGift);
        tabIconTalk?.RegisterCallback(callbackTabTalk);
        btnCloseOutfit?.RegisterCallback(callbackCloseOutfit);
        btnCloseLens?.RegisterCallback(callbackCloseLens);
        btnCloseGift?.RegisterCallback(callbackCloseGift);
        btnCloseTalk?.RegisterCallback(callbackCloseTalk);
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

        // Presenterのポーズ変更イベント
        if (OverlayCharacterPresenter.Instance != null)
        {
            OverlayCharacterPresenter.Instance.OnPoseChanged += OnPoseChanged;
        }

        // 衣装解放イベント
        if (CostumeManager.Instance != null)
        {
            CostumeManager.Instance.OnCostumeUnlocked += OnCostumeUnlocked;
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

        // Presenterのポーズ変更イベント解除
        if (OverlayCharacterPresenter.Instance != null)
        {
            OverlayCharacterPresenter.Instance.OnPoseChanged -= OnPoseChanged;
        }

        // 衣装解放イベント解除
        if (CostumeManager.Instance != null)
        {
            CostumeManager.Instance.OnCostumeUnlocked -= OnCostumeUnlocked;
        }
    }

    private void OnAffectionChanged(string characterId, int newValue, int delta)
    {
        affectionController.UpdateAffectionUI();
    }

    private void OnCostumeUnlocked(string characterId, string costumeId)
    {
        // 衣装が解放されたらボタンの状態を更新
        UpdateOutfitButtons();
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

    private void OnConversationStarted(PoseConversation conv)
    {
        Debug.Log($"[OperatorUI] Conversation started: {conv.title}");
    }

    private void OnConversationEnded()
    {
        Debug.Log("[OperatorUI] Conversation ended");
        // 好感度が上がった可能性があるので更新
        affectionController?.UpdateAffectionUI();
    }

    private void OnLensShapeChanged(LensMaskController.LensShape shape)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        presenter?.SetLensShape(shape);
    }

    private void OnLensSizeChanged(float size)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        presenter?.SetLensSize(size);
    }

    // ========================================
    // シーンUI切り替え
    // ========================================

    private void OnPoseChanged(string poseId)
    {
        ApplyCurrentPoseUI();
        UpdateTalkControllerForCurrentPose();
    }

    private void UpdateTalkControllerForCurrentPose()
    {
        if (talkController == null) return;

        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        var characterId = presenter.CurrentCharacterData?.characterId;
        var poseEntry = presenter.CurrentPoseEntry;

        talkController.UpdateForPose(characterId, poseEntry);
    }

    /// <summary>
    /// 現在のポーズに応じたUIを適用
    /// </summary>
    private void ApplyCurrentPoseUI()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        var poseEntry = presenter.CurrentPoseEntry;

        // 現在のシーンUIを非表示
        _currentSceneUI?.Hide();

        // ポーズ設定に応じてシーンUIを選択
        if (poseEntry != null && poseEntry.hideSidePanel)
        {
            _fullscreenSceneUI.SetHideBackButton(poseEntry.hideBackButton);
            _currentSceneUI = _fullscreenSceneUI;
        }
        else
        {
            _currentSceneUI = _defaultSceneUI;
        }

        // 新しいシーンUIを表示
        _currentSceneUI?.Show();

        UnityEngine.Debug.Log($"[OperatorUI] Scene UI switched: {_currentSceneUI?.GetType().Name}");
    }

    /// <summary>
    /// 現在のシーンUIを取得
    /// </summary>
    public ISceneUI CurrentSceneUI => _currentSceneUI;

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
        var presenter = OverlayCharacterPresenter.Instance;
        var characterId = presenter?.CurrentCharacterData?.characterId;

        // ロック状態チェック
        if (CostumeManager.Instance != null && !string.IsNullOrEmpty(characterId))
        {
            if (!CostumeManager.Instance.IsCostumeUnlockedByIndex(characterId, outfitIndex))
            {
                LogUIController.Msg("この衣装はまだ解放されていません");
                return;
            }

            // CostumeManagerに装備を通知
            CostumeManager.Instance.EquipCostumeByIndex(characterId, outfitIndex);
        }

        currentOutfit = outfitIndex;
        UpdateOutfitButtons();

        // OverlayCharacterPresenterでポーズ切り替え
        if (presenter != null)
        {
            string poseId = CostumeManager.GetCostumeIdFromIndex(outfitIndex);
            poseId = CostumeManager.CostumeIdToPoseId(poseId);
            presenter.SetPose(poseId);
        }

        LogUIController.Msg($"Outfit changed to: {GetOutfitName(outfitIndex)}");
    }

    private void UpdateOutfitButtons()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        var characterId = presenter?.CurrentCharacterData?.characterId;
        var costumeManager = CostumeManager.Instance;

        // activeクラスを解除
        btnOutfitDefault?.RemoveFromClassList("active");
        btnOutfitSkin1?.RemoveFromClassList("active");
        btnOutfitSkin2?.RemoveFromClassList("active");

        // lockedクラスを解除
        btnOutfitDefault?.RemoveFromClassList("locked");
        btnOutfitSkin1?.RemoveFromClassList("locked");
        btnOutfitSkin2?.RemoveFromClassList("locked");

        // ロック状態をチェックしてボタンを更新
        if (costumeManager != null && !string.IsNullOrEmpty(characterId))
        {
            // デフォルトは常に有効
            btnOutfitDefault?.SetEnabled(true);

            // Skin1のロック状態
            bool skin1Unlocked = costumeManager.IsCostumeUnlockedByIndex(characterId, 1);
            btnOutfitSkin1?.SetEnabled(skin1Unlocked);
            if (!skin1Unlocked) btnOutfitSkin1?.AddToClassList("locked");

            // Skin2のロック状態
            bool skin2Unlocked = costumeManager.IsCostumeUnlockedByIndex(characterId, 2);
            btnOutfitSkin2?.SetEnabled(skin2Unlocked);
            if (!skin2Unlocked) btnOutfitSkin2?.AddToClassList("locked");
        }

        // 現在の衣装をアクティブ表示
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
    // タブ操作
    // ========================================

    private void ToggleTab(string tabName)
    {
        var (tab, icon) = GetTabElements(tabName);
        if (tab == null) return;

        bool isHidden = tab.ClassListContains("hidden");
        if (isHidden)
        {
            OpenTab(tabName);
        }
        else
        {
            CloseTab(tabName);
        }
    }

    private void OpenTab(string tabName)
    {
        var (tab, icon) = GetTabElements(tabName);
        if (tab == null) return;

        tab.RemoveFromClassList("hidden");
        icon?.AddToClassList("active");
    }

    private void CloseTab(string tabName)
    {
        var (tab, icon) = GetTabElements(tabName);
        if (tab == null) return;

        tab.AddToClassList("hidden");
        icon?.RemoveFromClassList("active");
    }

    private (VisualElement tab, Button icon) GetTabElements(string tabName)
    {
        return tabName switch
        {
            "outfit" => (tabOutfit, tabIconOutfit),
            "lens" => (tabLens, tabIconLens),
            "gift" => (tabGift, tabIconGift),
            "talk" => (tabTalk, tabIconTalk),
            _ => (null, null)
        };
    }

    // ========================================
    // レンズ効果
    // ========================================

    private EventCallback<MouseMoveEvent> callbackLensMouseMove;

    private void ApplyLensEffect(int lensMode)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        if (lensMode > 0)
        {
            // レンズモードON → SpriteMaskモードを有効化
            presenter.EnableLensMask(lensMode);

            // マウス追従を開始
            if (callbackLensMouseMove == null)
            {
                callbackLensMouseMove = OnLensMouseMove;
                characterDisplay?.RegisterCallback(callbackLensMouseMove);
            }
        }
        else
        {
            // レンズモードOFF → SpriteMaskモードを無効化
            presenter.DisableLensMask();

            // マウス追従を停止
            if (callbackLensMouseMove != null)
            {
                characterDisplay?.UnregisterCallback(callbackLensMouseMove);
                callbackLensMouseMove = null;
            }
        }

        var lensItem = lensController.CurrentLensItem;
        if (lensItem != null && lensItem.lensSpecs.isLens)
        {
            Debug.Log($"[Lens] Applied - Filter: {lensItem.lensSpecs.filterMode}, Level: {lensMode}, MaskMode: {lensMode > 0}");
        }
    }

    private void OnLensMouseMove(MouseMoveEvent evt)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null || characterDisplay == null) return;

        Rect contentRect = characterDisplay.contentRect;
        if (contentRect.width <= 0 || contentRect.height <= 0) return;

        // 正規化座標を計算
        Vector2 normalizedPos = new Vector2(
            evt.localMousePosition.x / contentRect.width,
            evt.localMousePosition.y / contentRect.height
        );

        // レンズ位置を更新
        presenter.UpdateLensPosition(normalizedPos);
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
            lensController.OnLensShapeChanged -= OnLensShapeChanged;
            lensController.OnLensSizeChanged -= OnLensSizeChanged;
            lensController.Dispose();
        }

        if (giftController != null)
        {
            giftController.OnGiftGiven -= OnGiftGiven;
            giftController.Dispose();
        }

        affectionController?.Dispose();

        if (talkController != null)
        {
            talkController.OnConversationStarted -= OnConversationStarted;
            talkController.OnConversationEnded -= OnConversationEnded;
            talkController.Dispose();
        }

        // シーンUIの解放
        _currentSceneUI?.Hide();
        _defaultSceneUI?.Dispose();
        _fullscreenSceneUI?.Dispose();
        _currentSceneUI = null;
        _defaultSceneUI = null;
        _fullscreenSceneUI = null;

        // コールバック解除
        if (callbackOutfit0 != null) btnOutfitDefault?.UnregisterCallback(callbackOutfit0);
        if (callbackOutfit1 != null) btnOutfitSkin1?.UnregisterCallback(callbackOutfit1);
        if (callbackOutfit2 != null) btnOutfitSkin2?.UnregisterCallback(callbackOutfit2);
        if (callbackBack != null) btnBack?.UnregisterCallback(callbackBack);
        if (callbackCharacterClick != null) characterDisplay?.UnregisterCallback(callbackCharacterClick);
        if (callbackLensMouseMove != null) characterDisplay?.UnregisterCallback(callbackLensMouseMove);

        // タブ用コールバック解除
        if (callbackTabOutfit != null) tabIconOutfit?.UnregisterCallback(callbackTabOutfit);
        if (callbackTabLens != null) tabIconLens?.UnregisterCallback(callbackTabLens);
        if (callbackTabGift != null) tabIconGift?.UnregisterCallback(callbackTabGift);
        if (callbackTabTalk != null) tabIconTalk?.UnregisterCallback(callbackTabTalk);
        if (callbackCloseOutfit != null) btnCloseOutfit?.UnregisterCallback(callbackCloseOutfit);
        if (callbackCloseLens != null) btnCloseLens?.UnregisterCallback(callbackCloseLens);
        if (callbackCloseGift != null) btnCloseGift?.UnregisterCallback(callbackCloseGift);
        if (callbackCloseTalk != null) btnCloseTalk?.UnregisterCallback(callbackCloseTalk);

        callbackOutfit0 = null;
        callbackOutfit1 = null;
        callbackOutfit2 = null;
        callbackBack = null;
        callbackCharacterClick = null;
        callbackLensMouseMove = null;
        callbackTabOutfit = null;
        callbackTabLens = null;
        callbackTabGift = null;
        callbackTabTalk = null;
        callbackCloseOutfit = null;
        callbackCloseLens = null;
        callbackCloseGift = null;
        callbackCloseTalk = null;

        LogUIController.LogSystem("Operator View Disposed.");
    }
}
