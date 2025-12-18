using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// オペレーター画面のUIコントローラー
/// UI Toolkit（ボタン）とGameObject（キャラ表示）を連携
/// レンズアイテム使用、プレゼント、好感度UI、頭なでなどのふれあい機能
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

    // レンズアイテム関連
    private VisualElement lensItemsContainer;
    private Label lensBatteryLabel;
    private VisualElement lensBatteryBar;
    private ItemData currentLensItem;
    private float currentBatteryTime;
    private float maxBatteryTime;
    private bool isLensActive;

    // 好感度UI
    private Label affectionLevelLabel;
    private VisualElement affectionBarFill;
    private Label affectionValueLabel;

    // プレゼントUI
    private VisualElement giftContainer;
    private List<VisualElement> giftItemElements = new();

    // インタラクションエリア（UIベース、プレハブ側のCharacterInteractionZoneと併用）
    private VisualElement characterDisplay;

    // コールバック参照（解除用に保持）
    private EventCallback<ClickEvent> callbackOutfit0;
    private EventCallback<ClickEvent> callbackOutfit1;
    private EventCallback<ClickEvent> callbackOutfit2;
    private EventCallback<ClickEvent> callbackLensNormal;
    private EventCallback<ClickEvent> callbackLensClothes;
    private EventCallback<ClickEvent> callbackBack;
    private EventCallback<ClickEvent> callbackCharacterClick;

    // 現在の状態
    private int currentOutfit = 0;
    private int currentLensMode = 0; // 0: Normal, 1+: 透視レベル
    // 頭なで等のインタラクションはCharacterInteractionZone（プレハブ）で処理

    // イベント
    public event Action OnBackRequested;

    public void Initialize(VisualElement contentArea)
    {
        root = contentArea;

        SetupReferences();
        SetupCallbacks();
        SetupLensItemsUI();
        SetupGiftItemsUI();
        UpdateDisplay();
        UpdateAffectionUI();

        // PSBキャラ表示
        ShowCharacterOverlay();

        // イベント購読
        SubscribeToEvents();

        LogUIController.LogSystem("Operator View Initialized.");
    }

    /// <summary>
    /// イベント購読
    /// </summary>
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

    /// <summary>
    /// イベント購読解除
    /// </summary>
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
        UpdateAffectionUI();
    }

    private void OnItemCountChanged(string itemId, int newCount)
    {
        // レンズアイテムまたはプレゼントアイテムの変更時にUIを更新
        SetupLensItemsUI();
        SetupGiftItemsUI();
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
            // 表示エリアをcontent-area（root）に設定
            presenter.SetDisplayArea(root);
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

        // レンズアイテム関連
        lensItemsContainer = root.Q<VisualElement>("lens-items-container");
        lensBatteryLabel = root.Q<Label>("lens-battery-label");
        lensBatteryBar = root.Q<VisualElement>("lens-battery-fill");

        // 好感度UI
        affectionLevelLabel = root.Q<Label>("affection-level");
        affectionBarFill = root.Q<VisualElement>("affection-bar-fill");
        affectionValueLabel = root.Q<Label>("affection-value");

        // プレゼントUI
        giftContainer = root.Q<VisualElement>("gift-container");

        // キャラクター表示エリア（全体クリック用）
        // 詳細なインタラクション（頭なで等）はCharacterInteractionZone（プレハブ）で処理
        characterDisplay = root.Q<VisualElement>("character-display");
    }

    private void SetupCallbacks()
    {
        // コールバックをフィールドに保存（解除時に同じ参照を使うため）
        callbackOutfit0 = evt => SetOutfit(0);
        callbackOutfit1 = evt => SetOutfit(1);
        callbackOutfit2 = evt => SetOutfit(2);
        callbackLensNormal = evt => SetLensMode(0);
        callbackLensClothes = evt => SetLensMode(1);
        callbackBack = evt => OnBackRequested?.Invoke();
        callbackCharacterClick = evt => OnCharacterClicked();

        // 着せ替えボタン
        btnOutfitDefault?.RegisterCallback(callbackOutfit0);
        btnOutfitSkin1?.RegisterCallback(callbackOutfit1);
        btnOutfitSkin2?.RegisterCallback(callbackOutfit2);

        // レンズボタン
        btnLensNormal?.RegisterCallback(callbackLensNormal);
        btnLensClothes?.RegisterCallback(callbackLensClothes);

        // 戻るボタン
        btnBack?.RegisterCallback(callbackBack);

        // キャラクター表示エリア（全体クリック）
        // 詳細なインタラクションはプレハブ側で処理
        characterDisplay?.RegisterCallback(callbackCharacterClick);
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
    /// レンズモード切り替え（透視レベル）
    /// </summary>
    private void SetLensMode(int mode)
    {
        // レンズアイテムが必要な場合のチェック
        if (mode > 0 && currentLensItem == null)
        {
            LogUIController.Msg("レンズアイテムを選択してください");
            return;
        }

        // バッテリーチェック（mode > 0の場合）
        if (mode > 0 && currentBatteryTime <= 0 && maxBatteryTime > 0)
        {
            LogUIController.Msg("バッテリーが切れています");
            return;
        }

        currentLensMode = mode;
        isLensActive = mode > 0;
        UpdateLensButtons();
        ApplyLensEffect();

        string modeName = mode == 0 ? "Normal" : $"透視Lv.{mode}";
        LogUIController.Msg($"Lens mode: {modeName}");
    }

    /// <summary>
    /// レンズ効果を適用
    /// </summary>
    private void ApplyLensEffect()
    {
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        // レンズモードに応じてキャラクター表示を切り替え
        // TODO: 実際の透視レイヤー切り替え実装
        // presenter.SetPenetrateLevel(currentLensMode);

        if (currentLensItem != null && currentLensItem.lensSpecs.isLens)
        {
            // フィルターエフェクト適用
            // presenter.SetFilterMode(currentLensItem.lensSpecs.filterMode);
            Debug.Log($"[Lens] Filter: {currentLensItem.lensSpecs.filterMode}, Level: {currentLensMode}");
        }
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

    // ========================================
    // レンズアイテム関連
    // ========================================

    /// <summary>
    /// レンズアイテムUIをセットアップ
    /// </summary>
    private void SetupLensItemsUI()
    {
        if (lensItemsContainer == null) return;

        lensItemsContainer.Clear();

        // インベントリからレンズアイテムを取得
        var lensItems = GetOwnedLensItems();

        if (lensItems.Count == 0)
        {
            var emptyLabel = new Label("レンズアイテムがありません");
            emptyLabel.AddToClassList("lens-empty-text");
            lensItemsContainer.Add(emptyLabel);
            return;
        }

        foreach (var item in lensItems)
        {
            var itemElement = CreateLensItemElement(item);
            lensItemsContainer.Add(itemElement);
        }
    }

    /// <summary>
    /// 所持しているレンズアイテムを取得
    /// </summary>
    private List<ItemData> GetOwnedLensItems()
    {
        var result = new List<ItemData>();
        var inventory = InventoryManager.Instance;
        if (inventory == null) return result;

        // 全アイテムデータベースからレンズアイテムを検索
        var allItems = Resources.LoadAll<ItemData>("Data/Items");
        foreach (var item in allItems)
        {
            if (item.lensSpecs.isLens && inventory.Has(item.id))
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// レンズアイテム要素を作成
    /// </summary>
    private VisualElement CreateLensItemElement(ItemData item)
    {
        var element = new VisualElement();
        element.AddToClassList("lens-item");

        // アイコン
        var icon = new VisualElement();
        icon.AddToClassList("lens-item-icon");
        if (item.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(item.icon);
        }
        element.Add(icon);

        // 名前
        var nameLabel = new Label(item.displayName);
        nameLabel.AddToClassList("lens-item-name");
        element.Add(nameLabel);

        // スペック情報
        var specLabel = new Label($"Lv.{item.lensSpecs.penetrateLevel} / {item.lensSpecs.maxDuration}s");
        specLabel.AddToClassList("lens-item-spec");
        element.Add(specLabel);

        // クリックで選択
        element.RegisterCallback<ClickEvent>(evt =>
        {
            SelectLensItem(item);
            evt.StopPropagation();
        });

        // 現在選択中なら強調
        if (currentLensItem == item)
        {
            element.AddToClassList("selected");
        }

        return element;
    }

    /// <summary>
    /// レンズアイテムを選択
    /// </summary>
    private void SelectLensItem(ItemData item)
    {
        currentLensItem = item;
        maxBatteryTime = item.lensSpecs.maxDuration;
        currentBatteryTime = maxBatteryTime;

        UpdateBatteryUI();
        SetupLensItemsUI(); // 選択状態を更新

        LogUIController.Msg($"レンズ装備: {item.displayName}");

        // 効果音
        if (item.useSound != null)
        {
            AudioSource.PlayClipAtPoint(item.useSound, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// レンズを使用（バッテリー消費開始）
    /// </summary>
    public void UseLens()
    {
        if (currentLensItem == null)
        {
            LogUIController.Msg("レンズを装備してください");
            return;
        }

        if (currentBatteryTime <= 0 && maxBatteryTime > 0)
        {
            LogUIController.Msg("バッテリー切れです");
            return;
        }

        // 透視モードを有効化（レンズの透視レベルに応じて）
        SetLensMode(currentLensItem.lensSpecs.penetrateLevel);
    }

    /// <summary>
    /// バッテリーを消費（毎フレーム呼び出し用）
    /// </summary>
    public void UpdateBattery(float deltaTime)
    {
        if (!isLensActive || maxBatteryTime <= 0) return;

        currentBatteryTime -= deltaTime;
        if (currentBatteryTime <= 0)
        {
            currentBatteryTime = 0;
            SetLensMode(0); // 通常モードに戻す
            LogUIController.Msg("バッテリー切れ！");
        }

        UpdateBatteryUI();
    }

    /// <summary>
    /// バッテリーUIを更新
    /// </summary>
    private void UpdateBatteryUI()
    {
        if (lensBatteryLabel != null)
        {
            lensBatteryLabel.text = $"BATTERY: {currentBatteryTime:F1}s";
        }

        if (lensBatteryBar != null && maxBatteryTime > 0)
        {
            float percent = (currentBatteryTime / maxBatteryTime) * 100f;
            lensBatteryBar.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));

            // 残量に応じて色を変更
            if (percent > 50)
                lensBatteryBar.style.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            else if (percent > 20)
                lensBatteryBar.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            else
                lensBatteryBar.style.backgroundColor = new Color(1f, 0.3f, 0.3f);
        }
    }

    // ========================================
    // プレゼント関連
    // ========================================

    /// <summary>
    /// プレゼントアイテムUIをセットアップ
    /// </summary>
    private void SetupGiftItemsUI()
    {
        if (giftContainer == null) return;

        giftContainer.Clear();
        giftItemElements.Clear();

        // インベントリからプレゼント可能アイテムを取得
        var giftItems = GetOwnedGiftItems();

        if (giftItems.Count == 0)
        {
            var emptyElement = new VisualElement();
            emptyElement.AddToClassList("gift-empty");
            var emptyLabel = new Label("プレゼントできるアイテムがありません");
            emptyLabel.AddToClassList("gift-empty-text");
            emptyElement.Add(emptyLabel);
            giftContainer.Add(emptyElement);
            return;
        }

        foreach (var item in giftItems)
        {
            var itemElement = CreateGiftItemElement(item);
            giftContainer.Add(itemElement);
            giftItemElements.Add(itemElement);
        }
    }

    /// <summary>
    /// 所持しているプレゼントアイテムを取得
    /// </summary>
    private List<ItemData> GetOwnedGiftItems()
    {
        var result = new List<ItemData>();
        var inventory = InventoryManager.Instance;
        if (inventory == null) return result;

        // 全アイテムデータベースからプレゼント可能アイテムを検索
        var allItems = Resources.LoadAll<ItemData>("Data/Items");
        foreach (var item in allItems)
        {
            // 消耗品タイプまたは素材で、レンズでないもの
            if (!item.lensSpecs.isLens &&
                (item.type == ItemData.ItemType.Consumable || item.type == ItemData.ItemType.Material) &&
                inventory.Has(item.id))
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// プレゼントアイテム要素を作成
    /// </summary>
    private VisualElement CreateGiftItemElement(ItemData item)
    {
        var element = new VisualElement();
        element.AddToClassList("gift-item");

        // アイコン
        var icon = new VisualElement();
        icon.AddToClassList("gift-item-icon");
        if (item.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(item.icon);
        }
        element.Add(icon);

        // 所持数
        var inventory = InventoryManager.Instance;
        int count = inventory?.GetCount(item.id) ?? 0;
        var countLabel = new Label($"x{count}");
        countLabel.AddToClassList("gift-item-count");
        element.Add(countLabel);

        // レアリティカラー
        element.style.borderBottomColor = item.GetRarityColor();
        element.style.borderBottomWidth = 2;

        // クリックでプレゼント
        element.RegisterCallback<ClickEvent>(evt =>
        {
            GiveGift(item);
            evt.StopPropagation();
        });

        return element;
    }

    /// <summary>
    /// プレゼントを渡す
    /// </summary>
    private void GiveGift(ItemData item)
    {
        if (AffectionManager.Instance == null) return;

        AffectionManager.Instance.GiveGift(item.id);
        SetupGiftItemsUI(); // UI更新
    }

    // ========================================
    // 好感度UI
    // ========================================

    /// <summary>
    /// 好感度UIを更新
    /// </summary>
    private void UpdateAffectionUI()
    {
        var affectionManager = AffectionManager.Instance;
        if (affectionManager == null) return;

        int currentAffection = affectionManager.GetCurrentAffection();
        var currentLevel = affectionManager.GetCurrentAffectionLevel();

        // レベル表示
        if (affectionLevelLabel != null && currentLevel != null)
        {
            affectionLevelLabel.text = $"Lv.{currentLevel.level} {currentLevel.levelName}";
        }

        // バー表示
        if (affectionBarFill != null && currentLevel != null)
        {
            // 次のレベルまでの進捗を計算
            int levelStart = currentLevel.requiredAffection;
            int levelEnd = GetNextLevelRequirement(currentLevel.level);
            float progress = 0f;

            if (levelEnd > levelStart)
            {
                progress = (float)(currentAffection - levelStart) / (levelEnd - levelStart);
            }
            else
            {
                progress = 1f; // 最大レベル
            }

            affectionBarFill.style.width = new StyleLength(new Length(progress * 100f, LengthUnit.Percent));
        }

        // 数値表示
        if (affectionValueLabel != null)
        {
            int maxAffection = 200; // デフォルト値
            affectionValueLabel.text = $"{currentAffection} / {maxAffection}";
        }
    }

    /// <summary>
    /// 次のレベルに必要な好感度を取得
    /// </summary>
    private int GetNextLevelRequirement(int currentLevelIndex)
    {
        // CharacterDataから取得するのが理想だが、ここでは簡易実装
        int[] levelThresholds = { 0, 50, 100, 150, 200 };
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelThresholds.Length)
        {
            return levelThresholds[nextIndex];
        }
        return levelThresholds[levelThresholds.Length - 1];
    }

    // ========================================
    // キャラクターインタラクション
    // ========================================

    /// <summary>
    /// キャラクタークリック時（UI Toolkit側）
    /// 詳細なインタラクション（頭なで等）はCharacterInteractionZone（プレハブ）で処理
    /// </summary>
    private void OnCharacterClicked()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnCharacterClicked();
        }

        // クリック演出
        // TODO: ハートエフェクトなど
    }

    // ========================================
    // 破棄
    // ========================================

    public void Dispose()
    {
        // イベント購読解除
        UnsubscribeFromEvents();

        // Overlay非表示
        HideCharacterOverlay();

        // イベント解除（保存した参照を使用）
        if (callbackOutfit0 != null) btnOutfitDefault?.UnregisterCallback(callbackOutfit0);
        if (callbackOutfit1 != null) btnOutfitSkin1?.UnregisterCallback(callbackOutfit1);
        if (callbackOutfit2 != null) btnOutfitSkin2?.UnregisterCallback(callbackOutfit2);
        if (callbackLensNormal != null) btnLensNormal?.UnregisterCallback(callbackLensNormal);
        if (callbackLensClothes != null) btnLensClothes?.UnregisterCallback(callbackLensClothes);
        if (callbackBack != null) btnBack?.UnregisterCallback(callbackBack);
        if (callbackCharacterClick != null) characterDisplay?.UnregisterCallback(callbackCharacterClick);

        // 参照をクリア
        callbackOutfit0 = null;
        callbackOutfit1 = null;
        callbackOutfit2 = null;
        callbackLensNormal = null;
        callbackLensClothes = null;
        callbackBack = null;
        callbackCharacterClick = null;

        // UI要素のクリア
        giftItemElements.Clear();

        LogUIController.LogSystem("Operator View Disposed.");
    }
}
