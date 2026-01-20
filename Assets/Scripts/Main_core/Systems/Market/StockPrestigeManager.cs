using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 株式周回（プレステージ）システムの管理
/// 100%買い占め → リセット → 永続ボーナス獲得
/// </summary>
public class StockPrestigeManager : BaseSingleton<StockPrestigeManager>
{
    protected override bool Persistent => false;

    // ========================================
    // 設定
    // ========================================
    [Header("周回設定データ")]
    [SerializeField] private List<StockPrestigeData> prestigeDataList = new();

    // ========================================
    // ランタイム状態
    // ========================================
    // stockId -> 周回数
    private Dictionary<string, int> prestigeLevels = new();

    // ========================================
    // イベント
    // ========================================
    /// <summary>買収完了時（stockId, newPrestigeLevel）</summary>
    public event Action<string, int> OnAcquisitionComplete;

    /// <summary>周回データ変更時</summary>
    public event Action OnPrestigeDataChanged;

    // ========================================
    // 初期化
    // ========================================

    private void Start()
    {
        // PortfolioManagerのイベントを購読
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnHoldingChanged += CheckAcquisition;
        }
    }

    private void OnDestroy()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnHoldingChanged -= CheckAcquisition;
        }
    }

    // ========================================
    // 買収チェック
    // ========================================

    private void CheckAcquisition(string stockId)
    {
        float holdingRate = GetHoldingRate(stockId);

        // 100%到達チェック
        if (holdingRate >= 1.0f)
        {
            TryCompleteAcquisition(stockId);
        }
    }

    /// <summary>
    /// 保有率を計算（StockHoldingBonusManager削除に伴い移植）
    /// </summary>
    public float GetHoldingRate(string stockId)
    {
        var stockData = MarketManager.Instance?.GetStockData(stockId);
        if (stockData == null) return 0f;

        long totalShares = GetAdjustedTotalShares(stockId);
        if (totalShares <= 0) return 0f;

        int holdings = PortfolioManager.Instance?.GetHoldingQuantity(stockId) ?? 0;
        return (float)holdings / totalShares;
    }

    /// <summary>
    /// 買収完了を試行
    /// </summary>
    public bool TryCompleteAcquisition(string stockId)
    {
        var prestigeData = GetPrestigeData(stockId);
        if (prestigeData == null)
        {
            Debug.LogWarning($"[Prestige] No prestige data for stock: {stockId}");
            return false;
        }

        int currentLevel = GetPrestigeLevel(stockId);

        // 最大周回チェック
        if (prestigeData.IsMaxLevel(currentLevel))
        {
            Debug.Log($"[Prestige] {stockId} is at max prestige level: {currentLevel}");
            return false;
        }

        // 買収完了処理
        ExecuteAcquisition(stockId, prestigeData, currentLevel);
        return true;
    }

    private void ExecuteAcquisition(string stockId, StockPrestigeData prestigeData, int currentLevel)
    {
        int newLevel = currentLevel + 1;

        // 周回数を更新
        prestigeLevels[stockId] = newLevel;

        // プレイヤーの保有株をリセット
        ResetHoldings(stockId);

        // イベント発火
        OnAcquisitionComplete?.Invoke(stockId, newLevel);
        OnPrestigeDataChanged?.Invoke();

        // ログ
        string companyName = prestigeData.targetStock?.companyName ?? stockId;
        Debug.Log($"[Prestige] Acquisition complete! {companyName} -> Prestige Lv.{newLevel}");

        // 演出メッセージ（UIシステムがあれば表示）
        string message = string.Format(prestigeData.acquisitionMessage, companyName);
        ShowAcquisitionMessage(message, prestigeData);
    }

    private void ResetHoldings(string stockId)
    {
        // PortfolioManagerから該当株を全売却（ただしLMDは付与しない）
        if (PortfolioManager.Instance != null)
        {
            // 内部的に保有をクリア
            var holdings = PortfolioManager.Instance.Holdings;
            if (holdings.ContainsKey(stockId))
            {
                // 直接リセットするため、売却イベントは発火させない
                // PortfolioManagerに ResetHolding メソッドを追加するか、
                // ここでは単に売却して LMD を元に戻す
                int quantity = PortfolioManager.Instance.GetHoldingQuantity(stockId);
                if (quantity > 0)
                {
                    // 売却してLMDを獲得
                    double beforeMoney = WalletManager.Instance?.Money ?? 0;
                    PortfolioManager.Instance.TrySellAll(stockId);
                    double afterMoney = WalletManager.Instance?.Money ?? 0;

                    // 買収完了時はLMDを返さない（投資として消費）
                    // オプション: 一部返金してもいい
                    double refundRate = 0.0; // 0% = 全額消費、0.5 = 半額返金
                    double earned = afterMoney - beforeMoney;
                    double deduct = earned * (1 - refundRate);
                    WalletManager.Instance?.SpendMoney(deduct);
                }
            }
        }
    }

    private void ShowAcquisitionMessage(string message, StockPrestigeData prestigeData)
    {
        // TODO: UIシステムと連携してメッセージ表示
        Debug.Log($"[Prestige] {message}");

        // 効果音再生
        if (prestigeData.acquisitionSound != null)
        {
            // AudioManager があれば再生
            // AudioManager.Instance?.PlaySFX(prestigeData.acquisitionSound);
        }
    }

    // ========================================
    // 周回情報取得API
    // ========================================

    /// <summary>
    /// 指定銘柄の周回レベルを取得
    /// </summary>
    public int GetPrestigeLevel(string stockId)
    {
        return prestigeLevels.TryGetValue(stockId, out int level) ? level : 0;
    }

    /// <summary>
    /// 指定銘柄の周回設定を取得
    /// </summary>
    public StockPrestigeData GetPrestigeData(string stockId)
    {
        return prestigeDataList.FirstOrDefault(p => p.targetStock?.stockId == stockId);
    }

    /// <summary>
    /// 指定銘柄の現在のtotalSharesを計算（周回補正込み）
    /// </summary>
    public long GetAdjustedTotalShares(string stockId)
    {
        var prestigeData = GetPrestigeData(stockId);
        if (prestigeData == null)
        {
            // 周回設定がない場合は元のtotalSharesを返す
            var stockData = MarketManager.Instance?.GetStockData(stockId);
            return stockData?.totalShares ?? 1000000;
        }

        int level = GetPrestigeLevel(stockId);
        return prestigeData.CalculateTotalShares(level);
    }

    /// <summary>
    /// 指定銘柄の永続ボーナスを取得
    /// </summary>
    public float GetPrestigeBonus(string stockId, PrestigeBonusType type)
    {
        var prestigeData = GetPrestigeData(stockId);
        if (prestigeData == null) return 0f;

        int level = GetPrestigeLevel(stockId);
        return prestigeData.GetTotalBonus(type, level);
    }

    /// <summary>
    /// 全銘柄の指定タイプの永続ボーナス合計を取得
    /// </summary>
    public float GetTotalPrestigeBonus(PrestigeBonusType type)
    {
        float total = 0f;
        foreach (var data in prestigeDataList)
        {
            if (data?.targetStock == null) continue;
            total += GetPrestigeBonus(data.targetStock.stockId, type);
        }
        return total;
    }

    /// <summary>
    /// 全周回データのサマリーを取得
    /// </summary>
    public List<PrestigeSummary> GetPrestigeSummaries()
    {
        var summaries = new List<PrestigeSummary>();

        foreach (var data in prestigeDataList)
        {
            if (data?.targetStock == null) continue;

            string stockId = data.targetStock.stockId;
            int level = GetPrestigeLevel(stockId);

            summaries.Add(new PrestigeSummary
            {
                stockId = stockId,
                companyName = data.targetStock.companyName,
                prestigeLevel = level,
                currentTotalShares = GetAdjustedTotalShares(stockId),
                isMaxLevel = data.IsMaxLevel(level)
            });
        }

        return summaries;
    }

    // ========================================
    // セーブ/ロード
    // ========================================

    public PrestigeSaveData GetSaveData()
    {
        return new PrestigeSaveData
        {
            prestigeLevels = prestigeLevels
                .Select(kvp => new PrestigeLevelEntry { stockId = kvp.Key, level = kvp.Value })
                .ToList()
        };
    }

    public void LoadSaveData(PrestigeSaveData data)
    {
        prestigeLevels.Clear();

        if (data?.prestigeLevels != null)
        {
            foreach (var entry in data.prestigeLevels)
            {
                prestigeLevels[entry.stockId] = entry.level;
            }
        }

        OnPrestigeDataChanged?.Invoke();
    }

    // ========================================
    // デバッグ
    // ========================================

    [ContextMenu("Log Prestige Status")]
    public void LogPrestigeStatus()
    {
        Debug.Log("=== Stock Prestige Status ===");
        foreach (var data in prestigeDataList)
        {
            if (data?.targetStock == null) continue;

            string stockId = data.targetStock.stockId;
            int level = GetPrestigeLevel(stockId);
            long shares = GetAdjustedTotalShares(stockId);

            Debug.Log($"{data.targetStock.companyName}: Lv.{level}, TotalShares: {shares:N0}");

            foreach (var bonus in data.prestigeBonuses)
            {
                float value = bonus.valuePerLevel * level;
                Debug.Log($"  - {bonus.bonusType}: +{value * 100:F1}%");
            }
        }
    }
}

/// <summary>
/// 周回情報サマリー（UI表示用）
/// </summary>
public struct PrestigeSummary
{
    public string stockId;
    public string companyName;
    public int prestigeLevel;
    public long currentTotalShares;
    public bool isMaxLevel;
}

/// <summary>
/// 周回セーブデータ
/// </summary>
[Serializable]
public class PrestigeSaveData
{
    public List<PrestigeLevelEntry> prestigeLevels;
}

[Serializable]
public class PrestigeLevelEntry
{
    public string stockId;
    public int level;
}
