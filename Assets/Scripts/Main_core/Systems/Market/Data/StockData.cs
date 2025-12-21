using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 銘柄データ（ScriptableObject）
/// CompanyDataをマスターとして参照し、株式市場での表示・動作を定義
/// </summary>
[CreateAssetMenu(fileName = "New_Stock", menuName = "ArknightsClicker/Market/Stock Data")]
public class StockData : ScriptableObject
{
    // ========================================
    // 企業データ参照（マスター）
    // ========================================
    [Header("企業データ")]
    [Tooltip("この株に対応する企業データ（マスター）")]
    public CompanyData companyData;

    // ========================================
    // 株式固有の設定（表示順など）
    // ========================================
    [Header("銘柄表示設定")]
    [Tooltip("銘柄コード（例: RL, PL, BSW）- 空の場合はcompanyData.idを使用")]
    public string stockIdOverride;

    // ========================================
    // ヘルパープロパティ（後方互換性のため）
    // ========================================

    /// <summary>銘柄コード</summary>
    public string stockId => !string.IsNullOrEmpty(stockIdOverride) ? stockIdOverride : companyData?.id ?? "";

    /// <summary>企業名</summary>
    public string companyName => companyData?.displayName ?? "";

    /// <summary>企業の説明</summary>
    public string description => companyData?.description ?? "";

    /// <summary>企業ロゴ</summary>
    public Sprite logo => companyData?.logo;

    /// <summary>初期株価（LMD）</summary>
    public double initialPrice => companyData?.initialPrice ?? 1000;

    /// <summary>最低株価</summary>
    public double minPrice => companyData?.minPrice ?? 10;

    /// <summary>最高株価（0 = 無制限）</summary>
    public double maxPrice => companyData?.maxPrice ?? 0;

    /// <summary>ボラティリティ</summary>
    public float volatility => companyData?.volatility ?? 0.1f;

    /// <summary>ドリフト（長期トレンド）</summary>
    public float drift => companyData?.drift ?? 0.02f;

    /// <summary>ジャンプ確率</summary>
    public float jumpProbability => companyData?.jumpProbability ?? 0.01f;

    /// <summary>ジャンプ強度</summary>
    public float jumpIntensity => companyData?.jumpIntensity ?? 0.2f;

    /// <summary>企業特性</summary>
    public CompanyData.CompanyTrait trait => companyData?.traitType ?? CompanyData.CompanyTrait.None;

    /// <summary>取引手数料率</summary>
    public float transactionFee => companyData?.transactionFee ?? 0.01f;

    /// <summary>解放キーアイテム</summary>
    public ItemData unlockKeyItem => companyData?.unlockKeyItem;

    /// <summary>並び順</summary>
    public int sortOrder => companyData?.sortOrder ?? 0;

    /// <summary>チャートの色</summary>
    public Color chartColor => companyData?.chartColor ?? Color.green;

    /// <summary>企業テーマカラー</summary>
    public Color themeColor => companyData?.themeColor ?? Color.white;

    /// <summary>発行済み株式数</summary>
    public long totalShares => companyData?.totalShares ?? 1000000;

    /// <summary>配当率</summary>
    public float dividendRate => companyData?.dividendRate ?? 0f;

    /// <summary>配当間隔（秒）</summary>
    public int dividendIntervalSeconds => companyData?.dividendIntervalSeconds ?? 0;

    /// <summary>保有ボーナス</summary>
    public List<StockHoldingBonus> holdingBonuses => companyData?.holdingBonuses ?? new List<StockHoldingBonus>();

    // ========================================
    // ヘルパーメソッド（後方互換性のため）
    // ========================================

    /// <summary>
    /// 株が解放されているかチェック
    /// </summary>
    public bool IsUnlocked()
    {
        return companyData?.IsUnlocked() ?? true;
    }

    /// <summary>
    /// 特性の表示名を取得
    /// </summary>
    public string GetTraitDisplayName()
    {
        return companyData?.GetTraitDisplayName() ?? "一般";
    }

    /// <summary>
    /// 購入時の総コストを計算（手数料込み）
    /// </summary>
    public double CalculateBuyCost(double currentPrice, int quantity)
    {
        return companyData?.CalculateBuyCost(currentPrice, quantity)
            ?? currentPrice * quantity * (1 + 0.01);
    }

    /// <summary>
    /// 売却時の受取額を計算（手数料引き）
    /// </summary>
    public double CalculateSellReturn(double currentPrice, int quantity)
    {
        return companyData?.CalculateSellReturn(currentPrice, quantity)
            ?? currentPrice * quantity * (1 - 0.01);
    }
}

/// <summary>
/// 企業の特性タイプ（後方互換性のためのエイリアス）
/// 新規コードでは CompanyData.CompanyTrait を使用してください
/// </summary>
public enum StockTrait
{
    General = CompanyData.CompanyTrait.None,
    Military = CompanyData.CompanyTrait.Military,
    Innovation = CompanyData.CompanyTrait.TechInnovation,
    Logistics = CompanyData.CompanyTrait.Logistics,
    Trading = CompanyData.CompanyTrait.Trading,
    Medical = CompanyData.CompanyTrait.Arts, // Medical -> Arts にマッピング
    Energy = CompanyData.CompanyTrait.None   // Energy -> None にマッピング（該当なし）
}

// StockHoldingBonus と HoldingBonusType は CompanyData.cs で定義済み
