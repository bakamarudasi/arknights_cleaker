# Unity実装ステップバイステップガイド

> ゼロからこのクリッカーゲームを作る手順書

---

## 全体の流れ

```
Phase 1: プロジェクト準備
Phase 2: データ層（ScriptableObject）
Phase 3: コアシステム（Managers）
Phase 4: UI基盤（UI Toolkit）
Phase 5: ホーム画面（クリック機能）
Phase 6: ショップ画面（強化機能）
Phase 7: ガチャシステム
Phase 8: 株式市場システム
Phase 9: 好感度システム
Phase 10: セーブ/ロード
```

---

# Phase 1: プロジェクト準備

## Step 1.1: Unityプロジェクト作成

1. Unity Hub → New Project
2. テンプレート: **2D** または **2D (URP)**
3. Unity Version: **2022.3 LTS** 以上推奨

## Step 1.2: フォルダ構成作成

Project windowで右クリック → Create → Folder

```
Assets/
├── Scripts/
│   ├── Main_core/
│   │   ├── Core/
│   │   ├── Data/
│   │   │   ├── Definitions/
│   │   │   ├── Character/
│   │   │   └── Save/
│   │   └── Systems/
│   │       ├── Economy/
│   │       ├── Inventory/
│   │       ├── Upgrade/
│   │       ├── Click/
│   │       ├── Income/
│   │       └── SP/
│   └── Main_UI/
│       ├── Main_UI_script/
│       ├── Home_/
│       ├── Shop_/
│       └── Gacha_/
├── Resources/
│   └── Data/
│       ├── Upgrades/
│       ├── Items/
│       └── Characters/
├── UI/
│   ├── UXML/
│   └── USS/
└── Sprites/
```

## Step 1.3: UI Toolkit パッケージ確認

Window → Package Manager → UI Toolkit が入っていることを確認

---

# Phase 2: データ層（ScriptableObject）

## Step 2.1: BaseData（基底クラス）作成

**ファイル:** `Assets/Scripts/Main_core/Data/Definitions/BaseData.cs`

```csharp
using UnityEngine;

/// <summary>
/// 全データの基底クラス
/// </summary>
public abstract class BaseData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("システム内部ID（重複不可）")]
    public string id;

    [Tooltip("表示名")]
    public string displayName;

    [TextArea(2, 4)]
    [Tooltip("説明文")]
    public string description;

    [Tooltip("アイコン")]
    public Sprite icon;
}
```

## Step 2.2: UpgradeData（強化データ）作成

**ファイル:** `Assets/Scripts/Main_core/Data/Definitions/UpgradeData.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New_Upgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : BaseData
{
    // ========================================
    // 強化タイプ
    // ========================================
    public enum UpgradeType
    {
        Click_FlatAdd,      // クリック威力+X
        Click_PercentAdd,   // クリック威力+X%
        Income_FlatAdd,     // 自動収入+X/秒
        Income_PercentAdd,  // 自動収入+X%
        Critical_ChanceAdd, // クリティカル率+X%
        Critical_PowerAdd   // クリティカル倍率+X
    }

    public enum UpgradeCategory
    {
        Click, Income, Critical, Skill, Special
    }

    // ========================================
    // 設定
    // ========================================
    [Header("強化設定")]
    public UpgradeType upgradeType;
    public UpgradeCategory category;

    [Tooltip("1レベルあたりの効果値")]
    public double effectValue = 1;

    [Tooltip("最大レベル（0=無制限）")]
    public int maxLevel = 10;

    [Header("コスト設定")]
    [Tooltip("初期コスト")]
    public double baseCost = 100;

    [Tooltip("レベルごとのコスト倍率")]
    public float costMultiplier = 1.15f;

    [Header("表示設定")]
    public int sortOrder = 0;

    // ========================================
    // 計算メソッド
    // ========================================

    /// <summary>
    /// 指定レベルでのコストを計算
    /// </summary>
    public double GetCostAtLevel(int level)
    {
        return baseCost * System.Math.Pow(costMultiplier, level);
    }

    /// <summary>
    /// 最大レベルに達しているか
    /// </summary>
    public bool IsMaxLevel(int currentLevel)
    {
        return maxLevel > 0 && currentLevel >= maxLevel;
    }
}
```

## Step 2.3: UpgradeDatabase（データベース）作成

**ファイル:** `Assets/Scripts/Main_core/Data/Definitions/UpgradeDatabase.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/Upgrade Database")]
public class UpgradeDatabase : ScriptableObject
{
    public List<UpgradeData> upgrades = new();

    /// <summary>
    /// IDで検索
    /// </summary>
    public UpgradeData GetById(string id)
    {
        return upgrades.Find(u => u.id == id);
    }

    /// <summary>
    /// カテゴリでフィルタ＆ソート
    /// </summary>
    public List<UpgradeData> GetSorted(UpgradeData.UpgradeCategory category)
    {
        return upgrades
            .Where(u => u.category == category)
            .OrderBy(u => u.sortOrder)
            .ToList();
    }
}
```

## Step 2.4: テストデータ作成

1. Project window → `Resources/Data/Upgrades/` を右クリック
2. Create → Game → Upgrade Data
3. 名前を `click_power_1` に変更
4. Inspector で設定:
   - id: `click_power_1`
   - displayName: `クリックパワー`
   - upgradeType: `Click_FlatAdd`
   - category: `Click`
   - effectValue: `1`
   - baseCost: `100`
   - costMultiplier: `1.15`

5. 同様に `Resources/Data/` に `UpgradeDatabase` を作成
6. upgrades リストに作成した UpgradeData をドラッグ

---

# Phase 3: コアシステム（Managers）

## Step 3.1: WalletManager（通貨管理）

**ファイル:** `Assets/Scripts/Main_core/Systems/Economy/WalletManager.cs`

```csharp
using System;
using UnityEngine;

/// <summary>
/// 通貨管理マネージャー
/// </summary>
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    // ========================================
    // 残高
    // ========================================
    [Header("残高")]
    [SerializeField] private double _money = 1000;

    public double Money => _money;

    // ========================================
    // イベント
    // ========================================
    public event Action<double> OnMoneyChanged;

    // ========================================
    // 初期化
    // ========================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ========================================
    // 操作
    // ========================================

    /// <summary>
    /// お金を追加
    /// </summary>
    public void AddMoney(double amount)
    {
        if (amount <= 0) return;
        _money += amount;
        OnMoneyChanged?.Invoke(_money);
    }

    /// <summary>
    /// お金を使用
    /// </summary>
    public bool SpendMoney(double amount)
    {
        if (_money < amount) return false;
        _money -= amount;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    /// <summary>
    /// 支払い可能かチェック
    /// </summary>
    public bool CanAfford(double amount)
    {
        return _money >= amount;
    }
}
```

## Step 3.2: UpgradeManager（強化管理）

**ファイル:** `Assets/Scripts/Main_core/Systems/Upgrade/UpgradeManager.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 強化状態
/// </summary>
public enum UpgradeState
{
    Locked,
    CanUnlockButNotAfford,
    ReadyToUpgrade,
    MaxLevel
}

/// <summary>
/// 強化管理マネージャー
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    // ========================================
    // データ
    // ========================================
    [Header("データベース")]
    [SerializeField] private UpgradeDatabase _database;
    public UpgradeDatabase Database => _database;

    // 強化レベル（ID → Level）
    private Dictionary<string, int> _levels = new();

    // ========================================
    // 依存関係
    // ========================================
    private WalletManager _wallet;

    // ========================================
    // イベント
    // ========================================
    public event Action<UpgradeData, int> OnUpgradePurchased;

    // ========================================
    // 初期化
    // ========================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _wallet = WalletManager.Instance;
    }

    // ========================================
    // レベル管理
    // ========================================

    public int GetLevel(string id)
    {
        return _levels.TryGetValue(id, out int level) ? level : 0;
    }

    public void SetLevel(string id, int level)
    {
        _levels[id] = Mathf.Max(0, level);
    }

    // ========================================
    // 状態取得
    // ========================================

    public UpgradeState GetState(UpgradeData data)
    {
        if (data == null) return UpgradeState.Locked;

        int level = GetLevel(data.id);

        if (data.IsMaxLevel(level))
            return UpgradeState.MaxLevel;

        double cost = data.GetCostAtLevel(level);
        if (_wallet.CanAfford(cost))
            return UpgradeState.ReadyToUpgrade;

        return UpgradeState.CanUnlockButNotAfford;
    }

    // ========================================
    // 購入処理
    // ========================================

    public bool TryPurchase(UpgradeData data)
    {
        if (data == null) return false;

        int level = GetLevel(data.id);

        // MAXチェック
        if (data.IsMaxLevel(level)) return false;

        // コスト計算
        double cost = data.GetCostAtLevel(level);

        // 支払い
        if (!_wallet.SpendMoney(cost)) return false;

        // レベルアップ
        int newLevel = level + 1;
        SetLevel(data.id, newLevel);

        // イベント発火
        OnUpgradePurchased?.Invoke(data, newLevel);

        Debug.Log($"[Upgrade] {data.displayName} Lv.{level} → Lv.{newLevel}");
        return true;
    }
}
```

## Step 3.3: GameController（統括）

**ファイル:** `Assets/Scripts/Main_core/Core/GameController.cs`

```csharp
using System;
using UnityEngine;

/// <summary>
/// ゲーム全体の統括
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    // ========================================
    // マネージャー参照
    // ========================================
    [Header("マネージャー")]
    public WalletManager Wallet;
    public UpgradeManager Upgrade;

    // ========================================
    // クリック設定
    // ========================================
    [Header("クリック設定")]
    public double clickBase = 10;
    public float baseCriticalChance = 0.05f;
    public double baseCriticalMultiplier = 2.0;

    // ========================================
    // ボーナス
    // ========================================
    [Header("ボーナス")]
    public double clickFlatBonus = 0;
    public double clickPercentBonus = 0;

    // ========================================
    // 計算済みステータス
    // ========================================
    private double _finalClickValue;
    public double FinalClickPower => _finalClickValue;

    // ========================================
    // 初期化
    // ========================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        Application.targetFrameRate = 60;
    }

    void Start()
    {
        // マネージャー取得
        Wallet ??= FindFirstObjectByType<WalletManager>();
        Upgrade ??= FindFirstObjectByType<UpgradeManager>();

        // イベント登録
        if (Upgrade != null)
        {
            Upgrade.OnUpgradePurchased += OnUpgradePurchased;
        }

        RecalculateStats();
    }

    // ========================================
    // クリック処理
    // ========================================

    /// <summary>
    /// メインクリック処理
    /// </summary>
    public void ClickMainButton()
    {
        // クリティカル判定
        bool isCritical = UnityEngine.Random.value < baseCriticalChance;
        double multiplier = isCritical ? baseCriticalMultiplier : 1.0;

        // 獲得金額
        double earned = _finalClickValue * multiplier;

        // 通貨追加
        Wallet.AddMoney(earned);

        Debug.Log($"Click! +{earned:N0} (Critical: {isCritical})");
    }

    // ========================================
    // 強化購入時
    // ========================================

    private void OnUpgradePurchased(UpgradeData data, int newLevel)
    {
        switch (data.upgradeType)
        {
            case UpgradeData.UpgradeType.Click_FlatAdd:
                clickFlatBonus += data.effectValue;
                break;
            case UpgradeData.UpgradeType.Click_PercentAdd:
                clickPercentBonus += data.effectValue;
                break;
        }
        RecalculateStats();
    }

    // ========================================
    // ステータス計算
    // ========================================

    public void RecalculateStats()
    {
        _finalClickValue = (clickBase + clickFlatBonus) * (1 + clickPercentBonus);
        Debug.Log($"[Stats] ClickPower: {_finalClickValue}");
    }
}
```

## Step 3.4: シーン設定

1. Hierarchy で空のGameObjectを作成
2. 名前を `GameManager` に変更
3. 以下のコンポーネントをAdd Component:
   - GameController
   - WalletManager
   - UpgradeManager

4. UpgradeManager の Database フィールドに、
   作成した UpgradeDatabase をドラッグ

5. GameController の Wallet と Upgrade フィールドに、
   同じGameObjectの各コンポーネントをドラッグ

---

# Phase 4: UI基盤（UI Toolkit）

## Step 4.1: Panel Settings 作成

1. Project window → 右クリック → Create → UI Toolkit → Panel Settings Asset
2. 名前を `MainPanelSettings` に変更

## Step 4.2: メインUIドキュメント作成

**UXML:** `Assets/UI/UXML/MainUI.uxml`

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="root" class="root">
        <!-- サイドバー -->
        <ui:VisualElement name="sidebar" class="sidebar">
            <ui:Button name="btn-home" text="ホーム" class="menu-btn"/>
            <ui:Button name="btn-shop" text="ショップ" class="menu-btn"/>
        </ui:VisualElement>

        <!-- コンテンツエリア -->
        <ui:VisualElement name="content-area" class="content-area">
            <!-- ここに各画面が入る -->
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

**USS:** `Assets/UI/USS/MainStyles.uss`

```css
.root {
    flex-direction: row;
    flex-grow: 1;
    background-color: #1a1a2e;
}

.sidebar {
    width: 200px;
    background-color: #16213e;
    padding: 20px;
}

.menu-btn {
    height: 50px;
    margin-bottom: 10px;
    font-size: 18px;
    background-color: #0f3460;
    color: white;
    border-width: 0;
}

.menu-btn:hover {
    background-color: #e94560;
}

.content-area {
    flex-grow: 1;
    padding: 20px;
}
```

## Step 4.3: ホーム画面UXML作成

**UXML:** `Assets/UI/UXML/HomeView.uxml`

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="../USS/HomeStyles.uss"/>

    <ui:VisualElement name="home-container" class="home-container">
        <!-- 通貨表示 -->
        <ui:VisualElement class="currency-bar">
            <ui:Label name="money-label" text="1,000" class="money-label"/>
        </ui:VisualElement>

        <!-- クリックエリア -->
        <ui:VisualElement name="click-area" class="click-area">
            <ui:Label name="power-label" text="10" class="power-label"/>
            <ui:Button name="click-button" text="クリック!" class="click-button"/>
        </ui:VisualElement>

        <!-- 統計 -->
        <ui:VisualElement class="stats-bar">
            <ui:Label name="clicks-label" text="クリック: 0" class="stat-label"/>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

**USS:** `Assets/UI/USS/HomeStyles.uss`

```css
.home-container {
    flex-grow: 1;
    align-items: center;
    justify-content: space-between;
    padding: 40px;
}

.currency-bar {
    width: 100%;
    align-items: flex-end;
}

.money-label {
    font-size: 36px;
    color: #fbbf24;
    -unity-font-style: bold;
}

.click-area {
    align-items: center;
}

.power-label {
    font-size: 48px;
    color: white;
    margin-bottom: 20px;
}

.click-button {
    width: 200px;
    height: 200px;
    font-size: 24px;
    background-color: #e94560;
    color: white;
    border-radius: 100px;
    border-width: 0;
    transition: scale 0.1s;
}

.click-button:hover {
    scale: 1.05;
}

.click-button:active {
    scale: 0.95;
}

.stats-bar {
    width: 100%;
}

.stat-label {
    font-size: 18px;
    color: #888;
}
```

## Step 4.4: UIドキュメント配置

1. Hierarchy で空のGameObjectを作成 → `UIRoot`
2. Add Component → UI Document
3. Source Asset に `MainUI.uxml` を設定
4. Panel Settings に `MainPanelSettings` を設定

---

# Phase 5: ホーム画面（クリック機能）

## Step 5.1: IViewController インターフェース

**ファイル:** `Assets/Scripts/Main_UI/Main_UI_script/IViewController.cs`

```csharp
using UnityEngine.UIElements;

/// <summary>
/// 画面コントローラーの共通インターフェース
/// </summary>
public interface IViewController
{
    void Initialize(VisualElement root);
    void Dispose();
}
```

## Step 5.2: HomeUIController

**ファイル:** `Assets/Scripts/Main_UI/Home_/Script/HomeUIController.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ホーム画面コントローラー
/// </summary>
public class HomeUIController : IViewController
{
    private VisualElement _root;

    // UI要素
    private Label _moneyLabel;
    private Label _powerLabel;
    private Button _clickButton;
    private Label _clicksLabel;

    // 統計
    private int _totalClicks;

    // イベントコールバック
    private Action<double> _onMoneyChangedCallback;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        _root = root;

        // UI要素取得
        _moneyLabel = root.Q<Label>("money-label");
        _powerLabel = root.Q<Label>("power-label");
        _clickButton = root.Q<Button>("click-button");
        _clicksLabel = root.Q<Label>("clicks-label");

        // クリックイベント
        _clickButton.clicked += OnClickButton;

        // 通貨変更イベント
        _onMoneyChangedCallback = OnMoneyChanged;
        GameController.Instance.Wallet.OnMoneyChanged += _onMoneyChangedCallback;

        // 初期表示
        UpdateUI();

        Debug.Log("[HomeUIController] Initialized");
    }

    // ========================================
    // クリック処理
    // ========================================

    private void OnClickButton()
    {
        // ゲームロジック実行
        GameController.Instance.ClickMainButton();

        // 統計更新
        _totalClicks++;
        _clicksLabel.text = $"クリック: {_totalClicks:N0}";

        // ボタンアニメーション（パルス）
        _clickButton.AddToClassList("pulse");
        _root.schedule.Execute(() => {
            _clickButton.RemoveFromClassList("pulse");
        }).ExecuteLater(100);
    }

    // ========================================
    // UI更新
    // ========================================

    private void OnMoneyChanged(double amount)
    {
        _moneyLabel.text = FormatNumber(amount);
    }

    private void UpdateUI()
    {
        var gc = GameController.Instance;
        _moneyLabel.text = FormatNumber(gc.Wallet.Money);
        _powerLabel.text = FormatNumber(gc.FinalClickPower);
    }

    private string FormatNumber(double value)
    {
        if (value >= 1_000_000) return $"{value / 1_000_000:F1}M";
        if (value >= 1_000) return $"{value / 1_000:F1}K";
        return value.ToString("N0");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        // イベント解除
        if (_clickButton != null)
        {
            _clickButton.clicked -= OnClickButton;
        }

        if (GameController.Instance?.Wallet != null)
        {
            GameController.Instance.Wallet.OnMoneyChanged -= _onMoneyChangedCallback;
        }

        Debug.Log("[HomeUIController] Disposed");
    }
}
```

## Step 5.3: MainUIController

**ファイル:** `Assets/Scripts/Main_UI/Main_UI_script/MainUIController.cs`

```csharp
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// メインUI統括
/// </summary>
public class MainUIController : MonoBehaviour
{
    public static MainUIController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset homeViewTemplate;
    [SerializeField] private VisualTreeAsset shopViewTemplate;

    // UI Elements
    private VisualElement _contentArea;
    private Button _btnHome;
    private Button _btnShop;

    // 現在のコントローラー
    private IViewController _currentController;

    // ========================================
    // 初期化
    // ========================================

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        var root = uiDocument.rootVisualElement;

        // UI要素取得
        _contentArea = root.Q<VisualElement>("content-area");
        _btnHome = root.Q<Button>("btn-home");
        _btnShop = root.Q<Button>("btn-shop");

        // ボタンイベント
        _btnHome.clicked += () => SwitchToHome();
        _btnShop.clicked += () => SwitchToShop();

        // 初期画面
        SwitchToHome();
    }

    // ========================================
    // 画面切り替え
    // ========================================

    public void SwitchToHome()
    {
        SwitchView(homeViewTemplate, () => new HomeUIController());
    }

    public void SwitchToShop()
    {
        SwitchView(shopViewTemplate, () => new ShopUIController());
    }

    private void SwitchView(VisualTreeAsset template, System.Func<IViewController> createController)
    {
        // 前のコントローラーをDispose
        _currentController?.Dispose();
        _currentController = null;

        // コンテンツクリア
        _contentArea.Clear();

        // テンプレート読み込み
        if (template != null)
        {
            template.CloneTree(_contentArea);
        }

        // 新しいコントローラー作成
        _currentController = createController();
        _currentController.Initialize(_contentArea);
    }
}
```

## Step 5.4: シーン設定

1. `UIRoot` オブジェクトに `MainUIController` をAdd Component
2. Inspector で設定:
   - UI Document: 同じオブジェクトのUIDocumentコンポーネント
   - Home View Template: `HomeView.uxml`
   - Shop View Template: （Phase 6で作成）

3. **Play** して動作確認！

---

# Phase 6: ショップ画面（強化機能）

## Step 6.1: ショップ画面UXML

**UXML:** `Assets/UI/UXML/ShopView.uxml`

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="../USS/ShopStyles.uss"/>

    <ui:VisualElement name="shop-container" class="shop-container">
        <!-- ヘッダー -->
        <ui:VisualElement class="header">
            <ui:Label text="SHOP" class="title"/>
            <ui:Label name="money-label" text="0" class="money"/>
        </ui:VisualElement>

        <!-- メインコンテンツ -->
        <ui:VisualElement class="main-content">
            <!-- アップグレードリスト -->
            <ui:ScrollView name="upgrade-list" class="upgrade-list">
                <!-- 動的に生成 -->
            </ui:ScrollView>

            <!-- 詳細パネル -->
            <ui:VisualElement name="detail-panel" class="detail-panel">
                <ui:Label name="detail-name" text="SELECT ITEM" class="detail-name"/>
                <ui:Label name="detail-level" text="Lv. -" class="detail-level"/>
                <ui:Label name="detail-desc" text="" class="detail-desc"/>
                <ui:Label name="detail-cost" text="" class="detail-cost"/>
                <ui:Button name="buy-button" text="購入" class="buy-button"/>
            </ui:VisualElement>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

**USS:** `Assets/UI/USS/ShopStyles.uss`

```css
.shop-container {
    flex-grow: 1;
}

.header {
    flex-direction: row;
    justify-content: space-between;
    padding: 20px;
    background-color: rgba(0, 0, 0, 0.3);
}

.title {
    font-size: 24px;
    color: white;
    -unity-font-style: bold;
}

.money {
    font-size: 24px;
    color: #fbbf24;
}

.main-content {
    flex-direction: row;
    flex-grow: 1;
    padding: 20px;
}

.upgrade-list {
    flex-grow: 1;
    margin-right: 20px;
}

.upgrade-item {
    padding: 15px;
    margin-bottom: 10px;
    background-color: rgba(255, 255, 255, 0.1);
    border-left-width: 4px;
    border-left-color: #333;
}

.upgrade-item:hover {
    background-color: rgba(255, 255, 255, 0.2);
}

.upgrade-item.selected {
    border-left-color: #e94560;
}

.upgrade-item.affordable {
    border-left-color: #4ade80;
}

.upgrade-name {
    font-size: 18px;
    color: white;
}

.upgrade-level {
    font-size: 14px;
    color: #888;
}

.detail-panel {
    width: 300px;
    padding: 20px;
    background-color: rgba(0, 0, 0, 0.3);
}

.detail-name {
    font-size: 24px;
    color: white;
    margin-bottom: 10px;
}

.detail-level {
    font-size: 18px;
    color: #e94560;
    margin-bottom: 20px;
}

.detail-desc {
    font-size: 14px;
    color: #888;
    margin-bottom: 20px;
}

.detail-cost {
    font-size: 20px;
    color: #fbbf24;
    margin-bottom: 20px;
}

.buy-button {
    height: 50px;
    font-size: 18px;
    background-color: #4ade80;
    color: white;
    border-width: 0;
}

.buy-button:disabled {
    background-color: #333;
    color: #666;
}
```

## Step 6.2: ShopUIController

**ファイル:** `Assets/Scripts/Main_UI/Shop_/Script/ShopUIController.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ショップ画面コントローラー
/// </summary>
public class ShopUIController : IViewController
{
    private VisualElement _root;

    // UI要素
    private Label _moneyLabel;
    private ScrollView _upgradeList;
    private Label _detailName;
    private Label _detailLevel;
    private Label _detailDesc;
    private Label _detailCost;
    private Button _buyButton;

    // 状態
    private UpgradeData _selectedUpgrade;
    private List<VisualElement> _itemElements = new();

    // コールバック
    private Action<double> _onMoneyChangedCallback;
    private Action<UpgradeData, int> _onUpgradePurchasedCallback;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        _root = root;

        // UI要素取得
        _moneyLabel = root.Q<Label>("money-label");
        _upgradeList = root.Q<ScrollView>("upgrade-list");
        _detailName = root.Q<Label>("detail-name");
        _detailLevel = root.Q<Label>("detail-level");
        _detailDesc = root.Q<Label>("detail-desc");
        _detailCost = root.Q<Label>("detail-cost");
        _buyButton = root.Q<Button>("buy-button");

        // イベント
        _buyButton.clicked += OnBuyClicked;

        _onMoneyChangedCallback = _ => RefreshUI();
        _onUpgradePurchasedCallback = (_, _) => RefreshUI();

        GameController.Instance.Wallet.OnMoneyChanged += _onMoneyChangedCallback;
        GameController.Instance.Upgrade.OnUpgradePurchased += _onUpgradePurchasedCallback;

        // リスト生成
        BuildUpgradeList();
        RefreshUI();

        Debug.Log("[ShopUIController] Initialized");
    }

    // ========================================
    // リスト生成
    // ========================================

    private void BuildUpgradeList()
    {
        _upgradeList.Clear();
        _itemElements.Clear();

        var database = GameController.Instance.Upgrade.Database;
        if (database == null) return;

        foreach (var upgrade in database.upgrades)
        {
            var item = CreateUpgradeItem(upgrade);
            _upgradeList.Add(item);
            _itemElements.Add(item);
        }
    }

    private VisualElement CreateUpgradeItem(UpgradeData upgrade)
    {
        var item = new VisualElement();
        item.AddToClassList("upgrade-item");
        item.userData = upgrade;

        var nameLabel = new Label(upgrade.displayName);
        nameLabel.AddToClassList("upgrade-name");

        var levelLabel = new Label();
        levelLabel.AddToClassList("upgrade-level");
        levelLabel.name = $"level-{upgrade.id}";

        item.Add(nameLabel);
        item.Add(levelLabel);

        // クリックイベント
        item.RegisterCallback<PointerDownEvent>(evt => {
            SelectUpgrade(upgrade, item);
        });

        return item;
    }

    // ========================================
    // 選択処理
    // ========================================

    private void SelectUpgrade(UpgradeData upgrade, VisualElement item)
    {
        // 選択状態クリア
        foreach (var element in _itemElements)
        {
            element.RemoveFromClassList("selected");
        }

        // 新しい選択
        item.AddToClassList("selected");
        _selectedUpgrade = upgrade;

        RefreshDetailPanel();
    }

    // ========================================
    // 購入処理
    // ========================================

    private void OnBuyClicked()
    {
        if (_selectedUpgrade == null) return;

        GameController.Instance.Upgrade.TryPurchase(_selectedUpgrade);
    }

    // ========================================
    // UI更新
    // ========================================

    private void RefreshUI()
    {
        // 通貨表示
        _moneyLabel.text = GameController.Instance.Wallet.Money.ToString("N0");

        // リスト更新
        var upgradeManager = GameController.Instance.Upgrade;

        foreach (var item in _itemElements)
        {
            if (item.userData is UpgradeData upgrade)
            {
                int level = upgradeManager.GetLevel(upgrade.id);
                var levelLabel = item.Q<Label>($"level-{upgrade.id}");
                if (levelLabel != null)
                {
                    levelLabel.text = $"Lv.{level}";
                }

                // 購入可能状態
                var state = upgradeManager.GetState(upgrade);
                item.RemoveFromClassList("affordable");
                if (state == UpgradeState.ReadyToUpgrade)
                {
                    item.AddToClassList("affordable");
                }
            }
        }

        RefreshDetailPanel();
    }

    private void RefreshDetailPanel()
    {
        if (_selectedUpgrade == null)
        {
            _detailName.text = "SELECT ITEM";
            _detailLevel.text = "Lv. -";
            _detailDesc.text = "";
            _detailCost.text = "";
            _buyButton.SetEnabled(false);
            return;
        }

        var upgradeManager = GameController.Instance.Upgrade;
        int level = upgradeManager.GetLevel(_selectedUpgrade.id);
        double cost = _selectedUpgrade.GetCostAtLevel(level);
        var state = upgradeManager.GetState(_selectedUpgrade);

        _detailName.text = _selectedUpgrade.displayName;
        _detailLevel.text = $"Lv.{level}";
        _detailDesc.text = _selectedUpgrade.description;
        _detailCost.text = state == UpgradeState.MaxLevel ? "MAX" : cost.ToString("N0");

        _buyButton.SetEnabled(state == UpgradeState.ReadyToUpgrade);
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        _buyButton.clicked -= OnBuyClicked;

        if (GameController.Instance?.Wallet != null)
        {
            GameController.Instance.Wallet.OnMoneyChanged -= _onMoneyChangedCallback;
        }

        if (GameController.Instance?.Upgrade != null)
        {
            GameController.Instance.Upgrade.OnUpgradePurchased -= _onUpgradePurchasedCallback;
        }

        Debug.Log("[ShopUIController] Disposed");
    }
}
```

## Step 6.3: MainUIControllerにテンプレート設定

1. MainUIController の Shop View Template に `ShopView.uxml` を設定
2. **Play** して動作確認！

---

# Phase 7以降: 追加システム

Phase 7〜10は同様のパターンで実装:

1. **データ定義**（ScriptableObject）
2. **Manager作成**（ビジネスロジック）
3. **UXML/USS作成**（UI）
4. **UIController作成**（UI制御）
5. **GameControllerに統合**

---

# チェックリスト

## Phase 1-2 完了チェック
- [ ] フォルダ構成作成
- [ ] BaseData.cs 作成
- [ ] UpgradeData.cs 作成
- [ ] UpgradeDatabase.cs 作成
- [ ] テスト用UpgradeDataアセット作成

## Phase 3 完了チェック
- [ ] WalletManager.cs 作成
- [ ] UpgradeManager.cs 作成
- [ ] GameController.cs 作成
- [ ] シーンにGameManagerオブジェクト配置

## Phase 4 完了チェック
- [ ] Panel Settings作成
- [ ] MainUI.uxml 作成
- [ ] MainStyles.uss 作成
- [ ] HomeView.uxml 作成
- [ ] HomeStyles.uss 作成
- [ ] シーンにUIRootオブジェクト配置

## Phase 5-6 完了チェック
- [ ] IViewController.cs 作成
- [ ] HomeUIController.cs 作成
- [ ] MainUIController.cs 作成
- [ ] ShopView.uxml 作成
- [ ] ShopStyles.uss 作成
- [ ] ShopUIController.cs 作成
- [ ] 動作確認：クリックでお金が増える
- [ ] 動作確認：ショップで強化が買える

---

*各Phaseを順番に進めていけば、基本的なクリッカーゲームが完成します！*
