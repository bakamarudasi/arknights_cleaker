using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class SidebarController : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private VisualTreeAsset itemPrefab;

    [Header("Data")]
    [SerializeField] private List<MenuItemData> menuItems = new List<MenuItemData>();

    // イベント
    public event Action<int> OnMenuChanged;

    /// <summary>ロック中のメニューをクリックした時（ヒントテキストを渡す）</summary>
    public event Action<string> OnLockedMenuClicked;

    // 内部変数
    private VisualElement listContainer;
    private List<VisualElement> createdItems = new List<VisualElement>();

    // ロック状態管理（MenuType → ロック状態）
    private Dictionary<MenuType, bool> lockedMenus = new Dictionary<MenuType, bool>();

    // UI Toolkitはガベージコレクション対策でコールバックをキャッシュする必要がある
    private List<EventCallback<ClickEvent>> clickCallbacks = new List<EventCallback<ClickEvent>>();

    // EventManager購読用
    private Action<MenuType> _onMenuUnlockedCallback;

    /// <summary>
    /// 初期化処理 (MainUIControllerから呼ばれる想定)
    /// </summary>
    /// <param name="rootElement">サイドバーが含まれる親要素</param>
    public void Initialize(VisualElement rootElement)
    {
        if (rootElement == null)
        {
            Debug.LogError("[Sidebar] Root element provided is null.");
            return;
        }

        // 定数を使ってリストコンテナを探す
        listContainer = rootElement.Q<VisualElement>(UIConstants.LIST_SIDEBAR);

        if (listContainer == null)
        {
            Debug.LogError($"[Sidebar] Container '{UIConstants.LIST_SIDEBAR}' not found in the provided root.");
            return;
        }

        // ロック状態を初期化
        InitializeLockStates();

        // EventManagerのイベントを購読
        SubscribeToEventManager();

        GenerateMenu();
    }

    /// <summary>
    /// ロック状態を初期化（MenuItemDataのisLockedByDefaultから）
    /// </summary>
    private void InitializeLockStates()
    {
        lockedMenus.Clear();
        foreach (var item in menuItems)
        {
            lockedMenus[item.menuType] = item.isLockedByDefault;
        }
    }

    /// <summary>
    /// EventManagerのイベントを購読
    /// </summary>
    private void SubscribeToEventManager()
    {
        // EventManagerが存在する場合のみ購読
        if (EventManager.Instance != null)
        {
            _onMenuUnlockedCallback = OnMenuUnlockedFromEvent;
            EventManager.Instance.OnMenuUnlocked += _onMenuUnlockedCallback;
        }
        else
        {
            // 遅延して再試行
            Invoke(nameof(SubscribeToEventManager), 0.5f);
        }
    }

    private void OnMenuUnlockedFromEvent(MenuType menuType)
    {
        UnlockMenu(menuType);
    }

    private void GenerateMenu()
    {
        // クリーンアップ
        CleanupCallbacks();
        listContainer.Clear();
        createdItems.Clear();

        if (itemPrefab == null)
        {
            Debug.LogError("[Sidebar] Item Prefab is null.");
            return;
        }

        for (int i = 0; i < menuItems.Count; i++)
        {
            var data = menuItems[i];
            int index = i; // クロージャ用キャプチャ

            // 1. テンプレートの実体化
            VisualElement instance = itemPrefab.Instantiate();

            // 2. ルート要素を取得 (CSSクラスなどで特定)
            var itemRoot = instance.Q<VisualElement>(className: UIConstants.CLS_SIDEBAR_ITEM);
            if (itemRoot == null)
            {
                // Instantiate直下の要素がそれかもしれないのでチェック
                if (instance.ClassListContains(UIConstants.CLS_SIDEBAR_ITEM))
                    itemRoot = instance;
                else
                    // テンプレート構造が違う場合の保険: instance自体を使う
                    itemRoot = instance;
            }

            // 3. 各パーツへのデータ流し込み (定数を使用)
            var labelEl = itemRoot.Q<Label>(UIConstants.EL_ITEM_LABEL);
            var iconEl = itemRoot.Q<VisualElement>(UIConstants.EL_ICON_IMAGE);

            // ロック状態をチェック
            bool isLocked = IsMenuLocked(data.menuType);

            if (isLocked)
            {
                // ロック中の表示
                if (labelEl != null) labelEl.text = "???";
                if (iconEl != null)
                {
                    if (data.lockedIcon != null)
                        iconEl.style.backgroundImage = new StyleBackground(data.lockedIcon);
                    else if (data.icon != null)
                        iconEl.style.backgroundImage = new StyleBackground(data.icon);
                }
                itemRoot.AddToClassList(UIConstants.CLS_LOCKED);
            }
            else
            {
                // 通常表示
                if (labelEl != null) labelEl.text = data.displayName;
                if (iconEl != null && data.icon != null)
                    iconEl.style.backgroundImage = new StyleBackground(data.icon);
            }

            // バッジ初期状態
            SetBadgeVisibility(itemRoot, data.showBadgeOnStart);

            // 4. クリックイベント登録
            EventCallback<ClickEvent> callback = evt => OnItemClicked(index);
            itemRoot.RegisterCallback(callback);
            clickCallbacks.Add(callback);

            // 5. リストに追加
            listContainer.Add(instance);

            // 操作用に保存するのは itemRoot (クラス操作用)
            createdItems.Add(itemRoot);
        }

        // 初期選択（ロックされていない最初のメニューを選択）
        SelectFirstUnlockedItem();
    }

    /// <summary>
    /// アイテムがクリックされた時の処理
    /// </summary>
    private void OnItemClicked(int index)
    {
        if (index < 0 || index >= menuItems.Count) return;

        var data = menuItems[index];

        // ロック中ならヒントを表示して終了
        if (IsMenuLocked(data.menuType))
        {
            OnLockedMenuClicked?.Invoke(data.lockedHintText);
            Debug.Log($"[Sidebar] Locked menu clicked: {data.menuType} - {data.lockedHintText}");
            return;
        }

        SelectItem(index);
    }

    public void SelectItem(int index)
    {
        if (index < 0 || index >= createdItems.Count) return;

        // ロック中なら選択不可
        if (IsMenuLocked(menuItems[index].menuType)) return;

        // 全解除
        foreach (var item in createdItems)
        {
            item.RemoveFromClassList(UIConstants.CLS_SELECTED);
        }

        // 選択付与
        createdItems[index].AddToClassList(UIConstants.CLS_SELECTED);

        // イベント発火
        OnMenuChanged?.Invoke(index);
    }

    /// <summary>
    /// ロックされていない最初のメニューを選択
    /// </summary>
    private void SelectFirstUnlockedItem()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (!IsMenuLocked(menuItems[i].menuType))
            {
                SelectItem(i);
                return;
            }
        }
    }

    // ========================================
    // ロック/アンロック管理
    // ========================================

    /// <summary>
    /// メニューがロックされているかどうか
    /// </summary>
    public bool IsMenuLocked(MenuType menuType)
    {
        return lockedMenus.TryGetValue(menuType, out bool isLocked) && isLocked;
    }

    /// <summary>
    /// メニューをアンロックする
    /// </summary>
    public void UnlockMenu(MenuType menuType)
    {
        if (!lockedMenus.ContainsKey(menuType)) return;
        if (!lockedMenus[menuType]) return; // 既にアンロック済み

        lockedMenus[menuType] = false;

        // UIを更新
        int index = menuItems.FindIndex(m => m.menuType == menuType);
        if (index >= 0 && index < createdItems.Count)
        {
            var data = menuItems[index];
            var itemRoot = createdItems[index];

            // ラベルとアイコンを更新
            var labelEl = itemRoot.Q<Label>(UIConstants.EL_ITEM_LABEL);
            if (labelEl != null) labelEl.text = data.displayName;

            var iconEl = itemRoot.Q<VisualElement>(UIConstants.EL_ICON_IMAGE);
            if (iconEl != null && data.icon != null)
                iconEl.style.backgroundImage = new StyleBackground(data.icon);

            // ロッククラスを削除
            itemRoot.RemoveFromClassList(UIConstants.CLS_LOCKED);

            // アンロック演出（NEWバッジを表示）
            SetBadgeVisibility(itemRoot, true);
        }

        Debug.Log($"[Sidebar] Menu unlocked: {menuType}");
    }

    /// <summary>
    /// セーブデータからロック状態を復元
    /// </summary>
    public void RestoreUnlockedMenus(List<string> unlockedMenuNames)
    {
        if (unlockedMenuNames == null) return;

        foreach (var menuName in unlockedMenuNames)
        {
            if (Enum.TryParse<MenuType>(menuName, out var menuType))
            {
                lockedMenus[menuType] = false;
            }
        }

        // UIを再生成
        if (listContainer != null)
        {
            GenerateMenu();
        }
    }

    /// <summary>
    /// 現在のアンロック状態を取得（セーブ用）
    /// </summary>
    public List<string> GetUnlockedMenus()
    {
        var result = new List<string>();
        foreach (var kvp in lockedMenus)
        {
            if (!kvp.Value) // ロックされていない = アンロック済み
            {
                result.Add(kvp.Key.ToString());
            }
        }
        return result;
    }

    // ========================================
    // バッジ管理
    // ========================================

    public void SetBadge(int index, bool isVisible)
    {
        if (index >= 0 && index < createdItems.Count)
        {
            SetBadgeVisibility(createdItems[index], isVisible);
        }
    }

    private void SetBadgeVisibility(VisualElement itemRoot, bool isVisible)
    {
        var badge = itemRoot.Q<VisualElement>(UIConstants.EL_BADGE);
        if (badge != null)
        {
            if (isVisible) badge.AddToClassList(UIConstants.CLS_SHOW);
            else badge.RemoveFromClassList(UIConstants.CLS_SHOW);
        }
    }

    // ========================================
    // クリーンアップ
    // ========================================

    private void CleanupCallbacks()
    {
        for (int i = 0; i < createdItems.Count && i < clickCallbacks.Count; i++)
        {
            createdItems[i]?.UnregisterCallback(clickCallbacks[i]);
        }
        clickCallbacks.Clear();
    }

    private void OnDestroy()
    {
        CancelInvoke();

        if (EventManager.Instance != null && _onMenuUnlockedCallback != null)
        {
            EventManager.Instance.OnMenuUnlocked -= _onMenuUnlockedCallback;
        }
    }
}
