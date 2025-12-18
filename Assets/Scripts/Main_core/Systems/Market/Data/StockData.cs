using UnityEngine;

/// <summary>
/// 銘柄データ（ScriptableObject）
/// 各企業の株式情報を定義
/// </summary>
[CreateAssetMenu(fileName = "New_Stock", menuName = "ArknightsClicker/Market/Stock Data")]
public class StockData : ScriptableObject
{
    // ========================================
    // 基本情報
    // ========================================
    [Header("基本情報")]
    [Tooltip("銘柄コード（例: RL, PL, BSW）")]
    public string stockId;

    [Tooltip("企業名")]
    public string companyName;

    [Tooltip("企業の説明")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("企業ロゴ")]
    public Sprite logo;

    // ========================================
    // 株価設定
    // ========================================
    [Header("株価設定")]
    [Tooltip("初期株価（LMD）")]
    public double initialPrice = 1000;

    [Tooltip("最低株価（これ以下にはならない）")]
    public double minPrice = 10;

    [Tooltip("最高株価（0 = 無制限）")]
    public double maxPrice = 0;

    // ========================================
    // 変動特性
    // ========================================
    [Header("変動特性")]
    [Tooltip("ボラティリティ（変動の激しさ: 0.01〜0.5）")]
    [Range(0.01f, 0.5f)]
    public float volatility = 0.1f;

    [Tooltip("ドリフト（長期トレンド: -0.1〜0.2、正で右肩上がり）")]
    [Range(-0.1f, 0.2f)]
    public float drift = 0.02f;

    [Tooltip("ジャンプ確率（急騰/暴落の発生率: 0〜0.1）")]
    [Range(0f, 0.1f)]
    public float jumpProbability = 0.01f;

    [Tooltip("ジャンプ強度（急騰/暴落の大きさ: 0.1〜0.5）")]
    [Range(0.1f, 0.5f)]
    public float jumpIntensity = 0.2f;

    // ========================================
    // 企業特性
    // ========================================
    [Header("企業特性")]
    public StockTrait trait;

    [Tooltip("取引手数料率（0〜0.05）")]
    [Range(0f, 0.05f)]
    public float transactionFee = 0.01f;

    // ========================================
    // 解放条件
    // ========================================
    [Header("解放条件")]
    [Tooltip("この株を解放するキーアイテム（null = 最初から解放）")]
    public ItemData unlockKeyItem;

    [Tooltip("ショップでの並び順")]
    public int sortOrder = 0;

    // ========================================
    // 表示設定
    // ========================================
    [Header("表示設定")]
    [Tooltip("チャートの色")]
    public Color chartColor = Color.green;

    [Tooltip("企業テーマカラー")]
    public Color themeColor = Color.white;

    // ========================================
    // ヘルパーメソッド
    // ========================================

    /// <summary>
    /// 株が解放されているかチェック
    /// </summary>
    public bool IsUnlocked()
    {
        if (unlockKeyItem == null) return true;

        // InventoryManagerがあれば確認
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.GetItemCount(unlockKeyItem.id) > 0;
        }
        return false;
    }

    /// <summary>
    /// 特性の表示名を取得
    /// </summary>
    public string GetTraitDisplayName()
    {
        return trait switch
        {
            StockTrait.Military => "軍事",
            StockTrait.Innovation => "革新",
            StockTrait.Logistics => "物流",
            StockTrait.Trading => "貿易",
            StockTrait.Medical => "医療",
            StockTrait.Energy => "エネルギー",
            _ => "一般"
        };
    }

    /// <summary>
    /// 購入時の総コストを計算（手数料込み）
    /// </summary>
    public double CalculateBuyCost(double currentPrice, int quantity)
    {
        double baseCost = currentPrice * quantity;
        return baseCost * (1 + transactionFee);
    }

    /// <summary>
    /// 売却時の受取額を計算（手数料引き）
    /// </summary>
    public double CalculateSellReturn(double currentPrice, int quantity)
    {
        double baseReturn = currentPrice * quantity;
        return baseReturn * (1 - transactionFee);
    }
}

/// <summary>
/// 企業の特性タイプ
/// イベントやニュースの影響を決定
/// </summary>
public enum StockTrait
{
    General,    // 一般（特殊効果なし）
    Military,   // 軍事（戦争イベントで急騰）
    Innovation, // 革新（ボラティリティ高い）
    Logistics,  // 物流（景気敏感）
    Trading,    // 貿易（手数料安い、安定）
    Medical,    // 医療（源石関連イベントで変動）
    Energy      // エネルギー（長期安定成長）
}
