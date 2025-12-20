# データ仕様書

> スプレッドシート作成時の参考資料
> 各データタイプのフィールド意味と選択肢の詳細

---

## 目次

1. [UpgradeData（強化データ）](#1-upgradedata強化データ) - 🔴 高優先度
2. [ItemData（アイテムデータ）](#2-itemdataアイテムデータ) - 🔴 高優先度
3. [CompanyData（企業データ）](#3-companydataアイテムデータ) - 🟡 中優先度
4. [GachaBannerData（ガチャバナー）](#4-gachabannerdataガチャバナー) - 🟡 中優先度
5. [CharacterData（キャラクター）](#5-characterdataキャラクター) - 🟢 低優先度
6. [StockData（株式データ）](#6-stockdata株式データ) - 🟢 低優先度

---

# 1. UpgradeData（強化データ）

> ショップで購入できる強化アイテム。クリック威力や自動収入を上げる。

## スプレッドシートヘッダー

```
id | displayName | description | upgradeType | category | effectValue | maxLevel | currencyType | baseCost | costMultiplier | sortOrder | effectFormat | isPercentDisplay | categoryIcon
```

---

## フィールド説明

### 基本情報（BaseDataから継承）

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `id` | string | 一意のID（英数字とアンダースコア） | `click_flat_1` |
| `displayName` | string | ゲーム内表示名 | `クリックパワー` |
| `description` | string | 説明文 | `クリック威力を強化する` |

### 強化設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `upgradeType` | Enum | 効果の種類（下記参照） | `Click_FlatAdd` |
| `category` | Enum | UIカテゴリ（下記参照） | `Click` |
| `effectValue` | double | 1レベルあたりの効果値 | `1.0` |
| `maxLevel` | int | 最大レベル（0 = 無制限） | `10` |

### コスト設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `currencyType` | Enum | 支払い通貨（下記参照） | `LMD` |
| `baseCost` | double | レベル1の購入費用 | `100` |
| `costMultiplier` | float | レベルごとの費用上昇率 | `1.15` |

### 表示設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `sortOrder` | int | ショップでの並び順（小さい順） | `0` |
| `effectFormat` | string | 効果の表示形式 | `+{0}` |
| `isPercentDisplay` | bool | %表示するか | `false` |
| `categoryIcon` | string | カテゴリアイコン | `⚔️` |

---

## 選択肢詳細

### upgradeType（強化タイプ）

| 値 | 意味 | 関係するシステム | 具体例 |
|----|------|----------------|--------|
| `Click_FlatAdd` | クリック威力に固定値を加算 | ClickManager | effectValue=1 → クリック威力+1 |
| `Click_PercentAdd` | クリック威力に%加算 | ClickManager | effectValue=0.1 → クリック威力+10% |
| `Income_FlatAdd` | 自動収入に固定値を加算 | IncomeManager | effectValue=1 → 毎秒+1 LMD |
| `Income_PercentAdd` | 自動収入に%加算 | IncomeManager | effectValue=0.1 → 自動収入+10% |
| `Critical_ChanceAdd` | クリティカル発生率を加算 | CriticalSystem | effectValue=0.01 → クリティカル率+1% |
| `Critical_PowerAdd` | クリティカル倍率を加算 | CriticalSystem | effectValue=0.1 → クリティカル倍率+0.1 |
| `SP_ChargeAdd` | SP回復速度を加算 | SkillManager | effectValue=0.05 → SP回復+5% |
| `Fever_PowerAdd` | フィーバー時の倍率を加算 | FeverManager | effectValue=0.5 → フィーバー倍率+0.5 |

### category（カテゴリ）

| 値 | 意味 | UI上の表示 | 推奨アイコン |
|----|------|-----------|-------------|
| `Click` | クリック系強化 | 「クリック」タブ | ⚔️ |
| `Income` | 自動収入系強化 | 「自動収入」タブ | 💰 |
| `Critical` | クリティカル系強化 | 「クリティカル」タブ | ⚡ |
| `Skill` | SP・フィーバー系強化 | 「スキル」タブ | 🎯 |
| `Special` | 特殊・その他 | 「特殊」タブ | ⭐ |

### currencyType（通貨タイプ）

| 値 | 意味 | 入手方法 | 用途 |
|----|------|---------|------|
| `LMD` | 龍門幣（基本通貨） | クリック、自動収入 | 強化購入、ガチャ |
| `Certificate` | 資格証 | ガチャ被り、実績 | 高級強化、限定交換 |
| `Originium` | 純正源石（将来用） | 課金、イベント | 特殊購入 |

---

## 計算式

### コスト計算
```
レベルNの購入コスト = baseCost × (costMultiplier ^ N)
```

例: baseCost=100, costMultiplier=1.15 の場合
- Lv1: 100
- Lv2: 115
- Lv3: 132
- Lv10: 404

### 効果計算
```
レベルNの累計効果 = effectValue × N
```

例: effectValue=1.0 の場合
- Lv1: +1.0
- Lv5: +5.0
- Lv10: +10.0

---

## 推奨データ例

### クリック系

| id | displayName | upgradeType | effectValue | baseCost | costMultiplier | maxLevel |
|----|------------|-------------|-------------|----------|----------------|----------|
| `click_flat_1` | クリックパワー | Click_FlatAdd | 1 | 100 | 1.15 | 0 |
| `click_flat_2` | クリックパワーII | Click_FlatAdd | 5 | 1000 | 1.20 | 0 |
| `click_percent_1` | クリック効率 | Click_PercentAdd | 0.1 | 5000 | 1.25 | 10 |
| `click_percent_2` | クリック効率II | Click_PercentAdd | 0.25 | 50000 | 1.30 | 5 |

### 自動収入系

| id | displayName | upgradeType | effectValue | baseCost | costMultiplier | maxLevel |
|----|------------|-------------|-------------|----------|----------------|----------|
| `income_flat_1` | オート生産 | Income_FlatAdd | 1 | 500 | 1.15 | 0 |
| `income_flat_2` | オート生産II | Income_FlatAdd | 10 | 10000 | 1.20 | 0 |
| `income_percent_1` | 生産効率 | Income_PercentAdd | 0.1 | 25000 | 1.25 | 10 |

### クリティカル系

| id | displayName | upgradeType | effectValue | baseCost | costMultiplier | maxLevel |
|----|------------|-------------|-------------|----------|----------------|----------|
| `crit_chance_1` | 会心率 | Critical_ChanceAdd | 0.01 | 2000 | 1.20 | 20 |
| `crit_power_1` | 会心威力 | Critical_PowerAdd | 0.1 | 5000 | 1.25 | 10 |

### スキル系

| id | displayName | upgradeType | effectValue | baseCost | costMultiplier | maxLevel |
|----|------------|-------------|-------------|----------|----------------|----------|
| `sp_charge_1` | SPチャージ | SP_ChargeAdd | 0.05 | 10000 | 1.30 | 10 |
| `fever_power_1` | フィーバーパワー | Fever_PowerAdd | 0.5 | 30000 | 1.35 | 5 |

---

# 2. ItemData（アイテムデータ）

> ガチャ排出キャラ、素材、消耗品などのアイテム定義

## スプレッドシートヘッダー

```
id | displayName | description | type | rarity | sortOrder | maxStack | sellPrice | useEffect | effectValue | effectDuration | categoryIcon
```

---

## フィールド説明

### 基本情報

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `id` | string | 一意のID | `char_amiya` |
| `displayName` | string | 表示名 | `アーミヤ` |
| `description` | string | 説明文 | `ロドスのリーダー` |
| `type` | Enum | アイテム種別（下記参照） | `KeyItem` |
| `rarity` | Enum | レアリティ（下記参照） | `Star5` |

### 数値設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `sortOrder` | int | 表示順 | `0` |
| `maxStack` | int | 最大所持数（-1 = 無制限） | `-1` |
| `sellPrice` | int | 売却価格（LMD） | `100` |

### 消耗品設定（type=Consumable時のみ）

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `useEffect` | Enum | 使用時の効果（下記参照） | `RecoverSP` |
| `effectValue` | float | 効果量 | `50` |
| `effectDuration` | float | 効果持続時間（秒） | `60` |

---

## 選択肢詳細

### type（アイテムタイプ）

| 値 | 意味 | 用途 | 例 |
|----|------|-----|-----|
| `KeyItem` | 重要アイテム | ガチャ排出キャラ、レンズ、解放キー | アーミヤ、透視レンズ |
| `Material` | 素材 | 強化素材、交換素材 | 初級チップ、モジュール |
| `Consumable` | 消耗品 | 使用して効果を得る | SPドリンク、収入ブースター |

**重要**: ガチャから排出されるキャラクターは必ず `KeyItem` にすること！

### rarity（レアリティ）

| 値 | 表示 | 色 | ガチャ排出率目安 |
|----|-----|----|--------------|
| `Star1` | ★1 | 灰 | - |
| `Star2` | ★2 | 黄緑 | - |
| `Star3` | ★3 | 青 | 40% |
| `Star4` | ★4 | 紫 | 50% |
| `Star5` | ★5 | 金 | 8% |
| `Star6` | ★6 | オレンジ | 2% |

### useEffect（消耗品効果）

| 値 | 意味 | effectValue の意味 | effectDuration の意味 |
|----|------|-------------------|---------------------|
| `None` | 効果なし | - | - |
| `RecoverSP` | SP回復 | 回復量 | - |
| `BoostIncome` | 自動収入ブースト | 倍率（1.5 = 50%アップ） | 効果時間（秒） |
| `InstantMoney` | 即座にLMD獲得 | 獲得量 | - |
| `RecoverLensBattery` | レンズバッテリー回復 | 回復量（秒） | - |

---

## 潜在システム（被りボーナス）

同じキャラ（KeyItem）を複数回入手すると「潜在」が上がり、効果がアップ：

```
所持数1 = 潜在1 = 基本性能
所持数2 = 潜在2 = +20% ボーナス
所持数3 = 潜在3 = +40% ボーナス
所持数4 = 潜在4 = +60% ボーナス
...（上限なし）
```

計算式: `ボーナス = (所持数 - 1) × 0.2`

---

## 推奨データ例

### ガチャ排出キャラ（KeyItem）

| id | displayName | type | rarity | description |
|----|------------|------|--------|-------------|
| `char_amiya` | アーミヤ | KeyItem | Star5 | ロドス・アイランドのリーダー |
| `char_silverash` | シルバーアッシュ | KeyItem | Star6 | カランド貿易のCEO |
| `char_exusiai` | エクシア | KeyItem | Star6 | ペンギン急便のエース |
| `char_texas` | テキサス | KeyItem | Star5 | ペンギン急便の配達員 |
| `char_kroos` | クルース | KeyItem | Star3 | ロドスの狙撃手 |
| `char_melantha` | メランサ | KeyItem | Star3 | ロドスの近衛兵 |

### 素材（Material）

| id | displayName | type | rarity | sellPrice | description |
|----|------------|------|--------|-----------|-------------|
| `mat_chip_1` | 初級チップ | Material | Star1 | 10 | 基本強化素材 |
| `mat_chip_2` | 中級チップ | Material | Star3 | 50 | 中級強化素材 |
| `mat_chip_3` | 上級チップ | Material | Star5 | 200 | 上級強化素材 |
| `mat_device` | デバイス | Material | Star2 | 30 | スキル強化用素材 |
| `mat_module` | モジュール | Material | Star4 | 100 | 特殊強化用素材 |

### 消耗品（Consumable）

| id | displayName | type | rarity | useEffect | effectValue | effectDuration |
|----|------------|------|--------|-----------|-------------|----------------|
| `item_sp_drink` | SPドリンク | Consumable | Star2 | RecoverSP | 50 | 0 |
| `item_income_boost` | 収入ブースター | Consumable | Star3 | BoostIncome | 1.5 | 60 |
| `item_instant_lmd` | LMDパック | Consumable | Star2 | InstantMoney | 1000 | 0 |
| `item_lens_battery` | レンズバッテリー | Consumable | Star3 | RecoverLensBattery | 30 | 0 |

### プレゼント（Consumable）

| id | displayName | type | rarity | sellPrice | description |
|----|------------|------|--------|-----------|-------------|
| `gift_snack` | お菓子 | Consumable | Star1 | 5 | 好感度+5 |
| `gift_cake` | ケーキ | Consumable | Star3 | 20 | 好感度+15 |
| `gift_rare` | レアアイテム | Consumable | Star5 | 100 | 好感度+50 |

---

# 3. CompanyData（企業データ）

> ゲーム内企業の定義。株式システムと連動。

## スプレッドシートヘッダー

```
id | displayName | description | traitType | traitMultiplier | baseStockPrice | volatility | trendBias | sector | totalShares | dividendRate | dividendIntervalSeconds | isPlayerCompany
```

---

## フィールド説明

### 基本情報

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `id` | string | 企業ID | `rhodes_island` |
| `displayName` | string | 企業名 | `ロドス・アイランド製薬` |
| `description` | string | 企業説明 | `感染者治療を専門とする製薬会社` |

### 企業特性

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `traitType` | Enum | 特性タイプ（下記参照） | `Arts` |
| `traitMultiplier` | float | ボーナス効果量（1.1 = 10%アップ） | `1.1` |

### 株価設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `baseStockPrice` | float | 初期株価 | `100` |
| `volatility` | float | 変動しやすさ（0.1〜3.0） | `1.0` |
| `trendBias` | float | 長期トレンド（-1〜1） | `0` |
| `sector` | Enum | 業種セクター（下記参照） | `Tech` |
| `totalShares` | long | 発行済み株式数 | `1000000` |

### 配当設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `dividendRate` | float | 配当率（0.02 = 2%） | `0.02` |
| `dividendIntervalSeconds` | int | 配当間隔（秒）、0で配当なし | `3600` |

### 自社株設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `isPlayerCompany` | bool | プレイヤーの会社か | `true` |

---

## 選択肢詳細

### traitType（企業特性）

| 値 | 意味 | ボーナス対象 | 該当企業例 |
|----|------|------------|-----------|
| `None` | なし | - | 龍門近衛局 |
| `TechInnovation` | 技術革新 | スキル・SP回復速度 | ライン生命 |
| `Logistics` | 物流強化 | クリック効率・生産速度 | ペンギン急便 |
| `Military` | 武力介入 | 攻撃力・クリティカル率 | ブラックスチール |
| `Trading` | 貿易特化 | 所持金上限・売却額 | カランド貿易 |
| `Arts` | アーツ学 | 特殊リソース生成量 | ロドス |

### sector（業種セクター）

| 値 | 意味 | 特徴 | 該当企業例 |
|----|------|-----|-----------|
| `Tech` | テクノロジー | イノベーションイベントで変動 | ライン生命、ロドス |
| `Military` | 軍事・傭兵 | 戦争イベントで急騰 | ブラックスチール |
| `Logistics` | 物流・運送 | 景気連動が激しい | ペンギン急便 |
| `Finance` | 金融・貿易 | 安定だが低成長 | カランド貿易、龍門 |
| `Entertainment` | エンタメ・サービス | 季節イベントで変動 | シエスタ |
| `Resource` | 資源・エネルギー | 長期安定成長 | ウルサス |

---

## 推奨データ例

| id | displayName | traitType | sector | baseStockPrice | volatility | trendBias |
|----|------------|-----------|--------|----------------|------------|-----------|
| `rhodes_island` | ロドス・アイランド製薬 | Arts | Tech | 100 | 1.0 | 0 |
| `rhine_lab` | ライン生命 | TechInnovation | Tech | 150 | 1.5 | 0.05 |
| `penguin_logistics` | ペンギン急便 | Logistics | Logistics | 80 | 1.2 | 0.02 |
| `blacksteel` | ブラックスチール | Military | Military | 120 | 1.8 | 0 |
| `karlan_trade` | カランド貿易 | Trading | Finance | 200 | 0.8 | 0.03 |
| `lungmen` | 龍門近衛局 | None | Finance | 180 | 0.5 | 0.01 |

---

## 保有ボーナス（別シート推奨）

| companyId | requiredHoldingRate | bonusType | effectValue | description |
|-----------|---------------------|-----------|-------------|-------------|
| `rhodes_island` | 0.10 | UpgradeCostReduction | 0.05 | 強化費用5%軽減 |
| `rhodes_island` | 0.30 | ClickEfficiency | 0.10 | クリック効率10%アップ |
| `rhodes_island` | 0.51 | GachaRateUp | 0.05 | ガチャ確率5%アップ |

### bonusType（ボーナス種別）

| 値 | 意味 |
|----|------|
| `UpgradeCostReduction` | 強化費用軽減 |
| `ClickEfficiency` | クリック効率アップ |
| `AutoIncomeBoost` | 自動収入アップ |
| `GachaRateUp` | ガチャ確率アップ（該当企業キャラ） |
| `DividendBonus` | 配当金ボーナス |
| `ExpBonus` | 経験値ボーナス |
| `CriticalRate` | クリティカル率アップ |
| `SellPriceBonus` | 売却価格アップ |
| `TransactionFeeReduction` | 取引手数料軽減 |

---

# 4. GachaBannerData（ガチャバナー）

> ガチャの種類とアイテム排出設定

## スプレッドシートヘッダー（バナー本体）

```
bannerId | bannerName | description | currencyType | costSingle | costTen | hasPity | pityCount | startsLocked | requiredUnlockItem | isLimited
```

## スプレッドシートヘッダー（排出テーブル・別シート）

```
bannerId | itemId | weight | isPickup | stockCount
```

---

## フィールド説明

### バナー設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `bannerId` | string | バナーID | `standard_banner` |
| `bannerName` | string | バナー名 | `スタンダードガチャ` |
| `description` | string | 説明文 | `恒常オペレーターが排出されます` |
| `currencyType` | string | 使用通貨 | `Certificate` |
| `costSingle` | int | 単発コスト | `600` |
| `costTen` | int | 10連コスト | `6000` |

### 天井設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `hasPity` | bool | 天井あり | `true` |
| `pityCount` | int | 天井までの回数 | `50` |

### 解放設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `startsLocked` | bool | 最初はロック | `false` |
| `requiredUnlockItem` | string | 解放に必要なアイテムID | `key_limited_access` |
| `isLimited` | bool | 期間限定か | `false` |

### 排出テーブル

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `itemId` | string | 排出アイテムID | `char_silverash` |
| `weight` | float | 排出重み（相対値） | `0.4` |
| `isPickup` | bool | ピックアップ対象か | `true` |
| `stockCount` | int | 封入数（0 = 無制限） | `1` |

---

## 封入数システム（ボックスガチャ）

`stockCount` を設定すると、その数だけ排出されたら在庫切れ：

| stockCount | 意味 | 使い方 |
|------------|------|--------|
| `0` | 無制限 | 通常ガチャ |
| `1` | 1個のみ | 確定入手ボックス |
| `5` | 5個まで | 素材ボックス |

---

## 推奨データ例

### バナー

| bannerId | bannerName | currencyType | costSingle | costTen | hasPity | pityCount | startsLocked |
|----------|-----------|--------------|------------|---------|---------|-----------|--------------|
| `beginner_banner` | 初心者ピックアップ | Certificate | 300 | 2800 | true | 30 | false |
| `standard_banner` | スタンダードガチャ | Certificate | 600 | 6000 | true | 50 | false |
| `limited_banner_01` | 限定ピックアップ | Certificate | 600 | 6000 | true | 50 | true |

### 排出テーブル（standard_banner）

| bannerId | itemId | weight | isPickup | stockCount |
|----------|--------|--------|----------|------------|
| `standard_banner` | `char_silverash` | 0.4 | false | 0 |
| `standard_banner` | `char_exusiai` | 0.4 | false | 0 |
| `standard_banner` | `char_texas` | 2.0 | false | 0 |
| `standard_banner` | `char_lappland` | 2.0 | false | 0 |
| `standard_banner` | `char_vigna` | 12.5 | false | 0 |
| `standard_banner` | `char_melantha` | 20.0 | false | 0 |

---

# 5. CharacterData（キャラクター）

> キャラクターの好感度・演出設定（低優先度）

## スプレッドシートヘッダー

```
id | displayName | description | rarity | affiliatedCompanyId | sortOrder
```

---

## フィールド説明

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `id` | string | キャラID | `amiya` |
| `displayName` | string | 表示名 | `アーミヤ` |
| `description` | string | キャラ説明 | `ロドスのリーダー。兎の獣人。` |
| `rarity` | Enum | レアリティ | `Star5` |
| `affiliatedCompanyId` | string | 所属企業ID | `rhodes_island` |
| `sortOrder` | int | 表示順 | `1` |

---

## 好感度レベル（別シート推奨）

| characterId | level | requiredAffection | levelName | bonusClickPower |
|-------------|-------|-------------------|-----------|-----------------|
| `amiya` | 0 | 0 | 初対面 | 0 |
| `amiya` | 1 | 50 | 知り合い | 5 |
| `amiya` | 2 | 100 | 友人 | 10 |
| `amiya` | 3 | 150 | 親友 | 20 |
| `amiya` | 4 | 200 | 最高の信頼 | 50 |

---

# 6. StockData（株式データ）

> 株式市場の銘柄定義（低優先度）

## スプレッドシートヘッダー

```
stockId | companyName | description | initialPrice | minPrice | maxPrice | volatility | drift | jumpProbability | jumpIntensity | trait | transactionFee | totalShares | dividendRate | dividendIntervalSeconds | sortOrder | chartColor | themeColor
```

---

## フィールド説明

### 基本情報

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `stockId` | string | 銘柄コード | `RL` |
| `companyName` | string | 企業名 | `ライン生命` |
| `description` | string | 説明 | `医療テクノロジー企業` |

### 株価設定

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `initialPrice` | double | 初期株価 | `1000` |
| `minPrice` | double | 最低株価 | `10` |
| `maxPrice` | double | 最高株価（0 = 無制限） | `0` |

### 変動特性

| フィールド | 型 | 説明 | 範囲 | 例 |
|-----------|---|------|------|-----|
| `volatility` | float | ボラティリティ | 0.01〜0.5 | `0.1` |
| `drift` | float | ドリフト（長期トレンド） | -0.1〜0.2 | `0.02` |
| `jumpProbability` | float | 急騰/暴落確率 | 0〜0.1 | `0.01` |
| `jumpIntensity` | float | 急騰/暴落強度 | 0.1〜0.5 | `0.2` |

### 企業特性

| フィールド | 型 | 説明 | 例 |
|-----------|---|------|-----|
| `trait` | Enum | 特性（下記参照） | `Innovation` |
| `transactionFee` | float | 取引手数料率 | `0.01` |

---

## 選択肢詳細

### trait（銘柄特性）

| 値 | 意味 | 特徴 |
|----|------|-----|
| `General` | 一般 | 特殊効果なし |
| `Military` | 軍事 | 戦争イベントで急騰 |
| `Innovation` | 革新 | ボラティリティ高い |
| `Logistics` | 物流 | 景気敏感 |
| `Trading` | 貿易 | 手数料安い、安定 |
| `Medical` | 医療 | 源石関連イベントで変動 |
| `Energy` | エネルギー | 長期安定成長 |

---

## 推奨データ例

| stockId | companyName | initialPrice | volatility | drift | trait | transactionFee |
|---------|------------|--------------|------------|-------|-------|----------------|
| `RI` | ロドス・アイランド | 100 | 0.15 | 0 | Medical | 0.01 |
| `RL` | ライン生命 | 150 | 0.20 | 0.03 | Innovation | 0.01 |
| `PL` | ペンギン急便 | 80 | 0.12 | 0.02 | Logistics | 0.005 |
| `BSW` | ブラックスチール | 120 | 0.25 | 0 | Military | 0.02 |
| `KT` | カランド貿易 | 200 | 0.08 | 0.03 | Trading | 0.005 |

---

# 付録: 共通カラー定義

## レアリティカラー

| レアリティ | RGB | Hex |
|-----------|-----|-----|
| Star1 | (230, 230, 230) | `#E6E6E6` |
| Star2 | (191, 217, 51) | `#BFD933` |
| Star3 | (0, 166, 242) | `#00A6F2` |
| Star4 | (153, 102, 217) | `#9966D9` |
| Star5 | (255, 217, 51) | `#FFD933` |
| Star6 | (255, 128, 0) | `#FF8000` |

## カテゴリカラー

| カテゴリ | RGB | Hex |
|---------|-----|-----|
| Click | (255, 153, 51) | `#FF9933` |
| Income | (51, 204, 102) | `#33CC66` |
| Critical | (255, 77, 77) | `#FF4D4D` |
| Skill | (102, 153, 255) | `#6699FF` |
| Special | (204, 128, 255) | `#CC80FF` |

---

# 株式周回システム（プレステージ）

> 株を100%買い占めると「買収完了」→ リセットして永続ボーナス獲得

## 基本フロー

```
保有率100%到達
    ↓
「買収完了！」イベント発生
    ↓
永続ボーナス獲得（周回数に応じて増加）
    ↓
株がリセット（保有率0%に戻る）
    ↓
totalShares増加（次周回は買い占め難易度UP）
    ↓
また0から買い始める
```

## 周回ごとの変化

| 周回 | totalShares | 永続ボーナス（累計） | 難易度 |
|------|-------------|---------------------|--------|
| 1周目 | 1,000,000 | - | 基準 |
| 2周目 | 1,500,000 | クリック効率+5% | 1.5倍 |
| 3周目 | 2,250,000 | クリック効率+10% | 2.25倍 |
| 4周目 | 3,375,000 | クリック効率+15% | 3.375倍 |

※ totalShares倍率は調整可能（1.5倍、2倍など）

## 永続ボーナス例

| 企業 | ボーナス内容 |
|------|-------------|
| ロドス | クリック効率+5%/周 |
| ライン生命 | SP回復速度+3%/周 |
| ペンギン急便 | 自動収入+5%/周 |
| ブラックスチール | クリティカル率+2%/周 |
| カランド貿易 | 売却価格+5%/周 |

## 実装メモ

- `StockPrestigeData` のような新クラスが必要かも
- セーブデータに周回数を保存
- totalShares は `baseShares × (倍率 ^ 周回数)` で計算

---

# チェックリスト

## 最低限必要なデータ数

- [ ] **UpgradeData** × 10種類以上
  - [ ] クリック系 × 3
  - [ ] 収入系 × 3
  - [ ] クリティカル系 × 2
  - [ ] スキル系 × 2

- [ ] **ItemData（素材・消耗品）** × 10種類以上
  - [ ] 素材 (Material) × 5
  - [ ] プレゼント (Consumable) × 3
  - [ ] 変換トークン (Material) × 2

- [ ] **ItemData（キャラ = KeyItem）** × 15種類以上
  - [ ] ★6 × 3〜5
  - [ ] ★5 × 4〜6
  - [ ] ★4 × 4〜6
  - [ ] ★3 × 4〜6

- [ ] **CompanyData** × 5種類以上

- [ ] **GachaBannerData** × 2種類以上
  - [ ] 初心者バナー
  - [ ] 通常バナー

---

*最終更新: 2025年*
