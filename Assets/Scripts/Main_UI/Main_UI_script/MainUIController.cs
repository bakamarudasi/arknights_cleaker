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

    [Header("Menu Data")]
    [SerializeField] private List<MenuItemData> menuItems = new List<MenuItemData>();

    [Header("Database")]
    [SerializeField] private UpgradeDatabase upgradeDatabase;
    [SerializeField] private GachaDatabase gachaDatabase;


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

        // 4. サイドバー初期化
        if (sidebarController != null && sidebarContainer != null)
        {
            sidebarController.Initialize(sidebarContainer);
            sidebarController.OnMenuChanged += OnSidebarMenuChanged;
        }

        LogUIController.LogSystem("UI System Online.");

        // 初期画面へ
        SwitchToMenu(MenuType.Shop);
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

        // 2. データ検索
        var data = menuItems.Find(m => m.menuType == menuType);
        if (data == null) return;

        // 3. テンプレートがあれば表示
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

        // 4. ロジックのアタッチ (Dispose処理含む)
        AttachLogicController(menuType);

        // 5. ログ出力
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
            case MenuType.Shop:
                var shopController = new ShopUIController();
                shopController.Initialize(ContentArea, upgradeDatabase);
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
                gachaController.Initialize(ContentArea, gachaDatabase);
                currentViewController = gachaController;
                break;

            case MenuType.Home:
                var homeController = new HomeUIController();
                homeController.Initialize(ContentArea);
                currentViewController = homeController;
                break;

            default:
                LogUIController.LogSystem($"{menuType} Logic Not Implemented.");
                break;
        }
    }
}