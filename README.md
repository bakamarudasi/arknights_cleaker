# Arknights Cleaker

> アークナイツ風クリッカー/放置ゲーム

**Unity 2022+** | **C#** | **UI Toolkit**

---

## 概要

「アークナイツ」の世界観をベースにしたクリッカー/放置ゲーム。
プレイヤーは龍門幣（LMD）を稼ぎ、オペレーターを集め、株式市場で財を成す。

## ゲームの流れ

```
クリック → LMD獲得 → アップグレード購入 → クリック力UP → ループ
    │
    ├─→ ガチャ → キャラ入手 → 好感度UP → ボーナス獲得
    │
    └─→ 株購入 → 配当金 → 企業買収 → 永続ボーナス
```

---

## 主要システム

| システム | 説明 |
|---------|------|
| **クリック** | 画面タップでLMD獲得。クリティカル判定あり |
| **自動収入** | 毎秒自動でLMD獲得 |
| **アップグレード** | LMDを消費してクリック力・自動収入を強化 |
| **ガチャ** | オペレーター（キャラクター）を入手 |
| **好感度** | オペレーターにプレゼントして好感度UP → ボーナス獲得 |
| **株式市場** | 企業株を売買。配当金で不労所得。100%取得で買収 |
| **SP/フィーバー** | SPを貯めてフィーバー発動。一定時間倍率UP |
| **スロット** | クリック時に低確率で発動。大量ボーナス |

---

## 通貨

| 通貨 | 用途 | 入手方法 |
|------|------|---------|
| **龍門幣 (LMD)** | メイン通貨。強化・ガチャに使用 | クリック、自動収入、配当 |
| **資格証** | プレミアム通貨。限定交換 | ガチャ被り、実績報酬 |
| **純正源石** | 課金通貨（将来実装） | 課金、イベント |

---

## 技術スタック

| カテゴリ | 技術 |
|---------|------|
| エンジン | Unity 2022+ |
| 言語 | C# |
| UI | UI Toolkit (UXML + USS) |
| データ管理 | ScriptableObject |
| アニメーション | Unity Animator / Spine |

---

## ディレクトリ構成

```
Assets/
├── Scripts/
│   ├── Main_core/              # コアシステム
│   │   ├── Core/               # GameController, WalletManager等
│   │   ├── Data/               # ScriptableObject定義
│   │   │   ├── Definitions/    # UpgradeData, ItemData, CompanyData
│   │   │   ├── Character/      # CharacterData
│   │   │   └── Save/           # ProgressData
│   │   └── Systems/            # 各種システム
│   │       ├── Economy/        # WalletManager
│   │       ├── Inventory/      # InventoryManager
│   │       ├── Upgrade/        # UpgradeManager
│   │       ├── Click/          # ClickManager
│   │       ├── Income/         # IncomeManager
│   │       ├── SP/             # SPManager
│   │       ├── Market/         # 株式市場
│   │       ├── Affection/      # 好感度
│   │       └── Audio/          # AudioManager
│   │
│   └── Main_UI/                # UI画面
│       ├── Home_/              # ホーム（クリックエリア）
│       ├── Shop_/              # ショップ（強化購入）
│       ├── Gacha_/             # ガチャ
│       ├── Market_/            # 株式市場
│       ├── Operator_/          # オペレーター（好感度）
│       ├── Conversation_/      # 会話システム
│       └── Common/             # 共通UI部品
│
├── Items/                      # ScriptableObjectデータ
│   ├── Click_items/            # クリック系強化
│   ├── Autopooint/             # 自動収入系強化
│   ├── GachaItem_DB/           # ガチャアイテム
│   └── company_item/           # 企業データ
│
├── UI Toolkit/                 # UXML/USS ファイル
├── Scenes/                     # シーン
└── Resources/                  # 動的ロードリソース
```

---

## アーキテクチャ

### ファサードパターン

大きなUIコントローラーはサブコントローラーに責任を委譲：

```csharp
ShopUIController (ファサード)
├── ShopTabController        // タブ管理
├── ShopDetailPanelController // 詳細表示
├── ShopAnimationHelper      // アニメーション
└── ShopService              // ビジネスロジック
```

### イベント駆動

マネージャー間の通信はイベントで行う：

```csharp
WalletManager.OnMoneyChanged += (amount) => { ... };
UpgradeManager.OnUpgradePurchased += (data, level) => { ... };
SPManager.OnFeverStarted += () => { ... };
```

### シングルトン

各マネージャーは `Instance` でアクセス：

```csharp
GameController.Instance
WalletManager.Instance
UpgradeManager.Instance
```

---

## ゲームバランス

### クリック計算

```
最終ダメージ = (基本値 + 固定ボーナス) × (1 + %ボーナス)
             × クリティカル倍率（判定時のみ）
             × フィーバー倍率（発動時のみ）
```

### 強化コスト

```
レベルNの購入コスト = baseCost × (costMultiplier ^ N)

例: baseCost=100, costMultiplier=1.15
  Lv1:  100
  Lv5:  175
  Lv10: 404
  Lv50: 10,837
```

### ガチャ確率

| レアリティ | 確率 | 被りボーナス（資格証） |
|-----------|------|----------------------|
| ★6 | 2% | 25 |
| ★5 | 8% | 10 |
| ★4 | 50% | 5 |
| ★3 | 40% | 1 |

- 天井: 50連で★6確定

### 好感度レベル

| Lv | 必要値 | 名称 | ボーナス |
|----|-------|------|---------|
| 0 | 0 | 初対面 | なし |
| 1 | 50 | 知り合い | クリック+5 |
| 2 | 100 | 友人 | クリック+10 |
| 3 | 150 | 親友 | クリック+20 |
| 4 | 200 | 最高の信頼 | クリック+50 |

### 株式市場

- **株価計算**: 幾何ブラウン運動 + ジャンプ過程
- **配当**: 保有株数に応じて毎秒配当
- **買収**: 発行株数の100%取得で買収完了 → 永続ボーナス

---

## セーブデータ

```csharp
ProgressData
├── upgrades[]           // 強化レベル
├── inventory[]          // アイテム在庫
├── statistics           // 統計データ
│   ├── totalClicks
│   ├── totalMoneyEarned
│   └── totalPlayTime
├── triggeredEventIds[]  // 発動済みイベント
└── unlockedMenus[]      // 解放済みメニュー
```

---

## 実行方法

### 必要環境

- Unity 2022.3 以降

### 手順

1. Unity Hub でプロジェクトを開く
2. `Assets/Scenes/SampleScene.unity` をロード
3. 再生ボタンをクリック（または `Ctrl+P` / `Cmd+P`）

### ビルド

```
File > Build Settings
├── Scenes/SampleScene.unity をシーン0に追加
├── Platform を選択（PC: Standalone）
└── Build
```

---

## ドキュメント

| ファイル | 内容 |
|---------|------|
| [GAME_SPECIFICATION.md](./GAME_SPECIFICATION.md) | ゲーム仕様詳細 |
| [ARCHITECTURE.md](./ARCHITECTURE.md) | アーキテクチャガイド |
| [IMPLEMENTATION_GUIDE.md](./IMPLEMENTATION_GUIDE.md) | 実装ガイド |
| [DATA_SPEC_SHEET.md](./DATA_SPEC_SHEET.md) | データ仕様 |
| [DATA_SETUP_GUIDE.md](./DATA_SETUP_GUIDE.md) | データ設定ガイド |

---

## 画面一覧

| 画面 | 説明 |
|------|------|
| **ホーム** | メイン画面。クリックエリア、所持金表示、SPゲージ |
| **ショップ** | 強化購入。タブ切替、詳細パネル、一括購入 |
| **ガチャ** | オペレーター入手。バナー表示、演出、結果表示 |
| **オペレーター** | キャラ詳細。好感度、プレゼント、会話 |
| **マーケット** | 株式市場。チャート、売買、スキル |
| **ログ** | ゲームログ表示 |

---

## ライセンス

本プロジェクトは個人開発のファンゲームです。
「アークナイツ」は Hypergryph / Yostar の登録商標です。
