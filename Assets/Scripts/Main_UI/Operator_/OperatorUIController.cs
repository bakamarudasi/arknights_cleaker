using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// オペレーター画面のUIファサード
/// 各サブコントローラーの統合と画面全体のライフサイクル管理
/// </summary>
public class OperatorUIController : IViewController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement _root;
    private VisualElement _characterDisplay;
    private Button _btnOutfitDefault;
    private Button _btnOutfitSkin1;
    private Button _btnOutfitSkin2;
    private Button _btnBack;

    // ========================================
    // サブコントローラー
    // ========================================

    private OperatorLensController _lensController;
    private OperatorGiftController _giftController;
    private OperatorAffectionController _affectionController;
    private OperatorTalkController _talkController;
    private OperatorTabController _tabController;

    // ========================================
    // シーンUI
    // ========================================

    private ISceneUI _currentSceneUI;
    private DefaultSceneUI _defaultSceneUI;
    private FullscreenSceneUI _fullscreenSceneUI;

    // ========================================
    // コールバック管理
    // ========================================

    private readonly UICallbackRegistry _callbacks = new UICallbackRegistry();
    private bool _isLensMouseMoveRegistered;

    // ========================================
    // 状態
    // ========================================

    private int _currentOutfit;
    private CharacterInteractionZone[] _subscribedZones;

    // ========================================
    // イベント
    // ========================================

    public event Action OnBackRequested;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement contentArea)
    {
        _root = contentArea;

        QueryElements();
        InitializeSubControllers();
        InitializeSceneUIs();
        SetupCallbacks();
        SubscribeToEvents();

        ShowCharacterOverlay();
        UpdateOutfitButtons();
        ApplyCurrentSceneUI();
        UpdateTalkControllerForCurrentScene();

        LogUIController.LogSystem("Operator View Initialized.");
    }

    private void QueryElements()
    {
        _characterDisplay = _root.Q<VisualElement>("character-display");
        _btnOutfitDefault = _root.Q<Button>("btn-outfit-default");
        _btnOutfitSkin1 = _root.Q<Button>("btn-outfit-skin1");
        _btnOutfitSkin2 = _root.Q<Button>("btn-outfit-skin2");
        _btnBack = _root.Q<Button>("btn-back");
    }

    private void InitializeSubControllers()
    {
        // レンズ
        _lensController = new OperatorLensController();
        _lensController.Initialize(_root);
        _lensController.OnLensModeChanged += OnLensModeChanged;
        _lensController.OnLensShapeChanged += shape => OverlayCharacterPresenter.Instance?.SetLensShape(shape);
        _lensController.OnLensSizeChanged += size => OverlayCharacterPresenter.Instance?.SetLensSize(size);

        // ギフト
        _giftController = new OperatorGiftController();
        _giftController.Initialize(_root);
        _giftController.OnGiftGiven += _ => _affectionController?.UpdateAffectionUI();

        // 好感度
        _affectionController = new OperatorAffectionController();
        _affectionController.Initialize(_root);

        // 会話
        _talkController = new OperatorTalkController();
        _talkController.Initialize(_root);
        _talkController.OnConversationEnded += () => _affectionController?.UpdateAffectionUI();

        // タブ（自動登録）
        _tabController = new OperatorTabController();
        _tabController.AutoRegister(_root, "outfit", "lens", "gift", "talk");
    }

    private void InitializeSceneUIs()
    {
        _defaultSceneUI = new DefaultSceneUI();
        _defaultSceneUI.Initialize(_root);

        _fullscreenSceneUI = new FullscreenSceneUI();
        _fullscreenSceneUI.Initialize(_root);

        _currentSceneUI = _defaultSceneUI;
    }

    private void SetupCallbacks()
    {
        // 衣装ボタン
        _callbacks.RegisterClick(_btnOutfitDefault, () => SetOutfit(0));
        _callbacks.RegisterClick(_btnOutfitSkin1, () => SetOutfit(1));
        _callbacks.RegisterClick(_btnOutfitSkin2, () => SetOutfit(2));

        // 戻るボタン
        _callbacks.RegisterClick(_btnBack, () => OnBackRequested?.Invoke());

        // キャラクタークリック
        _callbacks.RegisterClick(_characterDisplay, OnCharacterClicked);
    }

    // ========================================
    // イベント購読
    // ========================================

    private void SubscribeToEvents()
    {
        if (AffectionManager.Instance != null)
            AffectionManager.Instance.OnAffectionChanged += OnAffectionChanged;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemCountChanged += OnItemCountChanged;

        if (OverlayCharacterPresenter.Instance != null)
            OverlayCharacterPresenter.Instance.OnSceneChanged += OnSceneChanged;

        if (CostumeManager.Instance != null)
            CostumeManager.Instance.OnCostumeUnlocked += OnCostumeUnlocked;
    }

    private void UnsubscribeFromEvents()
    {
        if (AffectionManager.Instance != null)
            AffectionManager.Instance.OnAffectionChanged -= OnAffectionChanged;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemCountChanged -= OnItemCountChanged;

        if (OverlayCharacterPresenter.Instance != null)
            OverlayCharacterPresenter.Instance.OnSceneChanged -= OnSceneChanged;

        if (CostumeManager.Instance != null)
            CostumeManager.Instance.OnCostumeUnlocked -= OnCostumeUnlocked;
    }

    private void OnAffectionChanged(string characterId, int newValue, int delta)
        => _affectionController?.UpdateAffectionUI();

    private void OnCostumeUnlocked(string characterId, string costumeId)
        => UpdateOutfitButtons();

    private void OnItemCountChanged(string itemId, int newCount)
    {
        _lensController?.SetupLensItemsUI();
        _giftController?.SetupGiftItemsUI();
    }

    private void OnSceneChanged(string sceneId)
    {
        ApplyCurrentSceneUI();
        UpdateTalkControllerForCurrentScene();
    }

    // ========================================
    // シーンUI切り替え
    // ========================================

    private void UpdateTalkControllerForCurrentScene()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null || _talkController == null) return;

        _talkController.UpdateForScene(
            presenter.CurrentCharacter?.characterId,
            presenter.CurrentSceneData
        );
    }

    private void ApplyCurrentSceneUI()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        var sceneData = presenter.CurrentSceneData;
        _currentSceneUI?.Hide();

        if (sceneData != null && sceneData.hideSidePanel)
        {
            _fullscreenSceneUI.SetHideBackButton(sceneData.hideBackButton);
            _currentSceneUI = _fullscreenSceneUI;
        }
        else
        {
            _currentSceneUI = _defaultSceneUI;
        }

        _currentSceneUI?.Show();
    }

    public ISceneUI CurrentSceneUI => _currentSceneUI;

    // ========================================
    // キャラクター表示
    // ========================================

    private void ShowCharacterOverlay()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null)
        {
            Debug.LogWarning("[OperatorUI] OverlayCharacterPresenter.Instance is NULL!");
            return;
        }

        presenter.SetDisplayArea(_characterDisplay);
        presenter.Show();
        presenter.SetUpdateCallback(_lensController.UpdateBattery);
        SubscribeToInteractionZones(presenter);
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
    }

    private void UnsubscribeFromInteractionZones()
    {
        if (_subscribedZones == null) return;

        foreach (var zone in _subscribedZones)
        {
            if (zone != null)
                zone.OnZoneTouched -= OnInteractionZoneTouched;
        }
        _subscribedZones = null;
    }

    private void OnInteractionZoneTouched(CharacterInteractionZone.ZoneType zoneType, int comboCount)
    {
        _affectionController?.UpdateAffectionUI();

        if (zoneType == CharacterInteractionZone.ZoneType.Head && comboCount >= 5)
        {
            LogUIController.Msg("<color=#FF69B4>♪♪♪</color>");
        }
    }

    // ========================================
    // 着せ替え
    // ========================================

    private void SetOutfit(int outfitIndex)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        var characterId = presenter?.CurrentCharacter?.characterId;

        if (CostumeManager.Instance != null && !string.IsNullOrEmpty(characterId))
        {
            if (!CostumeManager.Instance.IsCostumeUnlockedByIndex(characterId, outfitIndex))
            {
                LogUIController.Msg("この衣装はまだ解放されていません");
                return;
            }
            CostumeManager.Instance.EquipCostumeByIndex(characterId, outfitIndex);
        }

        _currentOutfit = outfitIndex;
        UpdateOutfitButtons();

        if (presenter != null)
        {
            string sceneId = CostumeManager.CostumeIdToSceneId(
                CostumeManager.GetCostumeIdFromIndex(outfitIndex)
            );
            presenter.SetScene(sceneId);
        }

        LogUIController.Msg($"Outfit changed to: {GetOutfitName(outfitIndex)}");
    }

    private void UpdateOutfitButtons()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        var characterId = presenter?.CurrentCharacter?.characterId;
        var costumeManager = CostumeManager.Instance;

        Button[] buttons = { _btnOutfitDefault, _btnOutfitSkin1, _btnOutfitSkin2 };

        // 全ボタンのクラスをリセット
        foreach (var btn in buttons)
        {
            btn?.RemoveFromClassList("active");
            btn?.RemoveFromClassList("locked");
        }

        // ロック状態を更新
        if (costumeManager != null && !string.IsNullOrEmpty(characterId))
        {
            _btnOutfitDefault?.SetEnabled(true);

            for (int i = 1; i <= 2; i++)
            {
                bool unlocked = costumeManager.IsCostumeUnlockedByIndex(characterId, i);
                buttons[i]?.SetEnabled(unlocked);
                if (!unlocked) buttons[i]?.AddToClassList("locked");
            }
        }

        // 現在の衣装をアクティブ表示
        if (_currentOutfit >= 0 && _currentOutfit < buttons.Length)
        {
            buttons[_currentOutfit]?.AddToClassList("active");
        }
    }

    private static string GetOutfitName(int index) => index switch
    {
        0 => "Default",
        1 => "Skin 1",
        2 => "Skin 2",
        _ => "Unknown"
    };

    // ========================================
    // レンズ効果
    // ========================================

    private void OnLensModeChanged(int lensMode)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        if (lensMode > 0)
        {
            presenter.EnableLensMask(lensMode);
            RegisterLensMouseMove();
        }
        else
        {
            presenter.DisableLensMask();
            // マウス追従は登録したままでOK（レンズOFF時は無視される）
        }

        var lensItem = _lensController.CurrentLensItem;
        if (lensItem?.lensSpecs.isLens == true)
        {
            Debug.Log($"[Lens] Applied - Level: {lensMode}");
        }
    }

    private void RegisterLensMouseMove()
    {
        if (_isLensMouseMoveRegistered) return;

        _callbacks.RegisterMouseMove(_characterDisplay, evt =>
        {
            var presenter = OverlayCharacterPresenter.Instance;
            if (presenter == null || _characterDisplay == null) return;

            Rect rect = _characterDisplay.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            Vector2 normalizedPos = new Vector2(
                evt.localMousePosition.x / rect.width,
                evt.localMousePosition.y / rect.height
            );
            presenter.UpdateLensPosition(normalizedPos);
        });

        _isLensMouseMoveRegistered = true;
    }

    // ========================================
    // キャラクターインタラクション
    // ========================================

    private void OnCharacterClicked(ClickEvent evt)
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null || _characterDisplay == null) return;

        Rect rect = _characterDisplay.contentRect;
        if (rect.width <= 0 || rect.height <= 0) return;

        Vector2 normalizedPos = new Vector2(
            evt.localPosition.x / rect.width,
            evt.localPosition.y / rect.height
        );

        var zone = presenter.GetInteractionZoneAt(normalizedPos);
        if (zone != null)
        {
            zone.HandleTouch();
        }
        else
        {
            AffectionManager.Instance?.OnCharacterClicked();
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        // イベント解除
        UnsubscribeFromEvents();
        UnsubscribeFromInteractionZones();
        HideCharacterOverlay();

        // サブコントローラー解放
        _lensController?.Dispose();
        _giftController?.Dispose();
        _affectionController?.Dispose();
        _talkController?.Dispose();
        _tabController?.Dispose();

        // シーンUI解放
        _currentSceneUI?.Hide();
        _defaultSceneUI?.Dispose();
        _fullscreenSceneUI?.Dispose();

        // コールバック一括解除
        _callbacks.Dispose();

        LogUIController.LogSystem("Operator View Disposed.");
    }
}
