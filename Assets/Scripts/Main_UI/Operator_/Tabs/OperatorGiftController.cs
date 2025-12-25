using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// オペレーター画面のプレゼント機能を担当するコントローラー
/// 単一責任: プレゼントアイテムの表示と贈呈
/// </summary>
public class OperatorGiftController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement giftContainer;
    private List<VisualElement> giftItemElements = new();

    // ========================================
    // キャッシュ
    // ========================================

    private static ItemData[] _cachedAllItems;

    // ========================================
    // イベント
    // ========================================

    /// <summary>プレゼントが渡された時に発火</summary>
    public event Action<ItemData> OnGiftGiven;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// コントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement root)
    {
        giftContainer = root.Q<VisualElement>("gift-container");
        SetupGiftItemsUI();
    }

    // ========================================
    // プレゼントUI
    // ========================================

    /// <summary>
    /// プレゼントアイテムUIをセットアップ
    /// </summary>
    public void SetupGiftItemsUI()
    {
        if (giftContainer == null) return;

        giftContainer.Clear();
        giftItemElements.Clear();

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

    private static ItemData[] GetAllItems()
    {
        if (_cachedAllItems == null)
        {
            var itemList = new List<ItemData>();

            // GachaDatabaseから全バナーの全アイテムを取得
            if (GachaManager.Instance?.Database != null)
            {
                var banners = GachaManager.Instance.Database.GetAllBanners();
                foreach (var banner in banners)
                {
                    if (banner.pool == null) continue;
                    foreach (var entry in banner.pool)
                    {
                        if (entry.item != null && !itemList.Contains(entry.item))
                        {
                            itemList.Add(entry.item);
                        }
                    }
                }
            }

            _cachedAllItems = itemList.ToArray();
        }
        return _cachedAllItems;
    }

    private List<ItemData> GetOwnedGiftItems()
    {
        var result = new List<ItemData>();
        var inventory = InventoryManager.Instance;
        if (inventory == null) return result;

        var allItems = GetAllItems();
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

    // ========================================
    // プレゼント贈呈
    // ========================================

    private void GiveGift(ItemData item)
    {
        if (AffectionManager.Instance == null) return;

        AffectionManager.Instance.GiveGift(item.id);
        SetupGiftItemsUI();
        OnGiftGiven?.Invoke(item);
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        OnGiftGiven = null;
        giftItemElements.Clear();
    }
}
