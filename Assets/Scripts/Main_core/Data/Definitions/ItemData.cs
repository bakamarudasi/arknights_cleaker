using UnityEngine;

/// <summary>
/// 施設開放キー、素材、消耗品などのアイテムデータ。
/// レンズの視覚効果（フィルター）とSE設定を追加した最終版。
/// </summary>
[CreateAssetMenu(fileName = "New_Item", menuName = "ArknightsClicker/Item Data")]
public class ItemData : BaseData
{
    public enum ItemType
    {
        KeyItem,       // 重要アイテム（レンズ本体など）
        Material,      // 素材
        Consumable,    // 消耗品（バッテリー回復など）
        CostumeUnlock  // 衣装解放アイテム
    }

    public enum Rarity
    {
        Star1, Star2, Star3, Star4, Star5, Star6
    }

    public enum ConsumableType
    {
        None, RecoverSP, BoostIncome, InstantMoney,
        RecoverLensBattery
    }

    // ■ 追加: レンズを通した時の視覚効果
    public enum LensFilterMode
    {
        Normal,      // 0: そのまま見える
        NightVision, // 1: 緑色っぽく（暗視）
        Thermo,      // 2: 赤青っぽく（熱感知）
        XRay,        // 3: 白黒反転（レントゲン）
        Mosaic       // 4: 逆にモザイクがかかる（故障中とか）
    }

    [Header("基本設定")]
    public ItemType type;
    public Rarity rarity;
    public int sortOrder = 0;
    public int maxStack = -1;
    public int sellPrice = 0;

    [Header("演出設定")]
    [Tooltip("アイテムを使用した時/装備した時の効果音")]
    public AudioClip useSound;

    [Header("レンズスペック (透視ガジェット用)")]
    public LensSpecs lensSpecs;

    [Header("消耗品設定 (Consumable用)")]
    public ConsumableType useEffect;
    public float effectValue;
    public float effectDuration;

    [Header("ガチャ被り設定")]
    public ItemData convertToItem;
    public int convertAmount = 1;

    [Header("衣装解放設定 (CostumeUnlock用)")]
    [Tooltip("解放対象のキャラクターID")]
    public string targetCharacterId;

    [Tooltip("解放する衣装インデックス (1=skin1, 2=skin2)")]
    [Range(1, 3)]
    public int targetCostumeIndex = 1;

    [System.Serializable]
    public class LensSpecs
    {
        [Tooltip("このアイテムは透視レンズとして使えるか")]
        public bool isLens = false;

        [Tooltip("レンズの広さ・視野半径 (px単位、またはScale倍率)")]
        public float viewRadius = 100f;

        [Tooltip("稼働時間・バッテリー容量 (秒)。0なら無限")]
        public float maxDuration = 30f;

        [Tooltip("透視深度 (1:上着まで, 2:インナーまで, 3:すべて... など)")]
        [Range(0, 5)]
        public int penetrateLevel = 1;

        // ■ 追加: 視覚フィルター設定
        [Tooltip("レンズを通した時の色味・エフェクト")]
        public LensFilterMode filterMode = LensFilterMode.Normal;

        [Tooltip("レンズの形状マスク (丸、双眼鏡、ひび割れなど)。空欄ならデフォルトの円")]
        public Sprite lensMask;

        [Tooltip("手ブレ補正などの追加効果倍率 (将来用)")]
        public float stability = 1.0f;
    }

    [Header("表示設定")]

    [Tooltip("効果の表示フォーマット（例: 'クリック +{0}'）")]
    public string effectFormat = "+{0}";

    [Tooltip("パーセント表示するか")]
    public bool isPercentDisplay = false;

    // ↓↓↓ この2行を追加 ↓↓↓
    [Tooltip("カテゴリアイコン（絵文字など: ⚔️=Click, 💰=Income, ⚡=Critical, 🎯=Skill, ⭐=Special）")]
    public string categoryIcon = "⚔️";

    [Tooltip("特別なアップグレードとしてマーク（STARバッジ表示）")]
    public bool isSpecial = false;

    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case Rarity.Star1: return new Color(0.9f, 0.9f, 0.9f);
            case Rarity.Star2: return new Color(0.75f, 0.85f, 0.2f);
            case Rarity.Star3: return new Color(0.0f, 0.65f, 0.95f);
            case Rarity.Star4: return new Color(0.6f, 0.4f, 0.85f);
            case Rarity.Star5: return new Color(1.0f, 0.85f, 0.2f);
            case Rarity.Star6: return new Color(1.0f, 0.5f, 0.0f);
            default: return Color.white;
        }
    }
}