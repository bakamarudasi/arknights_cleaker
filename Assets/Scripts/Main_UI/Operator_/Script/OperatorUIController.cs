using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// オペレーター画面のUIファサード (MonoBehaviour)
/// 単一責任: 各サブコントローラーの統合と画面全体のライフサイクル管理
///
/// 使い方:
/// 1. 空のGameObjectにこのスクリプトをアタッチ
/// 2. Inspectorで UXML/USS、シーンUIプレハブを設定
/// 3. プレハブ化
/// 4. MainUIControllerから生成
/// </summary>
public class OperatorUIController : MonoBehaviour, IViewController
{
    // ========================================
    // Inspector設定
    // ========================================

    [Header("=== UI テンプレート ===")]
    [Tooltip("OperatorView.uxml")]
    [SerializeField] private VisualTreeAsset viewTemplate;

    [Tooltip("OperatorStyles.uss")]
    [SerializeField] private StyleSheet styleSheet;

    [Header("=== シーンUI プレハブ ===")]
    [Tooltip("デフォルトシーンUI（サイドパネル表示）")]
    [SerializeField] private BaseSceneUI defaultSceneUIPrefab;

    [Tooltip("フルスクリーンシーンUI（サイドパネル非表示）")]
    [SerializeField] private BaseSceneUI fullscreenSceneUIPrefab;

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
    // シーンUI（ポーズ/シーンに応じたUI切り替え）
    // ========================================

    private ISceneUI _currentSceneUI;
    private BaseSceneUI _defaultSceneUI;
    private BaseSceneUI _fullscreenSceneUI;

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

    /// <summary>
    /// 外部からの初期化（MainUIControllerから呼ばれる）
    /// </summary>
    public void Initialize(VisualElement contentArea)
    {
        // UXMLをContentAreaに追加
        if (viewTemplate != null)
        {
            viewTemplate.CloneTree(contentArea);

            if (styleSheet != null)
            {
                contentArea.styleSheets.Add(styleSheet);
            }
        }

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

    private void InitializeSceneUIs()
    {
        // デフォルトシーンUI（プレハブから生成）
        if (defaultSceneUIPrefab != null)
        {
            _defaultSceneUI = Instantiate(defaultSceneUIPrefab, transform);
            _defaultSceneUI.Initialize(root);
        }
        else
        {
            Debug.LogWarning("[OperatorUI] defaultSceneUIPrefab is not assigned!");
        }

        // フルスクリーンシーンUI（プレハブから生成）
        if (fullscreenSceneUIPrefab != null)
        {
            _fullscreenSceneUI = Instantiate(fullscreenSceneUIPrefab, transform);
            _fullscreenSceneUI.Initialize(root);
        }
        else
        {
            Debug.LogWarning("[OperatorUI] fullscreenSceneUIPrefab is not assigned!");
        }

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
    // シーンUI切り替え
    // ========================================

    private void OnPoseChanged(string poseId)
    {
        ApplyCurrentPoseUI();
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
            if (_fullscreenSceneUI is FullscreenSceneUI fullscreen)
            {
                fullscreen.SetHideBackButton(poseEntry.hideBackButton);
            }
            _currentSceneUI = _fullscreenSceneUI;
        }
        else
        {
            _currentSceneUI = _defaultSceneUI;
        }

        // 新しいシーンUIを表示
        _currentSceneUI?.Show();

        Debug.Log($"[OperatorUI] Scene UI switched: {_currentSceneUI?.GetType().Name}");
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

        // シーンUIの解放
        _currentSceneUI?.Hide();
        if (_defaultSceneUI != null)
        {
            _defaultSceneUI.Dispose();
            Destroy(_defaultSceneUI.gameObject);
        }
        if (_fullscreenSceneUI != null)
        {
            _fullscreenSceneUI.Dispose();
            Destroy(_fullscreenSceneUI.gameObject);
        }
        _currentSceneUI = null;
        _defaultSceneUI = null;
        _fullscreenSceneUI = null;

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

    private void OnDestroy()
    {
        Dispose();
    }
}
