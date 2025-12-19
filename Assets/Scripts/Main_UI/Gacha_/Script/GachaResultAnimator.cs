using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ガチャ結果表示のアニメーションを担当するクラス
/// </summary>
public class GachaResultAnimator
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private VisualElement resultContainer;

    // ========================================
    // 状態
    // ========================================

    private IVisualElementScheduledItem animTimer;
    private List<GachaResultItem> pendingResults;
    private int animIndex;

    // ========================================
    // イベント
    // ========================================

    /// <summary>アニメーション完了時に発火</summary>
    public event Action OnAnimationCompleted;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// アニメーターを初期化
    /// </summary>
    public void Initialize(VisualElement rootElement, VisualElement container)
    {
        root = rootElement;
        resultContainer = container;
    }

    // ========================================
    // アニメーション制御
    // ========================================

    /// <summary>
    /// 結果アニメーションを開始
    /// </summary>
    public void StartAnimation(List<GachaResultItem> results)
    {
        resultContainer?.Clear();

        pendingResults = results;
        animIndex = 0;

        animTimer = root.schedule.Execute(OnAnimTick).Every(GachaUIConstants.RESULT_ANIM_BASE_INTERVAL_MS);
    }

    /// <summary>
    /// アニメーションを停止
    /// </summary>
    public void Stop()
    {
        animTimer?.Pause();
        animTimer = null;
        pendingResults = null;
    }

    // ========================================
    // アニメーション処理
    // ========================================

    private void OnAnimTick()
    {
        if (pendingResults == null || animIndex >= pendingResults.Count)
        {
            animTimer?.Pause();
            animTimer = null;
            OnAnimationCompleted?.Invoke();
            return;
        }

        var item = pendingResults[animIndex];
        var itemElement = CreateResultItemElement(item);
        resultContainer?.Add(itemElement);

        // 高レアは遅延を長く
        int delay = item.rarity switch
        {
            >= GachaUIConstants.RARITY_UR_MIN => GachaUIConstants.RESULT_DELAY_RARITY_6_MS,
            GachaUIConstants.RARITY_SSR_MIN => GachaUIConstants.RESULT_DELAY_RARITY_5_MS,
            4 => GachaUIConstants.RESULT_DELAY_RARITY_4_MS,
            _ => GachaUIConstants.RESULT_DELAY_DEFAULT_MS
        };

        animTimer?.Pause();
        animTimer = null;
        animIndex++;

        if (animIndex < pendingResults.Count)
        {
            root.schedule.Execute(OnAnimTick).ExecuteLater(delay);
        }
        else
        {
            root.schedule.Execute(() =>
            {
                OnAnimationCompleted?.Invoke();
            }).ExecuteLater(delay);
        }
    }

    // ========================================
    // UI生成
    // ========================================

    private VisualElement CreateResultItemElement(GachaResultItem item)
    {
        var container = new VisualElement();
        container.AddToClassList("result-item");
        container.AddToClassList($"rarity-{item.rarity}");

        // レアリティ別のアニメーションクラス
        string animClass = item.rarity switch
        {
            >= GachaUIConstants.RARITY_UR_MIN => "result-item-spin-6",
            GachaUIConstants.RARITY_SSR_MIN => "result-item-spin-5",
            4 => "result-item-spin-4",
            _ => "result-item-appear"
        };
        container.AddToClassList(animClass);

        if (item.isNew) container.AddToClassList("is-new");
        if (item.isPickup) container.AddToClassList("is-pickup");

        // アイコン
        var icon = new VisualElement();
        icon.AddToClassList("result-item-icon");
        if (item.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(item.icon);
        }
        container.Add(icon);

        // NEWバッジ
        if (item.isNew)
        {
            var newBadge = new Label("NEW");
            newBadge.AddToClassList("new-badge");
            container.Add(newBadge);
        }

        // ピックアップバッジ
        if (item.isPickup)
        {
            var pickupBadge = new Label("PICK UP");
            pickupBadge.AddToClassList("pickup-badge");
            container.Add(pickupBadge);
        }

        // 名前
        var nameLabel = new Label(item.itemName ?? "???");
        nameLabel.AddToClassList("result-item-name");
        container.Add(nameLabel);

        // レアリティ表示（星）
        var rarityLabel = new Label(new string('★', item.rarity));
        rarityLabel.AddToClassList("result-item-rarity");
        container.Add(rarityLabel);

        // 遅延してからvisibleクラスを追加
        root.schedule.Execute(() =>
        {
            container.AddToClassList("visible");
        }).ExecuteLater(GachaUIConstants.RESULT_ITEM_VISIBLE_DELAY_MS);

        return container;
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        Stop();
        OnAnimationCompleted = null;
    }
}
