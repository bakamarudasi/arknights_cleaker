# アークナイツ風クリッカーゲーム アーキテクチャガイド

> このファイルはAI（Claude/Gemini等）がプロジェクトを理解するためのリファレンスです

## プロジェクト概要

アークナイツ風のクリッカー/放置ゲーム。Unity 2022+ / UI Toolkit使用。

## ディレクトリ構造

```
Assets/Scripts/
├── Main_core/                    # コアシステム
│   ├── Core/
│   │   ├── GameController.cs     # ゲーム全体の統括（シングルトン）
│   │   ├── WalletManager.cs      # 通貨管理
│   │   ├── UpgradeManager.cs     # 強化システム
│   │   └── IncomeManager.cs      # 自動収入
│   ├── Data/
│   │   ├── Definitions/          # ScriptableObject定義
│   │   │   ├── UpgradeData.cs    # 強化データ
│   │   │   ├── ItemData.cs       # アイテムデータ
│   │   │   ├── CompanyData.cs    # 企業データ（株式市場）
│   │   │   └── BaseData.cs       # 共通基底クラス
│   │   ├── Character/
│   │   │   └── CharacterData.cs  # キャラクターデータ
│   │   └── Save/
│   │       └── ProgressData.cs   # セーブデータ
│   └── Systems/
│       ├── Market/               # 株式市場システム
│       ├── Tutorial/             # チュートリアル
│       └── Affection/            # 好感度システム
│
├── Main_UI/                      # UI関連
│   ├── Home_/Script/             # ホーム画面
│   │   ├── HomeUIController.cs
│   │   └── ClickAreaHandler.cs
│   ├── Shop_/Script/             # ショップ画面
│   │   ├── ShopUIController.cs      # ファサード
│   │   ├── ShopTabController.cs     # タブ管理
│   │   ├── ShopDetailPanelController.cs  # 詳細パネル
│   │   ├── ShopAnimationHelper.cs   # アニメーション
│   │   └── ShopService.cs           # ビジネスロジック
│   ├── Gacha_/Script/            # ガチャ画面
│   │   ├── GachaUIController.cs     # ファサード
│   │   ├── GachaBannerController.cs # バナー管理
│   │   ├── GachaVisualEffectController.cs  # 演出
│   │   ├── GachaResultAnimator.cs   # 結果表示
│   │   └── GachaManager.cs          # ビジネスロジック
│   ├── Market_/Script/           # 株式市場画面
│   │   ├── MarketUIController.cs    # ファサード
│   │   ├── MarketChartController.cs
│   │   ├── MarketTradeController.cs
│   │   └── MarketSkillController.cs
│   └── Operator_/Script/         # オペレーター画面
│       ├── OperatorUIController.cs  # ファサード
│       ├── OperatorLensController.cs
│       ├── OperatorGiftController.cs
│       └── OperatorAffectionController.cs
```

## アーキテクチャパターン

### 1. ファサードパターン（UIコントローラー）

大きなUIコントローラーは「ファサード」として機能し、サブコントローラーに責任を委譲：

```csharp
// ファサード例
public class ShopUIController : IViewController
{
    private ShopTabController tabController;        // タブ管理
    private ShopDetailPanelController detailPanel;  // 詳細表示
    private ShopAnimationHelper animationHelper;    // アニメーション
    private ShopService shopService;                // ビジネスロジック
}
```

### 2. サービス層（ビジネスロジック分離）

UI層とビジネスロジックを分離：

```csharp
// ShopService - 購入処理のビジネスロジック
public class ShopService
{
    public event Action<UpgradeData, int> OnPurchaseSuccess;

    public void ExecuteBulkPurchase(UpgradeData upgrade, int count) { ... }
    public double GetSingleCost(UpgradeData upgrade) { ... }
}
```

### 3. イベント駆動

マネージャー間の通信はイベントで行う：

```csharp
// WalletManager
public event Action<double> OnMoneyChanged;

// UIController側
wallet.OnMoneyChanged += UpdateMoneyDisplay;
```

### 4. ScriptableObjectベースのデータ

ゲームデータはScriptableObjectで定義：

```csharp
[CreateAssetMenu(fileName = "New_Upgrade", menuName = "ArknightsClicker/Upgrade Data")]
public class UpgradeData : BaseData { ... }
```

## 主要クラスの責任

| クラス | 責任 |
|--------|------|
| `GameController` | マネージャーの統括、ゲームループ |
| `WalletManager` | 通貨（LMD/資格証）の管理 |
| `UpgradeManager` | 強化の購入・レベル管理 |
| `IncomeManager` | 自動収入の計算・付与 |
| `GachaManager` | ガチャの抽選・天井管理 |
| `MarketManager` | 株価更新・取引処理 |
| `InventoryManager` | アイテム所持数管理 |
| `AffectionManager` | 好感度システム |

## 通貨システム

```csharp
public enum CurrencyType
{
    LMD,          // 龍門幣（メイン通貨）
    Certificate,  // 資格証（プレミアム通貨）
    Originium     // 純正源石（将来用）
}
```

## UI Toolkit使用ルール

1. **QueryElements**: `root.Q<T>("element-name")` でUI要素取得
2. **イベント**: `RegisterCallback<ClickEvent>` でイベント登録
3. **スタイル**: USS + `AddToClassList()` / `RemoveFromClassList()`
4. **スケジューラー**: `root.schedule.Execute().ExecuteLater()` でタイマー

## 定数管理

各UIモジュールは専用の定数クラスを持つ：

```csharp
public static class ShopUIConstants
{
    public const int CURRENCY_ANIMATION_INTERVAL_MS = 30;
    public const int TYPEWRITER_INTERVAL_MS = 20;
    // ...
}
```

## 新機能追加時の指針

### UIコントローラー追加

1. `IViewController` インターフェースを実装
2. 責任が大きくなったらサブコントローラーに分割
3. ビジネスロジックはサービスクラスに分離

### データ定義追加

1. `BaseData` を継承したScriptableObjectを作成
2. `CreateAssetMenu` 属性でメニュー追加
3. エディタでアセット作成可能に

### イベント追加

1. マネージャーに `public event Action<T>` を定義
2. 適切なタイミングで `?.Invoke()` で発火
3. リスナー側は `Dispose()` で必ず解除

## 参考ファイル

- `/Docs/ItemSystemSpec_ForAI.md` - アイテムシステム仕様
- `/Assets/Scripts/Main_UI/Operator_/SPECIFICATION.md` - オペレーター画面仕様
