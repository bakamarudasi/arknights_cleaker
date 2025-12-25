using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全強化データを管理するデータベース
/// カテゴリ別キャッシュにより高速アクセスを実現
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "Game/UpgradeDatabase")]
public class UpgradeDatabase : ScriptableObject
{
    [Header("全強化データ")]
    public List<UpgradeData> allUpgrades = new();

    // ========================================
    // キャッシュ
    // ========================================

    /// <summary>カテゴリ別キャッシュ</summary>
    private Dictionary<UpgradeData.UpgradeCategory, List<UpgradeData>> _categoryCache;

    /// <summary>カテゴリ別ソート済みキャッシュ</summary>
    private Dictionary<UpgradeData.UpgradeCategory, List<UpgradeData>> _sortedCache;

    /// <summary>ID検索用キャッシュ</summary>
    private Dictionary<string, UpgradeData> _idCache;

    /// <summary>キャッシュが有効かどうか</summary>
    private bool _isCacheValid = false;

    // ========================================
    // キャッシュ管理
    // ========================================

    /// <summary>
    /// キャッシュを構築（初回アクセス時または無効化後）
    /// </summary>
    private void BuildCacheIfNeeded()
    {
        if (_isCacheValid) return;

        _categoryCache = new Dictionary<UpgradeData.UpgradeCategory, List<UpgradeData>>();
        _sortedCache = new Dictionary<UpgradeData.UpgradeCategory, List<UpgradeData>>();
        _idCache = new Dictionary<string, UpgradeData>();

        // 全カテゴリのリストを初期化
        foreach (UpgradeData.UpgradeCategory cat in System.Enum.GetValues(typeof(UpgradeData.UpgradeCategory)))
        {
            _categoryCache[cat] = new List<UpgradeData>();
        }

        // データを振り分け
        foreach (var upgrade in allUpgrades)
        {
            if (upgrade == null) continue;

            _categoryCache[upgrade.category].Add(upgrade);

            if (!string.IsNullOrEmpty(upgrade.id))
            {
                _idCache[upgrade.id] = upgrade;
            }
        }

        // ソート済みキャッシュを構築
        foreach (var kvp in _categoryCache)
        {
            var sorted = new List<UpgradeData>(kvp.Value);
            sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            _sortedCache[kvp.Key] = sorted;
        }

        _isCacheValid = true;
    }

    /// <summary>
    /// キャッシュを無効化（データ変更時に呼び出す）
    /// </summary>
    public void InvalidateCache()
    {
        _isCacheValid = false;
    }

    /// <summary>
    /// エディタ時やデータ変更時に自動的にキャッシュを無効化
    /// </summary>
    private void OnValidate()
    {
        InvalidateCache();
    }

    private void OnEnable()
    {
        InvalidateCache();
    }

    // ========================================
    // 取得メソッド（キャッシュ使用）
    // ========================================

    /// <summary>
    /// カテゴリ別に取得（キャッシュから高速取得）
    /// 注意: 返されるリストは読み取り専用として扱うこと
    /// </summary>
    public List<UpgradeData> GetByCategory(UpgradeData.UpgradeCategory category)
    {
        BuildCacheIfNeeded();
        return _categoryCache.TryGetValue(category, out var list) ? list : new List<UpgradeData>();
    }

    /// <summary>
    /// ソート順で取得（キャッシュから高速取得）
    /// 注意: 返されるリストは読み取り専用として扱うこと
    /// </summary>
    public List<UpgradeData> GetSorted(UpgradeData.UpgradeCategory category)
    {
        BuildCacheIfNeeded();
        return _sortedCache.TryGetValue(category, out var list) ? list : new List<UpgradeData>();
    }

    // ========================================
    // フィルター付き取得（動的判定が必要なためキャッシュ不可）
    // ========================================

    /// <summary>
    /// ロック解除済みのみ（再利用可能なリストに結果を格納）
    /// </summary>
    public void GetUnlocked(UpgradeData.UpgradeCategory category, List<UpgradeData> result)
    {
        result.Clear();
        var source = GetByCategory(category);
        var upgradeManager = GameController.Instance?.Upgrade;
        if (upgradeManager == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            if (upgradeManager.MeetsPrerequisite(source[i]))
            {
                result.Add(source[i]);
            }
        }
    }

    /// <summary>
    /// ロック解除済みのみ（新規リスト生成版 - 後方互換）
    /// </summary>
    public List<UpgradeData> GetUnlocked(UpgradeData.UpgradeCategory category)
    {
        var result = new List<UpgradeData>();
        GetUnlocked(category, result);
        return result;
    }

    /// <summary>
    /// 購入可能なもののみ（再利用可能なリストに結果を格納）
    /// </summary>
    public void GetPurchasable(UpgradeData.UpgradeCategory category, List<UpgradeData> result)
    {
        result.Clear();
        var source = GetByCategory(category);
        var upgradeManager = GameController.Instance?.Upgrade;
        if (upgradeManager == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            if (upgradeManager.CanPurchase(source[i]))
            {
                result.Add(source[i]);
            }
        }
    }

    /// <summary>
    /// 購入可能なもののみ（新規リスト生成版 - 後方互換）
    /// </summary>
    public List<UpgradeData> GetPurchasable(UpgradeData.UpgradeCategory category)
    {
        var result = new List<UpgradeData>();
        GetPurchasable(category, result);
        return result;
    }

    /// <summary>
    /// MAX未到達のみ（再利用可能なリストに結果を格納）
    /// </summary>
    public void GetNotMaxed(UpgradeData.UpgradeCategory category, List<UpgradeData> result)
    {
        result.Clear();
        var source = GetByCategory(category);
        var upgradeManager = GameController.Instance?.Upgrade;
        if (upgradeManager == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            if (upgradeManager.GetState(source[i]) != UpgradeState.MaxLevel)
            {
                result.Add(source[i]);
            }
        }
    }

    /// <summary>
    /// MAX未到達のみ（新規リスト生成版 - 後方互換）
    /// </summary>
    public List<UpgradeData> GetNotMaxed(UpgradeData.UpgradeCategory category)
    {
        var result = new List<UpgradeData>();
        GetNotMaxed(category, result);
        return result;
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// IDで検索（キャッシュから高速取得）
    /// </summary>
    public UpgradeData FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        BuildCacheIfNeeded();
        return _idCache.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>
    /// カテゴリ内の購入可能数（バッジ用）
    /// GC圧力を避けるためカウントのみ
    /// </summary>
    public int GetPurchasableCount(UpgradeData.UpgradeCategory category)
    {
        var source = GetByCategory(category);
        var upgradeManager = GameController.Instance?.Upgrade;
        if (upgradeManager == null) return 0;

        int count = 0;
        for (int i = 0; i < source.Count; i++)
        {
            if (upgradeManager.CanPurchase(source[i]))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 全体の購入可能数
    /// GC圧力を避けるためカウントのみ
    /// </summary>
    public int GetTotalPurchasableCount()
    {
        BuildCacheIfNeeded();
        var upgradeManager = GameController.Instance?.Upgrade;
        if (upgradeManager == null) return 0;

        int count = 0;
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            if (upgradeManager.CanPurchase(allUpgrades[i]))
            {
                count++;
            }
        }
        return count;
    }
}