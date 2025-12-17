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

    // 内部変数
    private VisualElement listContainer;
    private List<VisualElement> createdItems = new List<VisualElement>();
    
    // UI Toolkitはガベージコレクション対策でコールバックをキャッシュする必要がある
    private List<EventCallback<ClickEvent>> clickCallbacks = new List<EventCallback<ClickEvent>>();

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
            // 見つからない場合は名前指定なしで class="sidebar-list" とかも探してみるなどの保険をかけても良い
            Debug.LogError($"[Sidebar] Container '{UIConstants.LIST_SIDEBAR}' not found in the provided root.");
            return;
        }

        GenerateMenu();
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
            if (labelEl != null) labelEl.text = data.displayName;

            var iconEl = itemRoot.Q<VisualElement>(UIConstants.EL_ICON_IMAGE);
            if (iconEl != null && data.icon != null)
                iconEl.style.backgroundImage = new StyleBackground(data.icon);

            // バッジ初期状態
            SetBadgeVisibility(itemRoot, data.showBadgeOnStart);

            // 4. クリックイベント登録
            EventCallback<ClickEvent> callback = evt => SelectItem(index);
            itemRoot.RegisterCallback(callback);
            clickCallbacks.Add(callback);

            // 5. リストに追加
            // itemRootではなくinstance(TemplateContainer)を追加するのが一般的だが
            // ここではレイアウト崩れを防ぐため itemRoot が TemplateContainer の子なら instance を追加
            listContainer.Add(instance); 
            
            // 操作用に保存するのは itemRoot (クラス操作用)
            createdItems.Add(itemRoot);
        }

        // 初期選択
        if (createdItems.Count > 0)
        {
            SelectItem(0);
        }
    }

    public void SelectItem(int index)
    {
        if (index < 0 || index >= createdItems.Count) return;

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

    private void CleanupCallbacks()
    {
        for (int i = 0; i < createdItems.Count && i < clickCallbacks.Count; i++)
        {
            createdItems[i]?.UnregisterCallback(clickCallbacks[i]);
        }
        clickCallbacks.Clear();
    }
}