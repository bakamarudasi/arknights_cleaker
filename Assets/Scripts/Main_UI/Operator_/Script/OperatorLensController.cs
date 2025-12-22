using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// オペレーター画面のレンズアイテム管理を担当するコントローラー
/// 単一責任: レンズアイテムの選択・使用・バッテリー管理
/// </summary>
public class OperatorLensController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement lensItemsContainer;
    private Label lensBatteryLabel;
    private VisualElement lensBatteryBar;
    private Button btnLensNormal;
    private Button btnLensClothes;
    private Button btnLensCircle;
    private Button btnLensRect;
    private Slider lensSizeSlider;

    // ========================================
    // 状態
    // ========================================

    private ItemData currentLensItem;
    private float currentBatteryTime;
    private float maxBatteryTime;
    private bool isLensActive;
    private int currentLensMode = 0;
    private LensMaskController.LensShape currentLensShape = LensMaskController.LensShape.Circle;
    private float currentLensSize = 2f;

    // ========================================
    // キャッシュ
    // ========================================

    private static ItemData[] _cachedAllItems;

    // ========================================
    // イベント
    // ========================================

    /// <summary>レンズモードが変更された時に発火</summary>
    public event Action<int> OnLensModeChanged;

    /// <summary>レンズ形状が変更された時に発火</summary>
    public event Action<LensMaskController.LensShape> OnLensShapeChanged;

    /// <summary>レンズサイズが変更された時に発火</summary>
    public event Action<float> OnLensSizeChanged;

    // ========================================
    // プロパティ
    // ========================================

    public bool IsLensActive => isLensActive;
    public int CurrentLensMode => currentLensMode;
    public ItemData CurrentLensItem => currentLensItem;
    public LensMaskController.LensShape CurrentLensShape => currentLensShape;
    public float CurrentLensSize => currentLensSize;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// コントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement root)
    {
        QueryElements(root);
        BindButtons();
        SetupLensItemsUI();
    }

    private void QueryElements(VisualElement root)
    {
        lensItemsContainer = root.Q<VisualElement>("lens-items-container");
        lensBatteryLabel = root.Q<Label>("lens-battery-label");
        lensBatteryBar = root.Q<VisualElement>("lens-battery-fill");
        btnLensNormal = root.Q<Button>("btn-lens-normal");
        btnLensClothes = root.Q<Button>("btn-lens-clothes");
        btnLensCircle = root.Q<Button>("btn-lens-circle");
        btnLensRect = root.Q<Button>("btn-lens-rect");
        lensSizeSlider = root.Q<Slider>("lens-size-slider");
    }

    private void BindButtons()
    {
        btnLensNormal?.RegisterCallback<ClickEvent>(evt => SetLensMode(0));
        btnLensClothes?.RegisterCallback<ClickEvent>(evt => SetLensMode(1));
        btnLensCircle?.RegisterCallback<ClickEvent>(evt => SetLensShape(LensMaskController.LensShape.Circle));
        btnLensRect?.RegisterCallback<ClickEvent>(evt => SetLensShape(LensMaskController.LensShape.Rectangle));
        lensSizeSlider?.RegisterValueChangedCallback(evt => SetLensSize(evt.newValue));
    }

    // ========================================
    // レンズアイテムUI
    // ========================================

    /// <summary>
    /// レンズアイテムUIをセットアップ
    /// </summary>
    public void SetupLensItemsUI()
    {
        if (lensItemsContainer == null) return;

        lensItemsContainer.Clear();

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

    private static ItemData[] GetAllItems()
    {
        if (_cachedAllItems == null)
        {
            _cachedAllItems = Resources.LoadAll<ItemData>("Data/Items");
        }
        return _cachedAllItems;
    }

    private List<ItemData> GetOwnedLensItems()
    {
        var result = new List<ItemData>();
        var inventory = InventoryManager.Instance;
        if (inventory == null) return result;

        var allItems = GetAllItems();
        foreach (var item in allItems)
        {
            if (item.lensSpecs.isLens && inventory.Has(item.id))
            {
                result.Add(item);
            }
        }
        return result;
    }

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

    // ========================================
    // レンズ選択・使用
    // ========================================

    private void SelectLensItem(ItemData item)
    {
        currentLensItem = item;
        maxBatteryTime = item.lensSpecs.maxDuration;
        currentBatteryTime = maxBatteryTime;

        UpdateBatteryUI();
        SetupLensItemsUI();

        LogUIController.Msg($"レンズ装備: {item.displayName}");

        // 効果音
        if (item.useSound != null)
        {
            AudioSource.PlayClipAtPoint(item.useSound, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// レンズを使用
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

        SetLensMode(currentLensItem.lensSpecs.penetrateLevel);
    }

    /// <summary>
    /// レンズモード切り替え
    /// </summary>
    public void SetLensMode(int mode)
    {
        if (mode > 0 && currentLensItem == null)
        {
            LogUIController.Msg("レンズアイテムを選択してください");
            return;
        }

        if (mode > 0 && currentBatteryTime <= 0 && maxBatteryTime > 0)
        {
            LogUIController.Msg("バッテリーが切れています");
            return;
        }

        currentLensMode = mode;
        isLensActive = mode > 0;
        UpdateLensButtons();

        string modeName = mode == 0 ? "Normal" : $"透視Lv.{mode}";
        LogUIController.Msg($"Lens mode: {modeName}");

        OnLensModeChanged?.Invoke(mode);
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

    // ========================================
    // バッテリー管理
    // ========================================

    /// <summary>
    /// バッテリーを消費
    /// </summary>
    public void UpdateBattery(float deltaTime)
    {
        if (!isLensActive || maxBatteryTime <= 0) return;

        currentBatteryTime -= deltaTime;
        if (currentBatteryTime <= 0)
        {
            currentBatteryTime = 0;
            SetLensMode(0);
            LogUIController.Msg("バッテリー切れ！");
        }

        UpdateBatteryUI();
    }

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
    // レンズ形状・サイズ
    // ========================================

    /// <summary>
    /// レンズ形状を設定
    /// </summary>
    public void SetLensShape(LensMaskController.LensShape shape)
    {
        if (currentLensShape == shape) return;

        currentLensShape = shape;
        UpdateShapeButtons();

        string shapeName = shape == LensMaskController.LensShape.Circle ? "丸型" : "四角型";
        LogUIController.Msg($"レンズ形状: {shapeName}");

        OnLensShapeChanged?.Invoke(shape);
    }

    private void UpdateShapeButtons()
    {
        btnLensCircle?.RemoveFromClassList("active");
        btnLensRect?.RemoveFromClassList("active");

        switch (currentLensShape)
        {
            case LensMaskController.LensShape.Circle:
                btnLensCircle?.AddToClassList("active");
                break;
            case LensMaskController.LensShape.Rectangle:
                btnLensRect?.AddToClassList("active");
                break;
        }
    }

    /// <summary>
    /// レンズサイズを設定
    /// </summary>
    public void SetLensSize(float size)
    {
        currentLensSize = size;
        OnLensSizeChanged?.Invoke(size);
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        OnLensModeChanged = null;
        OnLensShapeChanged = null;
        OnLensSizeChanged = null;
    }
}
