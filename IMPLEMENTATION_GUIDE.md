# アークナイツ風クリッカーゲーム 完全実装ガイド

> このドキュメントはゲーム全体の実装方法を詳細に解説します。
> 新規開発者やAIがプロジェクトを理解するためのリファレンスです。

---

## 目次

1. [プロジェクト概要](#1-プロジェクト概要)
2. [アーキテクチャ全体図](#2-アーキテクチャ全体図)
3. [コアシステム詳細](#3-コアシステム詳細)
4. [UIシステム詳細](#4-uiシステム詳細)
5. [データ構造詳細](#5-データ構造詳細)
6. [ガチャシステム詳細](#6-ガチャシステム詳細)
7. [株式市場システム詳細](#7-株式市場システム詳細)
8. [好感度システム詳細](#8-好感度システム詳細)
9. [イベント駆動設計](#9-イベント駆動設計)
10. [セーブ/ロードシステム](#10-セーブロードシステム)
11. [UI Toolkit使用ガイド](#11-ui-toolkit使用ガイド)
12. [新機能追加手順](#12-新機能追加手順)

---

# 1. プロジェクト概要

## 1.1 ゲームコンセプト

「アークナイツ」世界観のクリッカー/放置ゲーム。

**主要ゲームループ:**
```
クリック → LMD獲得 → 強化購入 → クリック威力UP → より多くのLMD獲得
              ↓
         ガチャでキャラ獲得 → 好感度UP → ボーナス効果
              ↓
         株式投資 → 配当・売買益 → さらなる資産増加
```

## 1.2 技術スタック

| 項目 | 技術 |
|------|------|
| エンジン | Unity 2022+ |
| UI | UI Toolkit (USS + UXML) |
| データ | ScriptableObject |
| 言語 | C# |

## 1.3 ディレクトリ構造

```
Assets/Scripts/
├── Main_core/                    # コアシステム（ビジネスロジック）
│   ├── Core/
│   │   └── GameController.cs     # ゲーム全体の統括（シングルトン）
│   ├── Data/
│   │   ├── Definitions/          # ScriptableObject定義
│   │   │   ├── BaseData.cs       # 全データの基底クラス
│   │   │   ├── UpgradeData.cs    # 強化データ
│   │   │   ├── ItemData.cs       # アイテムデータ
│   │   │   └── CompanyData.cs    # 企業データ
│   │   ├── Character/
│   │   │   └── CharacterData.cs  # キャラクターデータ
│   │   ├── Save/
│   │   │   └── ProgressData.cs   # セーブデータ構造
│   │   └── Contexts/
│   │       └── ClickStatsContext.cs  # クリック計算コンテキスト
│   ├── Systems/
│   │   ├── Economy/
│   │   │   └── WalletManager.cs  # 通貨管理
│   │   ├── Inventory/
│   │   │   └── InventoryManager.cs # アイテム所持管理
│   │   ├── Upgrade/
│   │   │   └── UpgradeManager.cs # 強化システム
│   │   ├── Click/
│   │   │   └── ClickManager.cs   # クリック計算（静的）
│   │   ├── Income/
│   │   │   └── IncomeManager.cs  # 自動収入
│   │   ├── SP/
│   │   │   └── SPManager.cs      # SP・フィーバー管理
│   │   ├── Affection/
│   │   │   └── AffectionManager.cs # 好感度システム
│   │   ├── Market/               # 株式市場システム
│   │   │   ├── MarketManager.cs
│   │   │   ├── StockPriceEngine.cs
│   │   │   ├── PortfolioManager.cs
│   │   │   ├── MarketEventBus.cs
│   │   │   └── Data/
│   │   │       ├── StockData.cs
│   │   │       └── StockDatabase.cs
│   │   └── Audio/
│   │       └── AudioManager.cs
│   └── UI/
│       ├── Managers/
│       │   └── FloatingTextManager.cs
│       └── Common/
│           ├── FloatingTextController.cs
│           └── UI_RollingCounter.cs
│
├── Main_UI/                      # UI関連
│   ├── Main_UI_script/
│   │   ├── MainUIController.cs   # UI全体の統括
│   │   └── IViewController.cs    # 画面コントローラーIF
│   ├── Sodebar_/                 # サイドバー
│   │   └── Script/
│   │       ├── SidebarController.cs
│   │       ├── MenuItemData.cs
│   │       └── UIConstants.cs
│   ├── Start_/                   # スタート画面
│   │   └── Script/
│   │       └── StartUIController.cs
│   ├── Home_/                    # ホーム画面（メインクリック）
│   │   └── Script/
│   │       ├── HomeUIController.cs
│   │       └── ClickAreaHandler.cs
│   ├── Shop_/                    # ショップ画面
│   │   └── Script/
│   │       ├── ShopUIController.cs      # ファサード
│   │       ├── ShopTabController.cs
│   │       ├── ShopDetailPanelController.cs
│   │       ├── ShopAnimationHelper.cs
│   │       ├── ShopService.cs           # ビジネスロジック
│   │       ├── ShopItemView.cs
│   │       └── ShopUIConstants.cs
│   ├── Gacha_/                   # ガチャ画面
│   │   └── Script/
│   │       ├── GachaUIController.cs
│   │       ├── GachaBannerController.cs
│   │       ├── GachaManager.cs
│   │       ├── GachaResultAnimator.cs
│   │       └── GachaVisualEffectController.cs
│   ├── Market_/                  # 株式市場画面
│   │   └── Script/
│   │       ├── MarketUIController.cs
│   │       ├── MarketChartController.cs
│   │       ├── MarketTradeController.cs
│   │       └── MarketSkillController.cs
│   ├── Operator_/                # オペレーター画面
│   │   └── Script/
│   │       ├── OperatorUIController.cs
│   │       ├── OperatorLensController.cs
│   │       ├── OperatorGiftController.cs
│   │       └── OperatorAffectionController.cs
│   └── Log_/                     # ログ表示
│       └── Script/
│           └── LogUIController.cs
```

---

# 2. アーキテクチャ全体図

## 2.1 レイヤー構成

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                              │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐           │
│  │  Home   │ │  Shop   │ │  Gacha  │ │ Market  │  ...      │
│  │ UICtrl  │ │ UICtrl  │ │ UICtrl  │ │ UICtrl  │           │
│  └────┬────┘ └────┬────┘ └────┬────┘ └────┬────┘           │
│       │           │           │           │                  │
│       └───────────┴─────┬─────┴───────────┘                  │
│                         │                                    │
│                  MainUIController                            │
└─────────────────────────┼────────────────────────────────────┘
                          │ イベント通知
┌─────────────────────────┼────────────────────────────────────┐
│                   Service Layer                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                   │
│  │ShopService│  │(他Service)│  │(他Service)│                   │
│  └─────┬────┘  └─────┬────┘  └─────┬────┘                   │
└────────┼─────────────┼─────────────┼─────────────────────────┘
         │             │             │
┌────────┼─────────────┼─────────────┼─────────────────────────┐
│        │       Core Layer          │                         │
│        ▼             ▼             ▼                         │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                  GameController                       │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │   │
│  │  │ Wallet │ │Inventory│ │Upgrade │ │ Income │        │   │
│  │  │Manager │ │Manager │ │Manager │ │Manager │        │   │
│  │  └────────┘ └────────┘ └────────┘ └────────┘        │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │   │
│  │  │   SP   │ │ Gacha  │ │ Market │ │Affection│        │   │
│  │  │Manager │ │Manager │ │Manager │ │Manager │        │   │
│  │  └────────┘ └────────┘ └────────┘ └────────┘        │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────┼────────────────────────────────────┐
│                   Data Layer                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │UpgradeData│  │ ItemData │  │ StockData│  │CharacterData│   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
│                  (ScriptableObject)                          │
└──────────────────────────────────────────────────────────────┘
```

## 2.2 設計パターン

### シングルトンパターン
全Managerクラスはシングルトン:
```csharp
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
```

### ファサードパターン
大きなUIControllerは責任を分割:
```csharp
public class ShopUIController : IViewController
{
    private ShopTabController tabController;        // タブ管理
    private ShopDetailPanelController detailPanel;  // 詳細表示
    private ShopAnimationHelper animationHelper;    // アニメーション
    private ShopService shopService;                // ビジネスロジック
}
```

### イベント駆動
Manager間の通信はC#イベント:
```csharp
// 発行側
public event Action<double> OnMoneyChanged;
wallet.OnMoneyChanged?.Invoke(newAmount);

// 購読側
wallet.OnMoneyChanged += UpdateMoneyUI;
```

---

# 3. コアシステム詳細

## 3.1 GameController

**役割:** ゲーム全体の統括。各Managerの参照を保持し、クリック処理とステータス計算を担当。

**ファイル:** `Main_core/Core/GameController.cs`

### 主要プロパティ
```csharp
public static GameController Instance { get; private set; }

// マネージャー参照
public WalletManager Wallet;
public InventoryManager Inventory;
public UpgradeManager Upgrade;
public IncomeManager Income;
public SPManager SP;
public GachaManager Gacha;

// クリック設定
public double clickBase = 10;
public float baseCriticalChance = 0.05f;
public double baseCriticalMultiplier = 2.0;

// 計算済みステータス
public double FinalClickPower => _finalClickValue;
public float FinalCritChance => _finalCritChance;
public double FinalCritMultiplier => _finalCritMultiplier;
```

### クリック処理フロー
```
ClickMainButton() 呼び出し
    │
    ├── SP.ChargeSP()  // SPを加算
    │
    ├── ClickStatsContext 作成
    │   ├── BaseClickValue（計算済みクリック威力）
    │   ├── CriticalChance（クリティカル率）
    │   ├── CriticalMultiplier（クリティカル倍率）
    │   └── FeverMultiplier（フィーバー倍率）
    │
    ├── ClickManager.Calculate(ctx)  // 純粋計算
    │   └── ClickResult {
    │         EarnedAmount,    // 獲得金額
    │         WasCritical      // クリティカルか
    │       }
    │
    ├── Wallet.AddMoney(result.EarnedAmount)
    │
    └── 統計更新 & FloatingText表示
```

### ステータス再計算
```csharp
public void RecalculateStats()
{
    // (基礎 + フラット) × (1 + パーセント) × グローバル
    _finalClickValue = (clickBase + clickFlatBonus)
                     * (1 + clickPercentBonus)
                     * globalMultiplier;

    _finalCritChance = Mathf.Clamp01(baseCriticalChance + (float)criticalChanceBonus);
    _finalCritMultiplier = baseCriticalMultiplier + criticalPowerBonus;

    OnStatsRecalculated?.Invoke();
}
```

## 3.2 WalletManager

**役割:** 通貨（LMD・資格証）の残高管理

**ファイル:** `Main_core/Systems/Economy/WalletManager.cs`

### 通貨タイプ
```csharp
public enum CurrencyType
{
    LMD,          // 龍門幣（メイン通貨）
    Certificate   // 資格証（プレミアム通貨）
}
```

### 主要メソッド
```csharp
// 残高取得
public double Money => _money;
public double Certificates => _certificates;

// 加算
public void AddMoney(double amount);
public void AddCertificates(double amount);

// 支払い（残高チェック付き）
public bool SpendMoney(double amount);
public bool SpendCertificates(double amount);

// 汎用（通貨タイプ指定）
public bool CanAfford(double amount, CurrencyType type);
public bool Spend(double amount, CurrencyType type);
```

### イベント
```csharp
public event Action<double> OnMoneyChanged;
public event Action<double> OnCertificateChanged;

// 統計用
public Action<double> OnMoneyEarned;
public Action<double> OnMoneySpent;
```

## 3.3 InventoryManager

**役割:** アイテムの所持数管理

**ファイル:** `Main_core/Systems/Inventory/InventoryManager.cs`

### データ構造
```csharp
// アイテムID → 所持数
private SerializableDictionary<string, int> _inventory = new();
```

### 主要メソッド
```csharp
// 所持数取得
public int GetCount(string id);

// 追加・使用
public void Add(string id, int amount = 1);
public bool Use(string id, int amount = 1);
public bool Has(string id, int amount = 1);

// 素材チェック（複数アイテム一括）
public bool HasAllMaterials(List<ItemCost> costs);
public bool UseAllMaterials(List<ItemCost> costs);

// 解放用アイテムチェック
public bool HasUnlockItem(ItemData item);
```

## 3.4 UpgradeManager

**役割:** 強化の購入処理・レベル管理

**ファイル:** `Main_core/Systems/Upgrade/UpgradeManager.cs`

### 強化状態
```csharp
public enum UpgradeState
{
    Locked,                 // 前提条件未達成
    CanUnlockButNotAfford,  // 条件OK、資金/素材不足
    ReadyToUpgrade,         // 購入可能
    MaxLevel                // 最大レベル到達
}
```

### 購入判定フロー
```csharp
public UpgradeState GetState(UpgradeData data)
{
    // 1. MAXチェック
    if (data.IsMaxLevel(currentLevel))
        return UpgradeState.MaxLevel;

    // 2. 前提条件チェック
    if (!MeetsPrerequisite(data))
        return UpgradeState.Locked;

    // 3. 購入可能チェック（通貨+素材）
    if (CanPurchase(data))
        return UpgradeState.ReadyToUpgrade;

    return UpgradeState.CanUnlockButNotAfford;
}
```

### 購入処理
```csharp
public bool TryPurchase(UpgradeData data)
{
    if (!CanPurchase(data)) return false;

    // 通貨消費
    wallet.Spend(cost, currencyType);

    // 素材消費
    inventory.UseAllMaterials(data.requiredMaterials);

    // レベルアップ
    AddLevel(data.id);

    // イベント発火
    OnUpgradePurchased?.Invoke(data, newLevel);

    return true;
}
```

## 3.5 IncomeManager

**役割:** 自動収入（毎秒収入）の計算と付与

**ファイル:** `Main_core/Systems/Income/IncomeManager.cs`

### 収入計算式
```
最終収入 = (基礎 + フラットボーナス) × (1 + パーセントボーナス) × グローバル倍率
```

### 主要メソッド
```csharp
// 開始・停止
public void StartIncome();
public void StopIncome();

// ボーナス追加
public void AddFlatBonus(double amount);
public void AddPercentBonus(double amount);

// 1秒あたりの収入
public double IncomePerSecond => _finalIncomePerTick / tickInterval;

// オフライン収入計算
public double CalculateOfflineEarnings(double offlineSeconds, double efficiency = 0.5);
```

## 3.6 SPManager

**役割:** SP（スキルポイント）とフィーバーモードの管理

**ファイル:** `Main_core/Systems/SP/SPManager.cs`

### フィーバーシステム
```
クリック → SP加算 → SP満タン → フィーバー発動（10秒）
                                    ↓
                              クリック威力3倍
                                    ↓
                              フィーバー終了 → SP 0にリセット
```

### 主要プロパティ
```csharp
public float CurrentSP => _currentSP;
public float MaxSP => _maxSP;
public bool IsFeverActive => _isFeverActive;
public float FillRate => _currentSP / _maxSP;

public float FinalChargeAmount => _baseChargeAmount + _chargeBonus;
public float FinalFeverMultiplier => _baseFeverMultiplier + _feverPowerBonus;
```

## 3.7 ClickManager

**役割:** クリック時の収入計算（純粋計算、副作用なし）

**ファイル:** `Main_core/Systems/Click/ClickManager.cs`

### 特徴
- **静的クラス**（インスタンス不要）
- 純粋な計算ロジックのみ
- 副作用（お金加算等）は呼び出し元で行う

### 計算メソッド
```csharp
public static ClickResult Calculate(ClickStatsContext context)
{
    bool isCritical = Random.value < context.CriticalChance;

    double multiplier = 1.0;
    if (isCritical) multiplier = context.CriticalMultiplier;
    if (context.IsFeverActive) multiplier *= context.FeverMultiplier;

    double earnedAmount = context.BaseClickValue * multiplier;

    return new ClickResult(earnedAmount, isCritical, multiplier);
}
```

---

# 4. UIシステム詳細

## 4.1 MainUIController

**役割:** UI全体の統括。画面遷移とビューの管理。

**ファイル:** `Main_UI/Main_UI_script/MainUIController.cs`

### 画面タイプ
```csharp
public enum MenuType
{
    Start,      // スタート画面
    Home,       // ホーム（メインクリック）
    Shop,       // ショップ（強化）
    Operators,  // オペレーター
    Gacha,      // ガチャ
    Market      // 株式市場
}
```

### 画面遷移フロー
```csharp
public void SwitchToMenu(MenuType menuType)
{
    // 1. コンテンツエリアをクリア
    ContentArea.Clear();

    // 2. Start画面はサイドバー非表示
    if (menuType == MenuType.Start)
    {
        sidebarContainer.style.display = DisplayStyle.None;
    }
    else
    {
        sidebarContainer.style.display = DisplayStyle.Flex;
    }

    // 3. テンプレート（UXML）をロード
    data.viewTemplate.CloneTree(ContentArea);

    // 4. ロジックコントローラーをアタッチ
    AttachLogicController(menuType);
}
```

### コントローラーのライフサイクル
```csharp
private void AttachLogicController(MenuType menuType)
{
    // ★重要: 前のコントローラーを必ずDispose
    if (currentViewController != null)
    {
        currentViewController.Dispose();
        currentViewController = null;
    }

    // 新しいコントローラーを生成
    switch (menuType)
    {
        case MenuType.Home:
            var homeController = new HomeUIController();
            homeController.Initialize(ContentArea);
            currentViewController = homeController;
            break;
        // ...
    }
}
```

## 4.2 IViewController インターフェース

全UIコントローラーが実装する共通インターフェース:
```csharp
public interface IViewController
{
    void Initialize(VisualElement root);
    void Dispose();
}
```

## 4.3 HomeUIController

**役割:** ホーム画面（メインクリック画面）の管理

**ファイル:** `Main_UI/Home_/Script/HomeUIController.cs`

### サブハンドラー構成
```
HomeUIController
    └── ClickAreaHandler    # クリック処理・コンボ・ダメージ表示
```

### 更新ループ
```csharp
private void SetupUpdateLoop()
{
    _updateSchedule = _root.schedule.Execute(() =>
    {
        _clickHandler?.UpdateComboDecay();
        _clickHandler?.CleanupDamageNumbers();
        _clickHandler?.UpdateDPS();
        UpdateParticles();
        UpdateStatsDisplay();
    }).Every(50); // 20fps更新
}
```

## 4.4 ClickAreaHandler

**役割:** クリックエリアの処理（コンボ、ダメージ数字、エフェクト）

**ファイル:** `Main_UI/Home_/Script/ClickAreaHandler.cs`

### コンボシステム
```csharp
private const float ComboTimeout = 1.5f;  // コンボ維持時間
private const int HighComboThreshold = 50; // 高コンボ閾値

// コンボボーナス（10コンボ以上で1%ずつ増加）
if (_comboCount > 10)
{
    float comboMultiplier = 1f + (_comboCount * 0.01f);
    finalDamage *= comboMultiplier;
}
```

### ダメージ数字プール
パフォーマンス最適化のためオブジェクトプールを使用:
```csharp
private readonly Queue<Label> _damageNumberPool = new();
private readonly List<Label> _activeDamageNumbers = new();
private const int DamageNumberPoolSize = 20;
```

## 4.5 ShopUIController

**役割:** ショップ画面のファサード

**ファイル:** `Main_UI/Shop_/Script/ShopUIController.cs`

### サブコントローラー構成
```
ShopUIController (ファサード)
    ├── ShopTabController           # タブ管理
    ├── ShopDetailPanelController   # 詳細パネル
    ├── ShopAnimationHelper         # アニメーション
    └── ShopService                 # ビジネスロジック
```

### ListView設定
```csharp
private void SetupListView()
{
    upgradeListView.makeItem = MakeItem;       // アイテムUI生成
    upgradeListView.bindItem = BindItem;       // データバインド
    upgradeListView.itemsSource = currentList;
    upgradeListView.fixedItemHeight = ShopUIConstants.LIST_ITEM_HEIGHT;
    upgradeListView.selectionType = SelectionType.Single;
    upgradeListView.selectionChanged += OnSelectionChanged;
}
```

## 4.6 ShopService

**役割:** ショップのビジネスロジック（UI層から分離）

**ファイル:** `Main_UI/Shop_/Script/ShopService.cs`

### 一括購入計算
```csharp
public int CalculateMaxBuyCount(UpgradeData upgrade, double availableMoney)
{
    int count = 0;
    double totalCost = 0;
    int level = currentLevel;

    while (count < safetyLimit)
    {
        double nextCost = upgrade.GetCostAtLevel(level);
        if (totalCost + nextCost > availableMoney) break;

        totalCost += nextCost;
        level++;
        count++;
    }

    return count;
}
```

---

# 5. データ構造詳細

## 5.1 BaseData（基底クラス）

全データの共通フィールド:
```csharp
public abstract class BaseData : ScriptableObject
{
    public string id;           // システムID（重複不可）
    public string displayName;  // 表示名
    public string description;  // 説明文
    public Sprite icon;         // アイコン
    public CompanyData affiliatedCompany;  // 所属企業
}
```

## 5.2 UpgradeData（強化データ）

```csharp
[CreateAssetMenu(fileName = "New_Upgrade", menuName = "ArknightsClicker/Upgrade Data")]
public class UpgradeData : BaseData
{
    // 強化タイプ
    public enum UpgradeType
    {
        Click_FlatAdd,      // クリック威力+X
        Click_PercentAdd,   // クリック威力+X%
        Income_FlatAdd,     // 自動収入+X/秒
        Income_PercentAdd,  // 自動収入+X%
        Critical_ChanceAdd, // クリティカル率+X%
        Critical_PowerAdd,  // クリティカル倍率+X
        SP_ChargeAdd,       // SPチャージ+X%
        Fever_PowerAdd      // フィーバー倍率+X
    }

    // カテゴリ
    public enum UpgradeCategory
    {
        Click, Income, Critical, Skill, Special
    }

    // 基本設定
    public UpgradeType upgradeType;
    public UpgradeCategory category;
    public double effectValue = 1;
    public int maxLevel = 10;  // 0 = 無制限

    // コスト設定
    public CurrencyType currencyType = CurrencyType.LMD;
    public double baseCost = 100;
    public float costMultiplier = 1.15f;
    public List<ItemCost> requiredMaterials;

    // 解放条件
    public ItemData requiredUnlockItem;
    public UpgradeData prerequisiteUpgrade;
    public int prerequisiteLevel = 1;

    // 株式連動
    public StockData relatedStock;
    public bool scaleWithHolding = false;
    public float maxHoldingMultiplier = 2.0f;

    // コスト計算
    public double GetCostAtLevel(int currentLevel)
    {
        return baseCost * Math.Pow(costMultiplier, currentLevel);
    }
}
```

## 5.3 ItemData（アイテムデータ）

```csharp
[CreateAssetMenu(fileName = "New_Item", menuName = "ArknightsClicker/Item Data")]
public class ItemData : BaseData
{
    public enum ItemType
    {
        KeyItem,    // 重要アイテム（キャラ、レンズ等）
        Material,   // 素材
        Consumable  // 消耗品
    }

    public enum Rarity
    {
        Star1, Star2, Star3, Star4, Star5, Star6
    }

    public ItemType type;
    public Rarity rarity;
    public int sortOrder = 0;
    public int maxStack = -1;
    public int sellPrice = 0;

    // 消耗品設定
    public ConsumableType useEffect;
    public float effectValue;
    public float effectDuration;

    // レンズ設定（透視機能）
    public LensSpecs lensSpecs;

    // ガチャ被り設定（潜在システム）
    public ItemData convertToItem;
    public int convertAmount = 1;
}
```

## 5.4 StockData（株式データ）

```csharp
[CreateAssetMenu(fileName = "New_Stock", menuName = "ArknightsClicker/Market/Stock Data")]
public class StockData : ScriptableObject
{
    // 基本情報
    public string stockId;      // 銘柄コード（例: RL, PL）
    public string companyName;  // 企業名
    public string description;
    public Sprite logo;

    // 株価設定
    public double initialPrice = 1000;
    public double minPrice = 10;
    public double maxPrice = 0;  // 0 = 無制限

    // 変動特性
    public float volatility = 0.1f;      // ボラティリティ
    public float drift = 0.02f;          // 長期トレンド
    public float jumpProbability = 0.01f; // 急騰/暴落確率
    public float jumpIntensity = 0.2f;   // 急騰/暴落強度

    // 企業特性
    public StockTrait trait;
    public float transactionFee = 0.01f;

    // 解放条件
    public ItemData unlockKeyItem;

    // 配当設定
    public long totalShares = 1000000;
    public float dividendRate = 0f;
    public int dividendIntervalSeconds = 0;

    // 保有ボーナス
    public List<StockHoldingBonus> holdingBonuses = new();
}
```

---

# 6. ガチャシステム詳細

## 6.1 GachaManager

**役割:** ガチャの抽選ロジック、天井管理、在庫管理

**ファイル:** `Main_UI/Gacha_/Script/GachaManager.cs`

### ガチャ実行フロー
```
PullGacha(banner, count)
    │
    ├── 通貨チェック → Wallet.CanAfford()
    │
    ├── 通貨消費 → Wallet.Spend()
    │
    ├── Pull(banner, count)
    │   └── [count回ループ]
    │       ├── 在庫チェック
    │       ├── 天井カウント++
    │       ├── 天井到達? → 最高レア確定
    │       ├── 通常抽選（ソフト天井考慮）
    │       ├── 在庫消費（ボックスガチャの場合）
    │       ├── 高レア排出 → 天井リセット
    │       └── GachaResultItem 生成
    │
    └── 結果をインベントリに追加
```

### 天井システム
```csharp
// ハード天井: 確定で最高レア排出
if (banner.hasPity && currentPity >= banner.pityCount)
{
    selectedEntry = GetHighestRarityEntry(banner);
    ResetPityCount(banner.bannerId);
}

// ソフト天井: 高レア確率が徐々に上昇
if (currentPity >= banner.softPityStart && entry.Rarity >= 5)
{
    float pityProgress = (currentPity - banner.softPityStart)
                       / (banner.pityCount - banner.softPityStart);
    weight *= (1f + pityProgress * 5f); // 最大6倍
}
```

### ボックスガチャ（封入数システム）
```csharp
// 在庫初期化
foreach (var entry in banner.pool)
{
    if (entry.HasStockLimit)
    {
        bannerStock[entry.item.id] = entry.stockCount;
    }
}

// 抽選時に在庫チェック
if (entry.HasStockLimit)
{
    int remaining = GetRemainingStock(banner, entry.item.id);
    if (remaining == 0) weight = 0f;  // 在庫切れは排出しない
}

// 排出後に在庫消費
ConsumeStock(banner, selectedEntry.item.id);
```

## 6.2 GachaBannerData

```csharp
public class GachaBannerData : ScriptableObject
{
    public string bannerId;
    public string bannerName;
    public string description;

    // コスト
    public CurrencyType currencyType;
    public double costSingle = 600;
    public double costTen = 6000;

    // 天井
    public bool hasPity = true;
    public int pityCount = 50;
    public int softPityStart = 40;

    // ピックアップ
    public List<ItemData> pickupItems;
    public float pickupRateBoost = 0.5f;

    // 排出プール
    public List<GachaPoolEntry> pool;

    // 解放条件
    public bool startsLocked = false;
    public ItemData requiredUnlockItem;
    public GachaBannerData prerequisiteBanner;
}
```

---

# 7. 株式市場システム詳細

## 7.1 MarketManager

**役割:** 株価の更新、履歴管理、ニュース生成

**ファイル:** `Main_core/Systems/Market/MarketManager.cs`

### 株価更新ループ
```csharp
private void Update()
{
    if (!isMarketOpen) return;

    // 株価更新（デフォルト1秒間隔）
    tickTimer += Time.deltaTime;
    if (tickTimer >= tickInterval)
    {
        tickTimer = 0f;
        UpdateAllPrices();
    }

    // ニュース生成（デフォルト30秒間隔）
    newsTimer += Time.deltaTime;
    if (newsTimer >= newsInterval)
    {
        newsTimer = 0f;
        GenerateRandomNews();
    }
}
```

### 株価計算（StockPriceEngine）
幾何ブラウン運動 + ジャンプ拡散モデル:
```csharp
public static double CalculateNextPrice(
    double currentPrice,
    float drift,
    float volatility,
    float deltaTime,
    float jumpProbability,
    float jumpIntensity,
    double minPrice,
    double maxPrice)
{
    // 通常変動（幾何ブラウン運動）
    double randomReturn = (drift * deltaTime)
                        + (volatility * normalRandom * Sqrt(deltaTime));

    // ジャンプ（急騰/暴落）
    if (Random.value < jumpProbability)
    {
        float direction = Random.value > 0.5f ? 1f : -1f;
        randomReturn += direction * jumpIntensity;
    }

    double newPrice = currentPrice * Exp(randomReturn);
    return Clamp(newPrice, minPrice, maxPrice);
}
```

## 7.2 StockRuntimeData

実行時の株式状態:
```csharp
public class StockRuntimeData
{
    public string stockId;
    public double currentPrice;
    public double previousPrice;
    public double openPrice;   // 始値
    public double highPrice;   // 高値
    public double lowPrice;    // 安値
    public Queue<double> priceHistory;  // チャート用履歴

    public double ChangeRate => (currentPrice - previousPrice) / previousPrice;
}
```

## 7.3 MarketEventBus

株式市場のイベント配信:
```csharp
public static class MarketEventBus
{
    public static event Action<StockPriceSnapshot> OnPriceUpdated;
    public static event Action<MarketNews> OnNewsGenerated;
    public static event Action<string, int, double> OnTradeExecuted;

    public static void PublishPriceUpdated(StockPriceSnapshot snapshot)
    {
        OnPriceUpdated?.Invoke(snapshot);
    }
}
```

---

# 8. 好感度システム詳細

## 8.1 AffectionManager

**役割:** キャラクターの好感度管理

**ファイル:** `Main_core/Systems/Affection/AffectionManager.cs`

### 好感度上昇手段
```csharp
// 1. クリック（クールダウンあり）
public void OnCharacterClicked()
{
    if (Time.time - _lastClickTime >= clickAffectionCooldown)
    {
        AddAffection(currentCharacter.characterId, clickAffectionAmount);
        _lastClickTime = Time.time;
    }
}

// 2. 頭なで（クールダウンなし）
public void OnHeadPetted(int bonus = 1)
{
    AddAffection(currentCharacter.characterId, bonus);
}

// 3. プレゼント
public void GiveGift(string itemId)
{
    // アイテム消費
    Inventory.Use(itemId, 1);

    // 好感度ボーナス取得
    int bonus = currentCharacter.GetGiftBonus(itemId);
    AddAffection(currentCharacter.characterId, bonus);
}
```

### 好感度レベル
```csharp
// CharacterData内で定義
public class AffectionLevel
{
    public int level;
    public int requiredAffection;
    public string levelName;        // "初対面", "知り合い", "友人"...
    public int bonusClickPower;     // クリック威力ボーナス
}
```

---

# 9. イベント駆動設計

## 9.1 イベント一覧

### WalletManager
```csharp
public event Action<double> OnMoneyChanged;
public event Action<double> OnCertificateChanged;
public Action<double> OnMoneyEarned;   // 統計用
public Action<double> OnMoneySpent;    // 統計用
```

### InventoryManager
```csharp
public event Action<string, int> OnItemCountChanged;
public Action<int> OnMaterialsUsed;  // 統計用
```

### UpgradeManager
```csharp
public event Action<string, int> OnUpgradeLevelChanged;
public event Action<UpgradeData, int> OnUpgradePurchased;
public Action OnUpgradeCountIncremented;
public Action<int> OnHighestLevelUpdated;
```

### SPManager
```csharp
public event Action<float> OnSPChanged;
public event Action OnFeverStarted;
public event Action OnFeverEnded;
```

### IncomeManager
```csharp
public event Action<double> OnIncomeGenerated;
```

### GachaManager
```csharp
public event Action<GachaBannerData, List<GachaResultItem>> OnGachaPulled;
public event Action<GachaBannerData> OnPityReached;
public event Action<GachaResultItem, int> OnHighRarityPulled;
```

### AffectionManager
```csharp
public event Action<string, int, int> OnAffectionChanged;
public event Action<string, AffectionLevel> OnAffectionLevelUp;
public event Action<string> OnDialogueRequested;
```

### MarketEventBus
```csharp
public static event Action<StockPriceSnapshot> OnPriceUpdated;
public static event Action<MarketNews> OnNewsGenerated;
public static event Action<string, int, double> OnTradeExecuted;
```

## 9.2 イベント購読のベストプラクティス

```csharp
public class SomeUIController : IViewController
{
    // コールバック参照を保持（解除用）
    private Action<double> _onMoneyChangedCallback;

    public void Initialize(VisualElement root)
    {
        // コールバックをフィールドに保存
        _onMoneyChangedCallback = OnMoneyChanged;

        // イベント購読
        GameController.Instance.Wallet.OnMoneyChanged += _onMoneyChangedCallback;
    }

    private void OnMoneyChanged(double amount)
    {
        // UI更新
    }

    public void Dispose()
    {
        // ★重要: 必ずイベント解除
        if (GameController.Instance?.Wallet != null)
        {
            GameController.Instance.Wallet.OnMoneyChanged -= _onMoneyChangedCallback;
        }
        _onMoneyChangedCallback = null;
    }
}
```

---

# 10. セーブ/ロードシステム

## 10.1 セーブデータ構造

各Managerがセーブ/ロード用メソッドを提供:

```csharp
// WalletManager
public void SetBalances(double money, double certificates);

// InventoryManager
public Dictionary<string, int> GetInventoryData();
public void SetInventoryData(Dictionary<string, int> data);

// UpgradeManager
public Dictionary<string, int> GetUpgradeData();
public void SetUpgradeData(Dictionary<string, int> data);

// GachaManager
public Dictionary<string, int> GetPityData();
public void SetPityData(Dictionary<string, int> data);
public Dictionary<string, Dictionary<string, int>> GetStockData();
public void SetStockData(Dictionary<string, Dictionary<string, int>> data);

// AffectionManager
public Dictionary<string, int> GetSaveData();
public void LoadSaveData(Dictionary<string, int> data);
```

## 10.2 統計データ

```csharp
[Serializable]
public class StatisticsData
{
    public int totalClicks;
    public int totalCriticalHits;
    public double highestClickDamage;
    public double totalMoneyEarned;
    public double totalMoneySpent;
    public int totalUpgradesPurchased;
    public int totalMaterialsUsed;
    public double totalPlayTimeSeconds;
}
```

---

# 11. UI Toolkit使用ガイド

## 11.1 基本操作

### 要素取得
```csharp
// 単一要素
var button = root.Q<Button>("buy-button");
var label = root.Q<Label>("money-label");
var element = root.Q<VisualElement>("container");

// 複数要素
var allButtons = root.Query<Button>().ToList();
```

### イベント登録
```csharp
// クリック
button.clicked += OnButtonClicked;

// ポインターイベント
element.RegisterCallback<PointerDownEvent>(OnPointerDown);
element.RegisterCallback<PointerUpEvent>(OnPointerUp);

// 選択変更（ListView）
listView.selectionChanged += OnSelectionChanged;
```

### スタイル変更
```csharp
// クラス追加/削除
element.AddToClassList("active");
element.RemoveFromClassList("active");
element.ToggleInClassList("highlighted");

// 直接スタイル設定
element.style.display = DisplayStyle.None;
element.style.backgroundColor = Color.red;
element.style.left = 100;
```

### スケジューラー
```csharp
// 遅延実行
root.schedule.Execute(() => {
    // 100ms後に実行
}).ExecuteLater(100);

// 繰り返し実行
root.schedule.Execute(() => {
    // 50msごとに実行
}).Every(50);

// 停止
scheduledItem.Pause();
```

## 11.2 アニメーション

USSのtransitionを活用:
```css
.element {
    transition-property: opacity, translate;
    transition-duration: 0.3s;
    transition-timing-function: ease-out;
}

.element.fade-out {
    opacity: 0;
    translate: 0 -20px;
}
```

C#側でクラス操作:
```csharp
element.AddToClassList("fade-out");
root.schedule.Execute(() => {
    element.RemoveFromHierarchy();
}).ExecuteLater(300);
```

---

# 12. 新機能追加手順

## 12.1 新しいManagerを追加

1. **クラス作成**
```csharp
public class NewManager : MonoBehaviour
{
    public static NewManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // イベント定義
    public event Action<SomeType> OnSomethingHappened;

    // 公開メソッド
    public void DoSomething() { }
}
```

2. **GameControllerに参照追加**
```csharp
public class GameController : MonoBehaviour
{
    public NewManager NewManager;

    private void FetchManagers()
    {
        NewManager ??= GetOrAddManager<NewManager>();
    }
}
```

## 12.2 新しいUI画面を追加

1. **UXMLテンプレート作成**
2. **USSスタイル作成**
3. **コントローラー作成**
```csharp
public class NewUIController : IViewController
{
    private VisualElement _root;

    public void Initialize(VisualElement root)
    {
        _root = root;
        QueryElements();
        BindEvents();
    }

    public void Dispose()
    {
        UnbindEvents();
    }
}
```

4. **MainUIControllerに追加**
```csharp
case MenuType.NewScreen:
    var newController = new NewUIController();
    newController.Initialize(ContentArea);
    currentViewController = newController;
    break;
```

## 12.3 新しいScriptableObjectを追加

1. **データクラス作成**
```csharp
[CreateAssetMenu(fileName = "New_Data", menuName = "ArknightsClicker/New Data")]
public class NewData : BaseData
{
    public int someValue;
    public float anotherValue;
}
```

2. **Databaseクラス作成（必要に応じて）**
```csharp
[CreateAssetMenu(fileName = "NewDatabase", menuName = "ArknightsClicker/New Database")]
public class NewDatabase : ScriptableObject
{
    public List<NewData> items = new();

    public NewData GetById(string id)
    {
        return items.Find(x => x.id == id);
    }
}
```

---

# 付録

## A. 定数クラス

各UIモジュールは専用の定数クラスを持つ:
```csharp
public static class ShopUIConstants
{
    public const int LIST_ITEM_HEIGHT = 80;
    public const int CURRENCY_ANIMATION_INTERVAL_MS = 30;
    public const int TYPEWRITER_INTERVAL_MS = 20;
    public const int HOLD_BUTTON_INITIAL_DELAY_MS = 300;
    public const int HOLD_BUTTON_X1_INTERVAL_MS = 100;
    public const int HOLD_BUTTON_X10_INTERVAL_MS = 50;
}
```

## B. 数値フォーマット

```csharp
private string FormatNumber(double value)
{
    if (value >= 1_000_000_000_000) return $"{value / 1_000_000_000_000:F2}T";
    if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F2}B";
    if (value >= 1_000_000) return $"{value / 1_000_000:F2}M";
    if (value >= 1_000) return $"{value / 1_000:F2}K";
    return value.ToString("N0");
}
```

## C. デバッグ用ログ

```csharp
public static class LogUIController
{
    public static void Msg(string message);       // 通常メッセージ
    public static void LogSystem(string message); // システムログ
}
```

---

*このドキュメントはプロジェクトの実装詳細を網羅しています。*
*最終更新: 2024年*
