using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class MainUIController : MonoBehaviour
{
    public static MainUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private LogUIController logController;
    [SerializeField] private SidebarController sidebarController;

    [Header("Templates")]
    [Tooltip("サイドバーの枠組み (SidebarView.uxml)")]
    [SerializeField] private VisualTreeAsset sidebarViewTemplate;

    [Tooltip("スタート画面のテンプレート (StartView.uxml)")]
    [SerializeField] private VisualTreeAsset startViewTemplate;

    [Header("Menu Data")]
    [SerializeField] private List<MenuItemData> menuItems = new List<MenuItemData>();


    // UI Elements
    public VisualElement ContentArea { get; private set; }
    private VisualElement sidebarContainer;

    // 現在のコントローラーを保持 (インターフェース型に変更)
    private IViewController currentViewController;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        var root = uiDocument.rootVisualElement;

        // 1. エリア取得
        sidebarContainer = root.Q<VisualElement>(UIConstants.CONTAINER_SIDEBAR);
        ContentArea = root.Q<VisualElement>(UIConstants.CONTENT_AREA);

        // 2. サイドバー生成
        if (sidebarContainer != null && sidebarViewTemplate != null)
        {
            sidebarViewTemplate.CloneTree(sidebarContainer);
        }

        // 3. ログシステム初期化
        if (logController != null) logController.Initialize(root);

        // 3.5. セーブインジケーター初期化
        SaveIndicatorController.Instance?.Initialize(root);

        // 4. サイドバー初期化
        if (sidebarController != null && sidebarContainer != null)
        {
            sidebarController.Initialize(sidebarContainer);
            sidebarController.OnMenuChanged += OnSidebarMenuChanged;
        }

        LogUIController.LogSystem("UI System Online.");

        // 初期画面へ（スタート画面から開始）
        SwitchToMenu(MenuType.Start);
    }

    private void OnSidebarMenuChanged(int index)
    {
        if (index >= 0 && index < menuItems.Count)
        {
            SwitchToMenu(menuItems[index].menuType);
        }
    }

    public void SwitchToMenu(MenuType menuType)
    {
        if (ContentArea == null) return;

        // 1. 前の画面を消す
        ContentArea.Clear();

        // ★注意: ここで currentViewController = null; をしてはいけません！
        // AttachLogicController の中で Dispose してから null にします。

        // 2. Start画面の特別処理（サイドバー非表示、MenuItemData不要）
        if (menuType == MenuType.Start)
        {
            // サイドバーを非表示
            if (sidebarContainer != null)
            {
                sidebarContainer.style.display = DisplayStyle.None;
            }

            // Start画面のテンプレートをロード
            if (startViewTemplate != null)
            {
                startViewTemplate.CloneTree(ContentArea);
            }

            // Start画面のロジックをアタッチ
            AttachLogicController(menuType);
            return;
        }

        // 3. 通常画面：サイドバーを表示
        if (sidebarContainer != null)
        {
            sidebarContainer.style.display = DisplayStyle.Flex;
        }

        // 4. データ検索
        var data = menuItems.Find(m => m.menuType == menuType);
        if (data == null) return;

        // 5. テンプレートがあれば表示
        if (data.viewTemplate != null)
        {
            data.viewTemplate.CloneTree(ContentArea);
        }
        else
        {
            // テンプレートがない場合のプレースホルダー
            var label = new Label($"Screen: {menuType}");
            label.style.color = Color.white;
            label.style.fontSize = 24;
            label.style.alignSelf = Align.Center;
            ContentArea.Add(label);
        }

        // 6. ロジックのアタッチ (Dispose処理含む)
        AttachLogicController(menuType);

        // 7. ログ出力
        if (!string.IsNullOrEmpty(data.logMessage))
        {
            LogUIController.Msg(data.logMessage);
        }
    }

    /// <summary>
    /// メニューに応じたC#のロジックを起動する
    /// </summary>
    private void AttachLogicController(MenuType menuType)
    {
        // ★重要：前の監督がいたら、絶対に後始末(Dispose)をさせる
        if (currentViewController != null)
        {
            currentViewController.Dispose(); // 「解散！イベント解除！」と命令
            currentViewController = null;
        }

        switch (menuType)
        {
            case MenuType.Start:
                var startController = new StartUIController();
                startController.Initialize(ContentArea);
                currentViewController = startController;
                break;

            case MenuType.Shop:
                var shopController = new ShopUIController();
                shopController.Initialize(ContentArea);
                currentViewController = shopController;
                break;

            case MenuType.Operators:
                var operatorController = new OperatorUIController();
                operatorController.Initialize(ContentArea);
                operatorController.OnBackRequested += () => SwitchToMenu(MenuType.Shop);
                currentViewController = operatorController;
                break;

            case MenuType.Gacha:
                var gachaController = new GachaUIController();
                gachaController.Initialize(ContentArea);
                currentViewController = gachaController;
                break;

            case MenuType.Home:
                var homeController = new HomeUIController();
                homeController.Initialize(ContentArea);
                currentViewController = homeController;
                break;

            case MenuType.Market:
                var marketController = new MarketUIController();
                marketController.Initialize(ContentArea);
                currentViewController = marketController;
                break;

            case MenuType.Settings:
                var settingsController = new SettingsUIController();
                settingsController.Initialize(ContentArea);
                currentViewController = settingsController;
                break;

            default:
                LogUIController.LogSystem($"{menuType} Logic Not Implemented.");
                break;
        }
    }
}