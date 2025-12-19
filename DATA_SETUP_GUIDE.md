# データ設定ガイド 📦

> ゲームに必要なScriptableObjectデータの作成ガイド
> Geminiなど他のAIと一緒にデータを考える際の参考資料

---

## 📊 必要なデータ一覧

| データタイプ | 作成場所 | 優先度 | 状態 |
|-------------|---------|--------|------|
| UpgradeData | `Resources/Data/Upgrades/` | 🔴 高 | 要作成 |
| ItemData | `Resources/Data/Items/` | 🔴 高 | 要作成 |
| CompanyData | `Resources/Data/Companies/` | 🟡 中 | 要作成 |
| GachaBannerData | `Resources/Data/Gacha/` | 🟡 中 | 要作成 |
| CharacterData | `Resources/Data/Characters/` | 🟢 低 | 要作成 |
| StockData | `Resources/Data/Market/` | 🟢 低 | 要作成 |

---

## 🔧 UpgradeData（強化データ）

### 作成方法
`Project > Create > ArknightsClicker > Upgrade Data`

### フィールド説明

```yaml
# 基本情報（BaseDataから継承）
id: "click_power_1"          # 一意ID
displayName: "クリックパワー"   # 表示名
description: "クリック威力を強化" # 説明
icon: Sprite                 # アイコン画像

# 強化タイプ
upgradeType: Click_FlatAdd   # 効果の種類
category: Click              # UIカテゴリ
effectValue: 1.0             # 1レベルあたりの効果値
maxLevel: 10                 # 最大レベル（0=無制限）

# コスト
currencyType: LMD            # 通貨タイプ
baseCost: 100                # 初期コスト
costMultiplier: 1.15         # レベル毎のコスト上昇率
```

### 推奨する強化リスト

#### クリック系（Click）
| ID | 名前 | 効果 | 基本コスト | 倍率 |
|----|------|------|-----------|------|
| `click_flat_1` | クリックパワー | クリック威力+1 | 100 | 1.15 |
| `click_flat_2` | クリックパワーII | クリック威力+5 | 1,000 | 1.20 |
| `click_percent_1` | クリック効率 | クリック威力+10% | 5,000 | 1.25 |
| `click_percent_2` | クリック効率II | クリック威力+25% | 50,000 | 1.30 |

#### 自動収入系（Income）
| ID | 名前 | 効果 | 基本コスト | 倍率 |
|----|------|------|-----------|------|
| `income_flat_1` | オート生産 | 自動収入+1/秒 | 500 | 1.15 |
| `income_flat_2` | オート生産II | 自動収入+10/秒 | 10,000 | 1.20 |
| `income_percent_1` | 生産効率 | 自動収入+10% | 25,000 | 1.25 |

#### クリティカル系（Critical）
| ID | 名前 | 効果 | 基本コスト | 倍率 |
|----|------|------|-----------|------|
| `crit_chance_1` | 会心率 | クリティカル率+1% | 2,000 | 1.20 |
| `crit_power_1` | 会心威力 | クリティカル倍率+0.1 | 5,000 | 1.25 |

#### スキル系（Skill）
| ID | 名前 | 効果 | 基本コスト | 倍率 |
|----|------|------|-----------|------|
| `sp_charge_1` | SPチャージ | SP回復速度+5% | 10,000 | 1.30 |
| `fever_power_1` | フィーバーパワー | フィーバー倍率+0.5 | 30,000 | 1.35 |

---

## 📦 ItemData（アイテムデータ）

### 作成方法
`Project > Create > ArknightsClicker > Item Data`

### アイテムタイプ

```csharp
public enum ItemType
{
    KeyItem,     // 🎯 重要アイテム（ガチャ排出キャラ、レンズ本体など）
    Material,    // 素材（強化素材）
    Consumable   // 消耗品（バッテリー回復、プレゼント等）
}
```

### ⚠️ 重要：ガチャ排出アイテムは KeyItem

**ガチャから排出されるアイテム（キャラクター/オペレーター）は必ず `KeyItem` タイプにすること！**

```yaml
# ガチャ排出キャラの例
id: "char_silverash"
displayName: "シルバーアッシュ"
type: KeyItem              # ← 必ずKeyItem！
rarity: Star6              # ★6
description: "カランド貿易のCEO"
```

### 潜在システム（被り = 強化）

同じキャラを複数回入手すると「潜在」が上がり、効果がアップする：

```
所持数1 = 潜在1 = 基本性能
所持数2 = 潜在2 = +20% ボーナス
所持数3 = 潜在3 = +40% ボーナス
所持数4 = 潜在4 = +60% ボーナス
...（上限なし）
```

**実装ポイント:**
- `InventoryManager.GetCount(itemId)` = 潜在レベル
- ボーナス計算: `(所持数 - 1) * 0.2f`
- 課金要素なし（ゲーム内通貨のみ）
- ShopViewで「所持: 3個 (+40%)」と表示

### レアリティ

```csharp
public enum Rarity
{
    Star1,  // ★1 - コモン
    Star2,  // ★2 - アンコモン
    Star3,  // ★3 - レア
    Star4,  // ★4 - スーパーレア
    Star5,  // ★5 - SSR
    Star6   // ★6 - UR
}
```

### 推奨アイテムリスト

#### 素材アイテム
| ID | 名前 | レア | 用途 |
|----|------|-----|------|
| `mat_chip_1` | 初級チップ | ★1 | 基本強化素材 |
| `mat_chip_2` | 中級チップ | ★3 | 中級強化素材 |
| `mat_chip_3` | 上級チップ | ★5 | 上級強化素材 |
| `mat_device` | デバイス | ★2 | スキル強化用 |
| `mat_module` | モジュール | ★4 | 特殊強化用 |

#### プレゼントアイテム
| ID | 名前 | レア | 好感度上昇 |
|----|------|-----|-----------|
| `gift_snack` | お菓子 | ★1 | +5 |
| `gift_cake` | ケーキ | ★3 | +15 |
| `gift_rare` | レアアイテム | ★5 | +50 |

#### レンズアイテム（特殊）
```yaml
id: "lens_basic"
displayName: "基本レンズ"
type: KeyItem              # レンズもKeyItem
lensSpecs:
  isLens: true
  penetrateLevel: 1
  maxDuration: 30.0  # 秒
  filterMode: Normal
```

---

## 🎭 ガチャ排出キャラクター（KeyItem）

### 推奨キャラクターリスト

ガチャから排出されるキャラは全て `ItemData` の `KeyItem` タイプで作成！

#### ★6 キャラクター
| ID | 名前 | 所属企業 | 備考 |
|----|------|---------|------|
| `char_silverash` | シルバーアッシュ | カランド貿易 | 潜在2で+20% |
| `char_exusiai` | エクシア | ペンギン急便 | 潜在2で+20% |
| `char_eyjafjalla` | エイヤフィヤトラ | ライン生命 | 潜在2で+20% |
| `char_saria` | サリア | ライン生命 | 潜在2で+20% |
| `char_chen` | チェン | 龍門近衛局 | 潜在2で+20% |

#### ★5 キャラクター
| ID | 名前 | 所属企業 | 備考 |
|----|------|---------|------|
| `char_lappland` | ラップランド | シエスタ | 潜在2で+20% |
| `char_texas` | テキサス | ペンギン急便 | 潜在2で+20% |
| `char_specter` | スペクター | 深海教会 | 潜在2で+20% |
| `char_ptilopsis` | プラチナ | ライン生命 | 潜在2で+20% |

#### ★4 キャラクター
| ID | 名前 | 所属企業 | 備考 |
|----|------|---------|------|
| `char_vigna` | ヴィグナ | フリー | 潜在2で+20% |
| `char_shirayuki` | シラユキ | ロドス | 潜在2で+20% |
| `char_cuora` | クオーラ | フリー | 潜在2で+20% |
| `char_gitano` | ギターノ | フリー | 潜在2で+20% |

#### ★3 キャラクター
| ID | 名前 | 所属企業 | 備考 |
|----|------|---------|------|
| `char_melantha` | メランサ | フリー | 潜在2で+20% |
| `char_kroos` | クルース | ロドス | 潜在2で+20% |
| `char_beagle` | ビーグル | ロドス | 潜在2で+20% |
| `char_hibiscus` | ハイビスカス | ロドス | 潜在2で+20% |

### キャラデータのテンプレート

```yaml
id: "char_amiya"
displayName: "アーミヤ"
type: KeyItem
rarity: Star5
description: "ロドス・アイランドのリーダー。兎の獣人。"
icon: # Spriteをセット

# 表示設定
sortOrder: 1
categoryIcon: "🐰"
isSpecial: true  # 主人公なので特別扱い

# ※ 被り = 潜在アップ（所持数で自動計算、設定不要）
# ※ 所持数2 = 潜在2 = +20%ボーナス
```

---

## 🏢 CompanyData（企業データ）

### 作成方法
`Project > Create > ArknightsClicker > Company Data`

### 企業特性タイプ

```csharp
public enum CompanyTrait
{
    None,           // なし
    TechInnovation, // 技術革新（スキル強化）
    Logistics,      // 物流強化（クリック効率）
    Military,       // 武力介入（クリティカル）
    Trading,        // 貿易特化（所持金上限）
    Arts            // アーツ学（特殊リソース）
}
```

### 推奨企業リスト

| ID | 名前 | セクター | 特性 | 基本株価 |
|----|------|---------|------|---------|
| `rhodes_island` | ロドス・アイランド製薬 | Tech | Arts | 100 |
| `rhine_lab` | ライン生命 | Tech | TechInnovation | 150 |
| `penguin_logistics` | ペンギン急便 | Logistics | Logistics | 80 |
| `blacksteel` | ブラックスチール | Military | Military | 120 |
| `karlan_trade` | カランド貿易 | Finance | Trading | 200 |
| `lungmen` | 龍門近衛局 | Finance | None | 180 |

### 株式保有ボーナス例

```yaml
# ロドス株 10%保有で強化費用5%オフ
holdingBonuses:
  - requiredHoldingRate: 0.10
    bonusType: UpgradeCostReduction
    effectValue: 0.05
    description: "強化費用5%軽減"

  - requiredHoldingRate: 0.30
    bonusType: ClickEfficiency
    effectValue: 0.10
    description: "クリック効率10%アップ"

  - requiredHoldingRate: 0.51
    bonusType: GachaRateUp
    effectValue: 0.05
    description: "ガチャ確率5%アップ"
```

---

## 🎰 GachaBannerData（ガチャバナー）

### 作成方法
`Project > Create > Game > Gacha > Banner Data`

### 推奨バナー構成

#### 1. 初心者バナー
```yaml
bannerId: "beginner_banner"
bannerName: "初心者ピックアップ"
description: "始めたばかりのドクターにおすすめ！"
currencyType: Certificate
costSingle: 300
costTen: 2800
hasPity: true
pityCount: 30
startsLocked: false
```

#### 2. 通常バナー
```yaml
bannerId: "standard_banner"
bannerName: "スタンダードガチャ"
description: "恒常オペレーターが排出されます"
currencyType: Certificate
costSingle: 600
costTen: 6000
hasPity: true
pityCount: 50
startsLocked: false
```

#### 3. 限定バナー
```yaml
bannerId: "limited_banner_01"
bannerName: "限定ピックアップ"
description: "期間限定！特別なオペレーター"
currencyType: Certificate
costSingle: 600
costTen: 6000
hasPity: true
pityCount: 50
isLimited: true
startsLocked: true
requiredUnlockItem: "key_limited_access"
```

### 排出テーブル例

```yaml
pool:
  # ★6 (2%)
  - item: "char_silverash"
    weight: 0.4
    isPickup: true
  - item: "char_exusiai"
    weight: 0.4
    isPickup: false

  # ★5 (8%)
  - item: "char_lappland"
    weight: 2.0
  - item: "char_texas"
    weight: 2.0

  # ★4 (50%)
  - item: "char_vigna"
    weight: 12.5

  # ★3 (40%)
  - item: "char_melantha"
    weight: 20.0
```

### 封入数システム（ボックスガチャ）

各アイテムに「封入数」を設定すると、その数だけ排出されたら在庫切れになる：

```yaml
# 初心者ボックスガチャ（10アイテム限定）
pool:
  - item: "char_silverash"
    weight: 1.0
    stockCount: 1      # ← 1個だけ封入

  - item: "char_texas"
    weight: 1.0
    stockCount: 1

  - item: "char_vigna"
    weight: 1.0
    stockCount: 1

  - item: "mat_chip_1"
    weight: 1.0
    stockCount: 3      # ← 素材は3個封入

  - item: "gift_cake"
    weight: 1.0
    stockCount: 4      # ← プレゼントは4個封入
```

**ポイント:**
- `stockCount: 0` = 無制限（通常ガチャ）
- `stockCount: 1` = 1個だけ（確定入手ボックス）
- 全アイテムが在庫切れ → バナー完売
- 在庫データはセーブ/ロード対応済み

**使用例:**
| バナータイプ | stockCount設定 |
|-------------|---------------|
| 通常ガチャ | 全て0（無制限） |
| 初心者ボックス | 全て1（各1個ずつ） |
| 素材ボックス | 素材=5、レア=1 |

---

## 👤 CharacterData（キャラクターデータ）

### 好感度レベル例

```yaml
affectionLevels:
  - level: 0
    requiredAffection: 0
    levelName: "初対面"
    bonusClickPower: 0

  - level: 1
    requiredAffection: 50
    levelName: "知り合い"
    bonusClickPower: 5

  - level: 2
    requiredAffection: 100
    levelName: "友人"
    bonusClickPower: 10

  - level: 3
    requiredAffection: 150
    levelName: "親友"
    bonusClickPower: 20

  - level: 4
    requiredAffection: 200
    levelName: "最高の信頼"
    bonusClickPower: 50
```

---

## 🎯 データ作成のコツ

### 1. バランス調整の基準

```
クリック1回の価値 = 1 LMD（初期）
自動収入 = 0.1 LMD/秒（初期）
強化1レベル = 10-30分のプレイ時間
```

### 2. コスト倍率の目安

| ゲーム段階 | 推奨倍率 |
|-----------|---------|
| 序盤 | 1.10 - 1.15 |
| 中盤 | 1.15 - 1.25 |
| 終盤 | 1.25 - 1.50 |

### 3. インフレ対策

- 通貨表示は `{value:N0}` でカンマ区切り
- 大きな数値は略記（1K, 1M, 1B）
- 複数の通貨を使い分ける

---

## ✅ チェックリスト

### 最低限必要なデータ

- [ ] **UpgradeData** x 10種類以上
  - [ ] クリック系 x 3
  - [ ] 収入系 x 3
  - [ ] クリティカル系 x 2
  - [ ] スキル系 x 2

- [ ] **ItemData（素材・消耗品）** x 10種類以上
  - [ ] 素材アイテム (Material) x 5
  - [ ] プレゼント (Consumable) x 3
  - [ ] 変換トークン (Material) x 2（黄色・緑色資格証）

- [ ] **ItemData（ガチャ排出キャラ = KeyItem）** x 15種類以上
  - [ ] ★6 キャラ x 3〜5
  - [ ] ★5 キャラ x 4〜6
  - [ ] ★4 キャラ x 4〜6
  - [ ] ★3 キャラ x 4〜6
  - [ ] ※ 被り = 潜在システム（自動計算、追加設定不要）

- [ ] **CompanyData** x 5種類以上

- [ ] **GachaBannerData** x 2種類以上
  - [ ] 初心者バナー
  - [ ] 通常バナー
  - [ ] バナーの `pool` に上記キャラをセット

---

## 📝 Geminiへの質問例

```
以下の条件でクリッカーゲームの強化データを10個考えてください：

- ゲームはアークナイツ風のクリッカー
- 通貨は「龍門幣(LMD)」
- 強化カテゴリ: Click, Income, Critical, Skill, Special
- 序盤から終盤までの進行を考慮
- コスト倍率は1.15〜1.30の範囲

出力形式:
| ID | 名前 | カテゴリ | 効果タイプ | 効果値 | 基本コスト | 倍率 | 最大Lv |
```

```
アークナイツの企業をモチーフにした株式市場の企業データを5つ考えてください：

- 各企業には特性(TechInnovation, Logistics, Military, Trading, Arts)を設定
- 初期株価は50〜200の範囲
- ボラティリティ（変動しやすさ）は0.5〜2.0の範囲
- 保有ボーナスを1-2個設定

出力形式は上のCompanyDataの形式で。
```

```
アークナイツのキャラクターをモチーフにしたガチャ排出アイテムを20個考えてください：

【重要ルール】
- 全てのキャラは ItemData の type: KeyItem として作成
- 被り = 潜在アップ（所持数で自動計算、追加設定不要）
- 潜在2（2体目入手）で+20%ボーナス、上限なし

【レアリティ配分】
- ★6 x 3キャラ
- ★5 x 5キャラ
- ★4 x 6キャラ
- ★3 x 6キャラ

【出力形式】
| ID | 名前 | レア | 所属企業 | 説明 | 特殊能力（あれば） |
```
