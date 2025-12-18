# Operator画面 技術仕様書

## 概要

オペレーター（キャラクター）表示画面の実装仕様。
PSBキャラクターの表示、レンズアイテム使用、ふれあい機能を提供。

---

## アーキテクチャ

```
┌─────────────────────────────────────────────────────────────┐
│                    OperatorUIController                      │
│                  (IViewController / 非MonoBehaviour)         │
│  - UI Toolkit制御                                            │
│  - レンズアイテム/プレゼント管理                              │
│  - 好感度UI更新                                              │
└─────────────────────┬───────────────────────────────────────┘
                      │ SetDisplayArea / SetUpdateCallback
                      ▼
┌─────────────────────────────────────────────────────────────┐
│               OverlayCharacterPresenter                      │
│                    (MonoBehaviour Singleton)                 │
│  - キャラクタープレハブ生成/管理                              │
│  - 表示/非表示制御                                           │
│  - Updateコールバック配信                                    │
└─────────────────────┬───────────────────────────────────────┘
                      │ Instantiate / FitToVisualElement
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              CharacterCanvasController                       │
│                    (MonoBehaviour)                           │
│  - Canvas自動サイズ調整                                      │
│  - PSB Bounds計算                                            │
│  - 画面サイズ変更対応                                        │
└─────────────────────┬───────────────────────────────────────┘
                      │ 子オブジェクト
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              CharacterInteractionZone                        │
│                    (MonoBehaviour)                           │
│  - タッチ/クリック検出                                       │
│  - コンボ判定                                                │
│  - 好感度ボーナス計算                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## クラス詳細

### 1. OperatorUIController

**役割**: オペレーター画面のUI制御（非MonoBehaviour）

**ファイル**: `Script/OperatorUIController.cs`

#### パブリックAPI

| メソッド | 説明 |
|---------|------|
| `Initialize(VisualElement)` | 初期化、UI要素のセットアップ |
| `Dispose()` | 破棄、イベント購読解除 |
| `UpdateBattery(float)` | バッテリー消費（deltaTime渡し） |
| `UseLens()` | レンズ使用開始 |

#### イベント

| イベント | 説明 |
|---------|------|
| `OnBackRequested` | 戻るボタン押下時 |

#### 内部状態

```csharp
private ItemData currentLensItem;      // 装備中のレンズ
private float currentBatteryTime;       // 残りバッテリー時間
private float maxBatteryTime;           // 最大バッテリー時間
private bool isLensActive;              // レンズ有効フラグ
private int currentOutfit;              // 現在の衣装 (0-2)
private int currentLensMode;            // 透視レベル (0=通常)
```

#### 依存関係

- `OverlayCharacterPresenter.Instance` - キャラ表示
- `AffectionManager.Instance` - 好感度管理
- `InventoryManager.Instance` - アイテム所持確認

---

### 2. OverlayCharacterPresenter

**役割**: キャラクタープレハブの生成と管理（Singleton）

**ファイル**: `Script/OverlayCharacterPresenter.cs`

#### Inspector設定

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `characterPrefab` | GameObject | CharacterCanvasController付きプレハブ |
| `sortingOrder` | int | 描画順（デフォルト: 100） |

#### パブリックAPI

| メソッド | 説明 |
|---------|------|
| `SetDisplayArea(VisualElement)` | 表示エリア設定 |
| `EnsureCreated()` | 互換性用（現在は空） |
| `Show()` | 表示 |
| `Show(GameObject)` | 指定プレハブで表示 |
| `Hide()` | 非表示 |
| `DestroyCharacter()` | インスタンス破棄 |
| `SetUpdateCallback(Action<float>)` | 毎フレームコールバック設定 |
| `ClearUpdateCallback()` | コールバック解除 |
| `GetInteractionZones()` | インタラクションゾーン取得 |
| `RefreshLayout()` | レイアウト再計算 |
| `SetSortingOrder(int)` | 描画順変更 |

#### プロパティ

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `Instance` | static | シングルトンインスタンス |
| `CurrentController` | CharacterCanvasController | 現在のコントローラー |
| `CurrentInstance` | GameObject | 現在のインスタンス |
| `IsShowing` | bool | 表示中かどうか |

#### ライフサイクル

```
Awake() → Instance設定
Show() → プレハブ生成 → イベント購読 → UpdateDisplay()
Update() → _onUpdateCallback?.Invoke(deltaTime)
Hide() → SetActive(false)
DestroyCharacter() → イベント解除 → Destroy
OnDestroy() → Instance = null
```

---

### 3. CharacterCanvasController

**役割**: Canvas自動サイズ調整、PSBフィッティング

**ファイル**: `Script/CharacterCanvasController.cs`

#### Inspector設定

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `canvas` | Canvas | 対象Canvas |
| `canvasScaler` | CanvasScaler | スケーラー |
| `contentRoot` | RectTransform | PSB配置親 |
| `psbRoot` | GameObject | PSBルートオブジェクト |
| `autoDetectBounds` | bool | Bounds自動検出 |
| `padding` | Vector2 | 内側余白 |
| `minScale` / `maxScale` | float | スケール制限 |
| `referenceResolution` | Vector2 | 基準解像度 |

#### パブリックAPI

| メソッド | 説明 |
|---------|------|
| `SetDisplayArea(Rect, int)` | スクリーン座標で表示エリア設定 |
| `FitToVisualElement(VisualElement, int)` | UI Toolkit要素にフィット |
| `FitToScreen(int)` | 画面全体にフィット |
| `UpdateLayout()` | レイアウト再計算 |
| `SetPSB(GameObject)` | PSB差し替え |
| `Show()` / `Hide()` | 表示制御 |
| `SetSortingOrder(int)` | 描画順変更 |

#### イベント

| イベント | 説明 |
|---------|------|
| `OnReady` | 初期化完了時 |
| `OnScaleChanged(float)` | スケール変更時 |
| `OnClicked` | クリック時 |
| `OnBoundsUpdated(Bounds)` | Bounds更新時 |

#### 座標変換

```
UI Toolkit (左上原点) → Screen座標 (左下原点) → Canvas座標
```

```csharp
// UITK → Screen
var screenRect = new Rect(
    worldBound.x,
    Screen.height - worldBound.y - worldBound.height,
    worldBound.width,
    worldBound.height
);
```

#### スケール計算

```csharp
// PSBサイズ（100px = 1unit）
float psbWidth = bounds.size.x * 100f;
float psbHeight = bounds.size.y * 100f;

// アスペクト比維持でフィット
float scale = Min(targetWidth / psbWidth, targetHeight / psbHeight);
```

---

### 4. CharacterInteractionZone

**役割**: タッチ検出、コンボ判定、好感度ボーナス

**ファイル**: `Script/CharacterInteractionZone.cs`

#### ゾーンタイプ

```csharp
public enum ZoneType
{
    Head,       // 頭（なでなで）
    Body,       // 体（タッチ）
    Hand,       // 手（握手）
    Special     // 特殊
}
```

#### Inspector設定

| フィールド | 型 | 説明 |
|-----------|-----|------|
| `zoneType` | ZoneType | ゾーン種別 |
| `zoneName` | string | 表示名 |
| `comboTimeout` | float | コンボ継続時間（秒） |
| `baseAffectionBonus` | int | 基本好感度ボーナス |
| `comboMultiplier` | float | コンボ倍率 |
| `touchParticle` | ParticleSystem | タッチエフェクト |
| `touchSound` | AudioClip | タッチ効果音 |
| `reactions` | string[] | リアクションセリフ |

#### パブリックAPI

| メソッド | 説明 |
|---------|------|
| `HandleTouch()` | タッチ処理（外部呼び出し可） |
| `ResetCombo()` | コンボリセット |

#### イベント

| イベント | 説明 |
|---------|------|
| `OnZoneTouched(ZoneType, int)` | タッチ時（ゾーン, コンボ数） |
| `OnTouch` | UnityEvent（Inspector用） |
| `OnComboTouch(int)` | UnityEvent（コンボ数付き） |

#### コンボ計算

```csharp
// ボーナス = 基本値 + (コンボ数 - 1) × 倍率
float bonus = baseAffectionBonus + (comboCount - 1) * comboMultiplier;
```

#### 必要コンポーネント

- `Collider2D` - タッチ検出に必須（BoxCollider2D推奨）

---

## プレハブ構成

```
CharacterPrefab (GameObject)
├── CharacterCanvasController (Component)
├── Canvas (Component)
├── CanvasScaler (Component)
│
└── ContentRoot (RectTransform)
    └── PSBRoot (GameObject)
        ├── SpriteRenderer (複数)
        │
        └── InteractionZones (GameObject)
            ├── HeadZone (CharacterInteractionZone + BoxCollider2D)
            ├── BodyZone (CharacterInteractionZone + BoxCollider2D)
            └── HandZone (CharacterInteractionZone + BoxCollider2D)
```

---

## シーケンス図

### 初期化フロー

```
OperatorUIController.Initialize()
    │
    ├─► SetupReferences()
    ├─► SetupCallbacks()
    ├─► SetupLensItemsUI()
    ├─► SetupGiftItemsUI()
    │
    └─► ShowCharacterOverlay()
            │
            ├─► presenter.SetDisplayArea(root)
            ├─► presenter.EnsureCreated()
            ├─► presenter.Show()
            │       │
            │       └─► Instantiate(prefab)
            │           controller.FitToVisualElement()
            │
            ├─► presenter.SetUpdateCallback(UpdateBattery)
            │
            └─► SubscribeToInteractionZones()
                    └─► zone.OnZoneTouched += handler
```

### タッチフロー

```
User Touch
    │
    ▼
CharacterInteractionZone.OnMouseDown()
    │
    ├─► comboCount++
    ├─► AffectionManager.OnHeadPetted(bonus)
    ├─► PlayEffects()
    ├─► ShowReaction()
    │
    └─► OnZoneTouched?.Invoke(type, combo)
            │
            ▼
    OperatorUIController.OnInteractionZoneTouched()
            │
            └─► UpdateAffectionUI()
```

---

## 既知の制限事項

1. **OnMouseDown依存**: タッチ検出は`Collider2D`が必須
2. **座標変換**: UI ToolkitとuGUIの座標系が異なる
3. **PSB単位**: 100ピクセル = 1ユニット想定
4. **シングルキャラ**: 同時に1キャラのみ表示可能

---

## 今後の拡張ポイント

- [ ] 複数キャラ同時表示
- [ ] 着せ替えプレハブ切り替え
- [ ] 透視レイヤー実装
- [ ] アニメーション連携
- [ ] タッチ位置に応じたリアクション変化
