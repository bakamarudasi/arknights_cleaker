using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 全銘柄データを管理するデータベース
/// </summary>
[CreateAssetMenu(fileName = "StockDatabase", menuName = "ArknightsClicker/Market/Stock Database")]
public class StockDatabase : ScriptableObject
{
    [Header("銘柄リスト")]
    public List<StockData> stocks = new();

    // キャッシュ
    private Dictionary<string, StockData> stockCache;

    private void OnEnable()
    {
        RebuildCache();
    }

    private void RebuildCache()
    {
        stockCache = new Dictionary<string, StockData>();
        foreach (var stock in stocks)
        {
            if (stock != null && !string.IsNullOrEmpty(stock.stockId))
            {
                stockCache[stock.stockId] = stock;
            }
        }
    }

    /// <summary>
    /// 銘柄IDで検索
    /// </summary>
    public StockData GetByStockId(string stockId)
    {
        if (stockCache == null) RebuildCache();

        return stockCache.TryGetValue(stockId, out var stock) ? stock : null;
    }

    /// <summary>
    /// 解放済みの銘柄を取得
    /// </summary>
    public List<StockData> GetUnlockedStocks()
    {
        return stocks.Where(s => s != null && s.IsUnlocked())
                     .OrderBy(s => s.sortOrder)
                     .ToList();
    }

    /// <summary>
    /// 未解放の銘柄を取得
    /// </summary>
    public List<StockData> GetLockedStocks()
    {
        return stocks.Where(s => s != null && !s.IsUnlocked())
                     .OrderBy(s => s.sortOrder)
                     .ToList();
    }

    /// <summary>
    /// 特性で絞り込み
    /// </summary>
    public List<StockData> GetByTrait(CompanyData.CompanyTrait trait)
    {
        return stocks.Where(s => s != null && s.trait == trait)
                     .OrderBy(s => s.sortOrder)
                     .ToList();
    }

    /// <summary>
    /// 特性で絞り込み（後方互換性用オーバーロード）
    /// </summary>
    public List<StockData> GetByTrait(StockTrait trait)
    {
        // StockTraitをCompanyTraitに変換して検索
        var companyTrait = (CompanyData.CompanyTrait)(int)trait;
        return GetByTrait(companyTrait);
    }

    /// <summary>
    /// 全銘柄数
    /// </summary>
    public int TotalCount => stocks.Count(s => s != null);

    /// <summary>
    /// 解放済み銘柄数
    /// </summary>
    public int UnlockedCount => stocks.Count(s => s != null && s.IsUnlocked());

#if UNITY_EDITOR
    [ContextMenu("Rebuild Cache")]
    private void EditorRebuildCache()
    {
        RebuildCache();
    }

    [ContextMenu("Sort by Sort Order")]
    private void EditorSortStocks()
    {
        stocks = stocks.Where(s => s != null).OrderBy(s => s.sortOrder).ToList();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
