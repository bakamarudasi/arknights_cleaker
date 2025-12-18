# アークナイツクリッカー アイテム・企業データ仕様書
> Gemini/AI向けアイデア出し用ドキュメント

## 概要
このゲームは「アークナイツ」世界観のクリッカーゲームです。
プレイヤーはクリックでLMD（ゲーム内通貨）を稼ぎ、アップグレードや株取引で資産を増やします。

---

## 1. アイテムシステム（ItemData）

### アイテムタイプ
```csharp
enum ItemType {
    KeyItem,    // 重要アイテム（施設解放キー、株解放キーなど）
    Material,   // 素材（アップグレードに使用）
    Consumable  // 消耗品（バッテリー回復、一時ブースト）
}
```

### レアリティ
```
Star1 (白) → Star2 (緑) → Star3 (青) → Star4 (紫) → Star5 (金) → Star6 (オレンジ)
```

### 消耗品効果タイプ
```csharp
enum ConsumableType {
    None,              // 効果なし
    RecoverSP,         // SP回復
    BoostIncome,       // 一時的に収入ブースト
    InstantMoney,      // 即座にLMD獲得
    RecoverLensBattery // レンズバッテリー回復（R18要素用）
}
```

### 特殊機能：レンズスペック
R18要素の透視レンズ用。AI提案不要。
```csharp
class LensSpecs {
    bool isLens;           // レンズかどうか
    float viewRadius;      // 視野範囲
    float maxDuration;     // バッテリー持続時間
    int penetrateLevel;    // 透視深度（1〜5）
    LensFilterMode filter; // 視覚効果
}
```

### アイテム作成ルール
- `id`: 一意のID（例: `key_stock_rhine`, `mat_chip_blue`）
- `displayName`: 日本語表示名
- `description`: フレーバーテキスト（アークナイツ世界観に合わせる）
- `icon`: アイコン画像（後で設定）
- `sellPrice`: 売却価格（0なら売却不可）
- `maxStack`: 最大スタック数（-1で無制限）

---

## 2. アップグレードシステム（UpgradeData）

### 強化タイプ
```csharp
enum UpgradeType {
    Click_FlatAdd,      // クリックダメージ +X
    Click_PercentAdd,   // クリックダメージ +X%
    Income_FlatAdd,     // 自動収入/秒 +X
    Income_PercentAdd,  // 自動収入 +X%
    Critical_ChanceAdd, // クリティカル率 +X%
    Critical_PowerAdd,  // クリティカル倍率 +X
    SP_ChargeAdd,       // SPチャージ速度 +X%
    Fever_PowerAdd      // フィーバー倍率 +X
}
```

### カテゴリ
| カテゴリ | アイコン | 色 |
|---------|---------|-----|
| Click | ⚔️ | オレンジ |
| Income | 💰 | 緑 |
| Critical | ⚡ | 赤 |
| Skill | 🎯 | 青 |
| Special | ⭐ | 紫 |

### 通貨タイプ
```csharp
enum CurrencyType {
    LMD,          // 龍門幣（メイン通貨）
    Certificate,  // 資格証（プレミアム通貨）
    Originium     // 純正源石（課金通貨、将来用）
}
```

### アップグレード作成ルール
```yaml
基本設定:
  - id: 一意のID（例: upgrade_click_power）
  - displayName: 日本語名
  - description: 効果説明
  - upgradeType: 効果タイプ
  - category: UIカテゴリ
  - effectValue: 1レベルあたりの効果値
  - maxLevel: 最大レベル（0で無制限）

コスト設定:
  - currencyType: 使用通貨
  - baseCost: 初期コスト
  - costMultiplier: レベルごとの倍率（通常1.15）
  - requiredMaterials: 必要素材リスト

解放条件:
  - requiredUnlockItem: 必要キーアイテム
  - prerequisiteUpgrade: 前提アップグレード
  - prerequisiteLevel: 前提レベル

表示設定:
  - sortOrder: 並び順
  - effectFormat: 表示形式（例: "クリック +{0}"）
  - isPercentDisplay: %表示するか
  - categoryIcon: カテゴリ絵文字
  - isSpecial: 特別マーク表示
```

---

## 3. 株式システム（StockData）

### 企業特性（StockTrait）
```csharp
enum StockTrait {
    General,    // 一般（特殊効果なし）
    Military,   // 軍事（戦争イベントで急騰）
    Innovation, // 革新（ボラティリティ高い）
    Logistics,  // 物流（景気敏感）
    Trading,    // 貿易（手数料安い、安定）
    Medical,    // 医療（源石関連で変動）
    Energy      // エネルギー（長期安定成長）
}
```

### 株価パラメータ
| パラメータ | 説明 | 範囲 |
|-----------|------|------|
| initialPrice | 初期株価 | 100〜100000 |
| minPrice | 最低株価 | 10〜 |
| maxPrice | 最高株価（0=無制限） | 0〜 |
| volatility | 変動の激しさ | 0.01〜0.5 |
| drift | 長期トレンド | -0.1〜0.2 |
| jumpProbability | 急騰/暴落確率 | 0〜0.1 |
| jumpIntensity | 急騰/暴落強度 | 0.1〜0.5 |
| transactionFee | 取引手数料 | 0〜0.05 |

### 企業データ作成ルール
```yaml
基本情報:
  - stockId: 銘柄コード（2-4文字英字、例: RL, PL, BSW）
  - companyName: 企業名（日本語）
  - description: 企業説明（アークナイツ設定準拠）
  - logo: 企業ロゴ

株価設定:
  - 安定株: volatility低、drift正、jump低
  - ハイリスク株: volatility高、drift高め、jump高
  - ディフェンシブ株: volatility低、drift低、jump極低

解放条件:
  - unlockKeyItem: ガチャから出るキーアイテム
  - sortOrder: 表示順

表示設定:
  - chartColor: チャート線の色
  - themeColor: 企業テーマカラー

株式発行・配当:
  - totalShares: 発行済み株式数（保有率計算用、デフォルト100万）
  - dividendRate: 配当率（0.02 = 2%）
  - dividendIntervalSeconds: 配当間隔（秒、0で配当なし）
```

### 株式保有ボーナス（HoldingBonus）
各企業の株を一定以上保有するとボーナスが発動します。

```csharp
enum HoldingBonusType {
    UpgradeCostReduction,   // 強化費用軽減
    ClickEfficiency,        // クリック効率アップ
    AutoIncomeBoost,        // 自動収入アップ
    GachaRateUp,            // ガチャ確率アップ（この企業のキャラ）
    DividendBonus,          // 配当金ボーナス
    ExpBonus,               // 経験値ボーナス
    CriticalRate,           // クリティカル率アップ
    SellPriceBonus,         // 売却価格アップ
    TransactionFeeReduction // 取引手数料軽減
}
```

### 保有ボーナス設定例
```yaml
holdingBonuses:
  - requiredHoldingRate: 0.05  # 5%保有で発動
    bonusType: UpgradeCostReduction
    effectValue: 0.05          # 5%コスト軽減
    description: "ライン生命の株主優待：強化費用5%オフ"

  - requiredHoldingRate: 0.10  # 10%保有で発動
    bonusType: GachaRateUp
    effectValue: 0.02          # ガチャ確率2%アップ
    description: "大株主特典：ライン生命オペレーター排出率UP"

  - requiredHoldingRate: 0.30  # 30%保有で発動
    bonusType: DividendBonus
    effectValue: 0.50          # 配当50%ボーナス
    description: "筆頭株主特権：配当金1.5倍"
```

### アップグレードとの連携
アップグレードに`relatedStock`を設定すると、その株の保有率に応じて効果が増加します。

```yaml
株式連動設定:
  - relatedStock: ライン生命（StockData参照）
  - scaleWithHolding: true     # 保有率で効果スケール
  - maxHoldingMultiplier: 2.0  # 100%保有時に効果2倍
```

---

## 4. アークナイツ世界観の企業一覧（参考）

### 既存の主要組織
| 組織名 | 説明 | 適した特性 |
|--------|------|------------|
| ロドス | 感染者救援製薬会社 | Medical |
| ライン生命 | 製薬・医療大手 | Medical |
| ペンギン急便 | 物流会社 | Logistics |
| ブラックスチール | PMC（民間軍事会社） | Military |
| レユニオン | 感染者テロ組織（悪役） | - |
| 龍門 | 炎国の商業都市 | Trading |
| シラクーザ | マフィア国家 | Trading |
| カランド | 貿易・傭兵 | Trading/Military |
| ケルシー商会 | 謎の商会 | Innovation |
| クロージャ商店 | ロドス内商店 | General |
| サンクタ企業群 | ラテラーノの企業 | Energy |

### 企業設定のアイデア出し依頼例
```
以下のフォーマットで新しい企業を5つ提案してください：

企業名: [名前]
銘柄コード: [2-4文字]
特性: [General/Military/Innovation/Logistics/Trading/Medical/Energy]
volatility: [0.01-0.5]
drift: [-0.1-0.2]
設定説明: [アークナイツ世界観に合った100字程度の説明]
```

---

## 5. キーアイテム連携

### 株解放キーアイテム
```yaml
命名規則: key_stock_[企業名の略称]
例:
  - key_stock_rhine     → ライン生命株を解放
  - key_stock_penguin   → ペンギン急便株を解放
  - key_stock_blacksteel → ブラックスチール株を解放

入手方法:
  - ガチャ（副産物として低確率）
  - 特定キャラの好感度報酬
  - イベント報酬
```

### 施設解放キーアイテム
```yaml
命名規則: key_facility_[施設名]
例:
  - key_facility_market  → マーケット画面解放
  - key_facility_gacha   → ガチャ解放
  - key_facility_dorm    → 寮解放

入手方法:
  - メインストーリー進行
  - 初回購入ボーナス
```

---

## 6. AI向け依頼テンプレート

### 新アイテム提案依頼
```
アークナイツクリッカーの新アイテムを提案してください。

タイプ: [KeyItem/Material/Consumable]
レアリティ目安: [Star1-6]
用途: [株解放/アップグレード素材/一時ブースト]

以下のフォーマットで5つ提案：
- id: [英語スネークケース]
- displayName: [日本語名]
- description: [フレーバーテキスト]
- 効果/用途: [具体的な使い道]
```

### 新アップグレード提案依頼
```
アークナイツクリッカーの新アップグレードを提案してください。

カテゴリ: [Click/Income/Critical/Skill/Special]
コスト通貨: [LMD/Certificate]

以下のフォーマットで3つ提案：
- id: [英語スネークケース]
- displayName: [日本語名]
- upgradeType: [効果タイプ]
- effectValue: [1レベルの効果値]
- maxLevel: [最大レベル]
- baseCost: [初期コスト]
- description: [効果説明]
```

### 新企業提案依頼
```
アークナイツ世界観に合った架空企業を提案してください。

以下のフォーマットで提案：
- stockId: [2-4文字英字]
- companyName: [日本語企業名]
- trait: [General/Military/Innovation/Logistics/Trading/Medical/Energy]
- initialPrice: [初期株価]
- volatility: [0.01-0.5]
- drift: [-0.1-0.2]
- description: [世界観に合った企業説明100字]
- unlockKeyItem名: [key_stock_xxx]
```

---

## 7. 既存データとの整合性

### 禁止事項
- 既存IDと重複するIDの使用
- ゲームバランスを著しく崩すパラメータ
- アークナイツ世界観から逸脱した設定

### 推奨事項
- 既存キャラクター・組織との関連性を持たせる
- フレーバーテキストに世界観要素を含める
- 適切なレアリティ・コストバランス

---

*このドキュメントは企画・アイデア出し用です。実装時はUnityのScriptableObjectとして作成してください。*
