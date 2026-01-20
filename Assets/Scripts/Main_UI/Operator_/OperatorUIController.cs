using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// オペレーター画面のUIファサード（フルスクリーンレイアウト版）
/// 各サブコントローラーの統合と画面全体のライフサイクル管理
/// </summary>
public class OperatorUIController : BaseUIController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement _characterDisplay;

    // 背景エフェクト
    private VisualElement _bgCanvas;
    private VisualElement _vignette;
    private VisualElement _levelupFlash;

    // 左サイドバー
    private Button _navOutfit;
    private Button _navGift;
    private Button _navTalk;
    private Button _navLens;
    private Button _btnBack;
    private Label _lvDisplay;

    // ステータスゲージ
    private VisualElement _barAff;
    private Label _txtAff;
    private VisualElement _barExc;
    private Label _txtExc;

    // キャラクター名
    private Label _characterName;

    // モーダル
    private VisualElement _modalOverlay;
    private VisualElement _modalPanel;
    private Label _modalTitle;
    private Button _btnModalClose;
    private VisualElement _contentOutfit;
    private VisualElement _contentGift;
    private VisualElement _contentTalk;
    private VisualElement _contentLens;

    // 衣装ボタン
    private Button _btnOutfitDefault;
    private Button _btnOutfitSkin1;
    private Button _btnOutfitSkin2;

    // ========================================
    // サブコントローラー
    // ========================================

    private OperatorLensController _lensController;
    private OperatorGiftController _giftController;
    private OperatorTalkController _talkController;
    private MessageWindowController _messageController;
    private ZoomWindowManager _zoomWindowManager;

    // ズーム窓コンテナ
    private VisualElement _zoomContainer;

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
    private string _currentModalTab;

    // ========================================
    // イベント
    // ========================================

    public event Action OnBackRequested;

    // ========================================
    // 初期化（BaseUIControllerのオーバーライド）
    // ========================================

    protected override void QueryElements()
    {
        _characterDisplay = root.Q<VisualElement>("character-display");

        // 背景エフェクト
        _bgCanvas = root.Q<VisualElement>("bg-canvas");
        _vignette = root.Q<VisualElement>("vignette");
        _levelupFlash = root.Q<VisualElement>("levelup-flash");

        // 左サイドバー
        _navOutfit = root.Q<Button>("nav-outfit");
        _navGift = root.Q<Button>("nav-gift");
        _navTalk = root.Q<Button>("nav-talk");
        _navLens = root.Q<Button>("nav-lens");
        _btnBack = root.Q<Button>("btn-back");
        _lvDisplay = root.Q<Label>("lv-display");

        // ステータスゲージ
        _barAff = root.Q<VisualElement>("bar-aff");
        _txtAff = root.Q<Label>("txt-aff");
        _barExc = root.Q<VisualElement>("bar-exc");
        _txtExc = root.Q<Label>("txt-exc");

        // キャラクター名
        _characterName = root.Q<Label>("character-name");

        // モーダル
        _modalOverlay = root.Q<VisualElement>("modal-overlay");
        _modalPanel = root.Q<VisualElement>("modal-panel");
        _modalTitle = root.Q<Label>("modal-title");
        _btnModalClose = root.Q<Button>("btn-modal-close");
        _contentOutfit = root.Q<VisualElement>("content-outfit");
        _contentGift = root.Q<VisualElement>("content-gift");
        _contentTalk = root.Q<VisualElement>("content-talk");
        _contentLens = root.Q<VisualElement>("content-lens");

        // 衣装ボタン
        _btnOutfitDefault = root.Q<Button>("btn-outfit-default");
        _btnOutfitSkin1 = root.Q<Button>("btn-outfit-skin1");
        _btnOutfitSkin2 = root.Q<Button>("btn-outfit-skin2");

        // ズーム窓コンテナ
        _zoomContainer = root.Q<VisualElement>("zoom-container");
    }

    protected override void InitializeSubControllers()
    {
        // レンズ（モーダル内）
        _lensController = new OperatorLensController();
        _lensController.Initialize(root);
        _lensController.OnLensModeChanged += OnLensModeChanged;
        _lensController.OnLensShapeChanged += shape => OverlayCharacterPresenter.Instance?.SetLensShape(shape);
        _lensController.OnLensSizeChanged += size => OverlayCharacterPresenter.Instance?.SetLensSize(size);

        // ギフト
        _giftController = new OperatorGiftController();
        _giftController.Initialize(root);
        _giftController.OnGiftGiven += _ => UpdateStatusUI();

        // 会話
        _talkController = new OperatorTalkController();
        _talkController.Initialize(root);
        _talkController.OnConversationEnded += () => UpdateStatusUI();

        // メッセージウィンドウ
        _messageController = new MessageWindowController();
        _messageController.Initialize(root);

        // ズーム窓マネージャー
        InitializeZoomWindowManager();

        // シーンUI初期化
        InitializeSceneUIs();
    }

    private void InitializeZoomWindowManager()
    {
        // 既存のマネージャーを使用、なければ作成
        _zoomWindowManager = ZoomWindowManager.Instance;
        if (_zoomWindowManager == null)
        {
            var go = new GameObject("ZoomWindowManager");
            _zoomWindowManager = go.AddComponent<ZoomWindowManager>();
        }

        // 現在のシーンデータで初期化
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter != null && _zoomContainer != null)
        {
            _zoomWindowManager.Initialize(_zoomContainer, presenter.CurrentSceneData);
        }

        // イベント購読
        _zoomWindowManager.OnZoomWindowOpened += OnZoomWindowOpened;
        _zoomWindowManager.OnAllWindowsClosed += OnAllZoomWindowsClosed;
    }

    private void InitializeSceneUIs()
    {
        _defaultSceneUI = new DefaultSceneUI();
        _defaultSceneUI.Initialize(root);

        _fullscreenSceneUI = new FullscreenSceneUI();
        _fullscreenSceneUI.Initialize(root);

        _currentSceneUI = _defaultSceneUI;
    }

    protected override void BindUIEvents()
    {
        // 左サイドバーナビゲーション
        _callbacks.RegisterClick(_navOutfit, () => OpenModal("outfit", "COSTUME"));
        _callbacks.RegisterClick(_navGift, () => OpenModal("gift", "PRESENT"));
        _callbacks.RegisterClick(_navTalk, () => OpenModal("talk", "TALK"));
        _callbacks.RegisterClick(_navLens, () => OpenModal("lens", "LENS"));

        // 戻るボタン
        _callbacks.RegisterClick(_btnBack, () => OnBackRequested?.Invoke());

        // モーダル閉じる
        _callbacks.RegisterClick(_btnModalClose, CloseModal);
        _callbacks.RegisterClick(_modalOverlay, evt =>
        {
            // パネル外クリックで閉じる
            if (evt.target == _modalOverlay)
                CloseModal();
        });

        // 衣装ボタン
        _callbacks.RegisterClick(_btnOutfitDefault, () => SetOutfit(0));
        _callbacks.RegisterClick(_btnOutfitSkin1, () => SetOutfit(1));
        _callbacks.RegisterClick(_btnOutfitSkin2, () => SetOutfit(2));

        // キャラクタークリック
        _callbacks.RegisterClick(_characterDisplay, OnCharacterClicked);
    }

    // ========================================
    // モーダル制御
    // ========================================

    private void OpenModal(string tabId, string title)
    {
        _currentModalTab = tabId;

        // タイトル設定
        if (_modalTitle != null)
            _modalTitle.text = title;

        // 全コンテンツを非表示
        _contentOutfit?.AddToClassList("hidden");
        _contentGift?.AddToClassList("hidden");
        _contentTalk?.AddToClassList("hidden");
        _contentLens?.AddToClassList("hidden");

        // 対象コンテンツを表示
        var content = tabId switch
        {
            "outfit" => _contentOutfit,
            "gift" => _contentGift,
            "talk" => _contentTalk,
            "lens" => _contentLens,
            _ => null
        };
        content?.RemoveFromClassList("hidden");

        // ナビゲーションのアクティブ状態更新
        UpdateNavActiveState(tabId);

        // モーダル表示
        _modalOverlay?.RemoveFromClassList("hidden");
    }

    private void CloseModal()
    {
        _modalOverlay?.AddToClassList("hidden");
        _currentModalTab = null;
        ClearNavActiveState();
    }

    private void UpdateNavActiveState(string activeTab)
    {
        _navOutfit?.RemoveFromClassList("active");
        _navGift?.RemoveFromClassList("active");
        _navTalk?.RemoveFromClassList("active");
        _navLens?.RemoveFromClassList("active");

        var activeNav = activeTab switch
        {
            "outfit" => _navOutfit,
            "gift" => _navGift,
            "talk" => _navTalk,
            "lens" => _navLens,
            _ => null
        };
        activeNav?.AddToClassList("active");
    }

    private void ClearNavActiveState()
    {
        _navOutfit?.RemoveFromClassList("active");
        _navGift?.RemoveFromClassList("active");
        _navTalk?.RemoveFromClassList("active");
        _navLens?.RemoveFromClassList("active");
    }

    // ========================================
    // イベント購読（BaseUIControllerのオーバーライド）
    // ========================================

    protected override void BindGameEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnAffectionChanged += OnAffectionChanged;
            AffectionManager.Instance.OnDialogueRequested += OnDialogueRequested;
            AffectionManager.Instance.OnAffectionLevelUp += OnAffectionLevelUp;
        }

        if (ExcitementManager.Instance != null)
        {
            ExcitementManager.Instance.OnExcitementChanged += OnExcitementChanged;
            ExcitementManager.Instance.OnExcitementLevelChanged += UpdateExcitementEffects;
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemCountChanged += OnItemCountChanged;

        if (OverlayCharacterPresenter.Instance != null)
            OverlayCharacterPresenter.Instance.OnSceneChanged += OnSceneChanged;

        if (CostumeManager.Instance != null)
            CostumeManager.Instance.OnCostumeUnlocked += OnCostumeUnlocked;
    }

    protected override void OnPostInitialize()
    {
        ShowCharacterOverlay();
        UpdateOutfitButtons();
        ApplyCurrentSceneUI();
        UpdateTalkControllerForCurrentScene();
        UpdateStatusUI();
        UpdateExcitementEffects(0);

        // 初期メッセージ
        _messageController?.ShowMessage("......やっと来た。待ちくたびれたわよ？", GetCurrentCharacterName(), 3f);

        LogUIController.LogSystem($"{LogTag} Operator View Initialized (Fullscreen Layout).");
    }

    // ========================================
    // 破棄（BaseUIControllerのオーバーライド）
    // ========================================

    protected override void UnbindGameEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnAffectionChanged -= OnAffectionChanged;
            AffectionManager.Instance.OnDialogueRequested -= OnDialogueRequested;
            AffectionManager.Instance.OnAffectionLevelUp -= OnAffectionLevelUp;
        }

        if (ExcitementManager.Instance != null)
        {
            ExcitementManager.Instance.OnExcitementChanged -= OnExcitementChanged;
            ExcitementManager.Instance.OnExcitementLevelChanged -= UpdateExcitementEffects;
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemCountChanged -= OnItemCountChanged;

        if (OverlayCharacterPresenter.Instance != null)
            OverlayCharacterPresenter.Instance.OnSceneChanged -= OnSceneChanged;

        if (CostumeManager.Instance != null)
            CostumeManager.Instance.OnCostumeUnlocked -= OnCostumeUnlocked;
    }

    protected override void UnbindUIEvents()
    {
        // コールバック一括解除
        _callbacks.Dispose();
    }

    protected override void OnPreDispose()
    {
        UnsubscribeFromInteractionZones();
        HideCharacterOverlay();
    }

    protected override void DisposeSubControllers()
    {
        _lensController?.Dispose();
        _giftController?.Dispose();
        _talkController?.Dispose();
        _messageController?.Dispose();

        // ズーム窓マネージャーのイベント解除
        if (_zoomWindowManager != null)
        {
            _zoomWindowManager.OnZoomWindowOpened -= OnZoomWindowOpened;
            _zoomWindowManager.OnAllWindowsClosed -= OnAllZoomWindowsClosed;
        }

        // シーンUI解放
        _currentSceneUI?.Hide();
        _defaultSceneUI?.Dispose();
        _fullscreenSceneUI?.Dispose();
    }

    protected override void OnPostDispose()
    {
        LogUIController.LogSystem($"{LogTag} Operator View Disposed.");
    }

    private void OnAffectionChanged(string characterId, int newValue, int delta)
    {
        UpdateStatusUI();
    }

    private void OnExcitementChanged(float newValue, float delta)
    {
        UpdateExcitementUI();
    }

    private void OnDialogueRequested(string dialogue)
    {
        _messageController?.ShowMessage(dialogue, GetCurrentCharacterName(), 3f);
    }

    private void OnAffectionLevelUp(string characterId, AffectionLevel newLevel)
    {
        // フラッシュエフェクト
        PlayLevelUpFlash();

        // レベルアップメッセージ
        string message = !string.IsNullOrEmpty(newLevel.levelUpMessage)
            ? newLevel.levelUpMessage
            : $"好感度が上がったわ！ (Lv.{newLevel.level} {newLevel.levelName})";

        _messageController?.ShowMessage(message, GetCurrentCharacterName(), 4f);

        // UI更新
        UpdateStatusUI();
    }

    private void PlayLevelUpFlash()
    {
        if (_levelupFlash == null) return;

        // フラッシュ開始
        _levelupFlash.RemoveFromClassList("fade-out");
        _levelupFlash.AddToClassList("active");

        // 少し待ってからフェードアウト
        _levelupFlash.schedule.Execute(() =>
        {
            _levelupFlash.RemoveFromClassList("active");
            _levelupFlash.AddToClassList("fade-out");
        }).StartingIn(150);

        // フェードアウト完了後にクラスをクリア
        _levelupFlash.schedule.Execute(() =>
        {
            _levelupFlash.RemoveFromClassList("fade-out");
        }).StartingIn(1000);
    }

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
        UpdateCharacterNameDisplay();

        // ズーム窓マネージャーにシーン変更を通知
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter != null && _zoomWindowManager != null)
        {
            _zoomWindowManager.OnSceneChanged(presenter.CurrentSceneData);
        }
    }

    // ========================================
    // UI更新
    // ========================================

    private void UpdateStatusUI()
    {
        UpdateAffectionUI();
        UpdateExcitementUI();
    }

    private void UpdateAffectionUI()
    {
        var affManager = AffectionManager.Instance;
        if (affManager == null) return;

        int current = affManager.GetCurrentAffection();
        var level = affManager.GetCurrentAffectionLevel();

        // レベル表示
        int levelNum = level?.level ?? 1;
        if (_lvDisplay != null)
            _lvDisplay.text = $"Lv.{levelNum}";

        // EXPバー
        int nextThreshold = level?.nextThreshold ?? 100;
        int prevThreshold = level?.threshold ?? 0;
        int expInLevel = current - prevThreshold;
        int expNeeded = nextThreshold - prevThreshold;
        float percent = expNeeded > 0 ? (float)expInLevel / expNeeded * 100f : 100f;

        if (_barAff != null)
            _barAff.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));

        if (_txtAff != null)
            _txtAff.text = $"{expInLevel} / {expNeeded}";
    }

    private void UpdateExcitementUI()
    {
        var excManager = ExcitementManager.Instance;
        if (excManager == null) return;

        float percent = excManager.ExcitementPercent;

        if (_barExc != null)
            _barExc.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));

        if (_txtExc != null)
            _txtExc.text = $"{Mathf.FloorToInt(percent)}%";
    }

    private void UpdateExcitementEffects(int level)
    {
        // 背景色変化
        _bgCanvas?.RemoveFromClassList("excited-low");
        _bgCanvas?.RemoveFromClassList("excited-high");

        _vignette?.RemoveFromClassList("excited-low");
        _vignette?.RemoveFromClassList("excited-high");

        if (level >= 2)
        {
            _bgCanvas?.AddToClassList("excited-high");
            _vignette?.AddToClassList("excited-high");
        }
        else if (level >= 1)
        {
            _bgCanvas?.AddToClassList("excited-low");
            _vignette?.AddToClassList("excited-low");
        }
    }

    private void UpdateCharacterNameDisplay()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter?.CurrentCharacter == null) return;

        if (_characterName != null)
            _characterName.text = presenter.CurrentCharacter.displayName ?? presenter.CurrentCharacter.characterId;
    }

    private string GetCurrentCharacterName()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        return presenter?.CurrentCharacter?.displayName ?? "???";
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

        UpdateCharacterNameDisplay();
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
        UpdateStatusUI();

        // ズーム窓マネージャーに通知
        _zoomWindowManager?.OnZoneTouched(zoneType, comboCount);

        // ズーム窓がアクティブでない場合のみセリフ表示
        if (_zoomWindowManager == null || !_zoomWindowManager.HasActiveWindows)
        {
            string dialogue = GetZoneDialogue(zoneType, comboCount);
            if (!string.IsNullOrEmpty(dialogue))
            {
                _messageController?.ShowMessage(dialogue, GetCurrentCharacterName(), 2.5f);
            }
        }

        if (zoneType == CharacterInteractionZone.ZoneType.Head && comboCount >= 5)
        {
            LogUIController.Msg("<color=#FF69B4>...</color>");
        }
    }

    private void OnZoomWindowOpened(CharacterInteractionZone.ZoneType zoneType)
    {
        // ズーム窓が開いた時の処理
        Debug.Log($"[OperatorUI] Zoom window opened for zone: {zoneType}");
    }

    private void OnAllZoomWindowsClosed()
    {
        // 全ズーム窓が閉じた時の処理
        Debug.Log("[OperatorUI] All zoom windows closed");
    }

    private string GetZoneDialogue(CharacterInteractionZone.ZoneType zoneType, int comboCount)
    {
        var affManager = AffectionManager.Instance;
        int level = affManager?.GetLevel(OverlayCharacterPresenter.Instance?.CurrentCharacter?.characterId) ?? 1;

        // レベルとコンボに応じたセリフ（サンプル）
        return (zoneType, level, comboCount) switch
        {
            (CharacterInteractionZone.ZoneType.Head, >= 3, >= 3) => "......ふふ、あなたの手、落ち着くわ。",
            (CharacterInteractionZone.ZoneType.Head, >= 1, _) => "あ、なでなでするの？ 子供扱いしないでよね。",
            (CharacterInteractionZone.ZoneType.Body, >= 3, _) => "......もう、わがままなんだから。",
            (CharacterInteractionZone.ZoneType.Body, >= 1, _) => "くすぐったいってば！",
            (CharacterInteractionZone.ZoneType.Hand, >= 3, _) => "ギュッてしてると、安心する。",
            (CharacterInteractionZone.ZoneType.Hand, >= 1, _) => "手、繋ぎたいの？ 仕方ないわね。",
            _ => null
        };
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
                _messageController?.ShowMessage("この衣装はまだ解放されていません", null, 2f);
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

        _messageController?.ShowMessage($"衣装を変更したわ。", GetCurrentCharacterName(), 2f);
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

}
