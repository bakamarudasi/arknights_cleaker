using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 強化の購入処理・状態管理を担当するマネージャー
/// WalletManager と InventoryManager に依存
/// </summary>
public class UpgradeManager : BaseSingleton<UpgradeManager>
{
    protected override bool Persistent => false;

    // ========================================
    // データベース
    // ========================================

    [Header("データベース")]
    [SerializeField] private UpgradeDatabase _database;

    /// <summary>強化データベース（外部参照用）</summary>
    public UpgradeDatabase Database => _database;

    // ========================================
    // 依存マネージャー参照
    // ========================================

    [Header("依存マネージャー")]
    [SerializeField] private WalletManager wallet;
    [SerializeField] private InventoryManager inventory;

    // ========================================
    // 強化レベルデータ（ID → Level）
    // ========================================

    [Header("強化レベル (Debug)")]
    [SerializeField] private SerializableDictionary<string, int> _upgradeLevels = new();

    // ========================================
    // イベント
    // ========================================

    /// <summary>強化レベルが変化した時 (id, newLevel)</summary>
    public event Action<string, int> OnUpgradeLevelChanged;

    /// <summary>強化が購入された時 (UpgradeData, newLevel)</summary>
    public event Action<UpgradeData, int> OnUpgradePurchased;

    // ========================================
    // 統計用コールバック
    // ========================================

    public Action OnUpgradeCountIncremented;
    public Action<int> OnHighestLevelUpdated;

    // ========================================
    // 初期化
    // ========================================

    void Start()
    {
        // 依存マネージャーの自動取得
        if (wallet == null) wallet = WalletManager.Instance;
        if (inventory == null) inventory = InventoryManager.Instance;
    }

    /// <summary>
    /// 依存マネージャーを設定（手動セットアップ用）
    /// </summary>
    public void Initialize(WalletManager walletManager, InventoryManager inventoryManager)
    {
        wallet = walletManager;
        inventory = inventoryManager;
    }

    // ========================================
    // レベル管理
    // ========================================

    /// <summary>
    /// 強化の現在レベルを取得
    /// </summary>
    public int GetLevel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        return _upgradeLevels.TryGetValue(id, out int level) ? level : 0;
    }

    /// <summary>
    /// 強化レベルを設定
    /// </summary>
    public void SetLevel(string id, int level)
    {
        if (string.IsNullOrEmpty(id)) return;

        _upgradeLevels[id] = Mathf.Max(0, level);
        OnUpgradeLevelChanged?.Invoke(id, _upgradeLevels[id]);

        // 統計更新
        OnHighestLevelUpdated?.Invoke(level);
    }

    /// <summary>
    /// 強化レベルを加算
    /// </summary>
    public void AddLevel(string id, int amount = 1)
    {
        int newLevel = GetLevel(id) + amount;
        SetLevel(id, newLevel);
    }

    // ========================================
    // 購入判定
    // ========================================

    /// <summary>
    /// 前提条件を満たしているかチェック
    /// </summary>
    public bool MeetsPrerequisite(UpgradeData data)
    {
        if (data == null) return false;

        // アイテム解放条件
        if (!inventory.HasUnlockItem(data.requiredUnlockItem))
            return false;

        // 前提強化条件
        if (data.prerequisiteUpgrade != null)
        {
            int prereqLevel = GetLevel(data.prerequisiteUpgrade.id);
            if (prereqLevel < data.prerequisiteLevel)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 強化が購入可能かチェック（通貨・素材含む）
    /// </summary>
    public bool CanPurchase(UpgradeData data)
    {
        if (data == null) return false;

        int currentLevel = GetLevel(data.id);

        // 最大レベルチェック
        if (data.IsMaxLevel(currentLevel))
            return false;

        // 前提条件チェック
        if (!MeetsPrerequisite(data))
            return false;

        // 通貨チェック
        double cost = data.GetCostAtLevel(currentLevel);
        CurrencyType currencyType = ConvertCurrencyType(data.currencyType);
        if (!wallet.CanAfford(cost, currencyType))
            return false;

        // 素材チェック
        if (!inventory.HasAllMaterials(data.requiredMaterials))
            return false;

        return true;
    }

    /// <summary>
    /// 強化の状態を取得（UI表示用）
    /// </summary>
    public UpgradeState GetState(UpgradeData data)
    {
        if (data == null) return UpgradeState.Locked;

        int currentLevel = GetLevel(data.id);

        // MAX判定
        if (data.IsMaxLevel(currentLevel))
            return UpgradeState.MaxLevel;

        // 前提条件判定
        if (!MeetsPrerequisite(data))
            return UpgradeState.Locked;

        // 購入可能判定
        if (CanPurchase(data))
            return UpgradeState.ReadyToUpgrade;

        // 資金/素材不足
        return UpgradeState.CanUnlockButNotAfford;
    }

    // ========================================
    // 購入処理
    // ========================================

    /// <summary>
    /// 強化を購入（メイン処理）
    /// </summary>
    /// <returns>成功したらtrue</returns>
    public bool TryPurchase(UpgradeData data)
    {
        if (!CanPurchase(data))
            return false;

        int currentLevel = GetLevel(data.id);
        double cost = data.GetCostAtLevel(currentLevel);
        CurrencyType currencyType = ConvertCurrencyType(data.currencyType);

        // 通貨消費
        wallet.Spend(cost, currencyType);

        // 素材消費
        inventory.UseAllMaterials(data.requiredMaterials);

        // レベルアップ
        AddLevel(data.id);

        // イベント発火
        int newLevel = GetLevel(data.id);
        OnUpgradePurchased?.Invoke(data, newLevel);
        OnUpgradeCountIncremented?.Invoke();

        Debug.Log($"[Upgrade] {data.displayName} Lv.{currentLevel} → Lv.{newLevel}");
        return true;
    }

    // ========================================
    // バッチ購入（複数レベル一気買い）
    // ========================================

    /// <summary>
    /// 指定回数まで連続購入を試みる
    /// </summary>
    /// <returns>実際に購入できた回数</returns>
    public int TryPurchaseMultiple(UpgradeData data, int maxCount)
    {
        int purchased = 0;
        for (int i = 0; i < maxCount; i++)
        {
            if (!TryPurchase(data)) break;
            purchased++;
        }
        return purchased;
    }

    /// <summary>
    /// 買えるだけ買う
    /// </summary>
    public int TryPurchaseMax(UpgradeData data, int safetyLimit = 100)
    {
        return TryPurchaseMultiple(data, safetyLimit);
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// リスト内の購入可能な強化の数を取得（バッジ表示用）
    /// </summary>
    public int GetPurchasableCount(List<UpgradeData> upgradeList)
    {
        if (upgradeList == null) return 0;

        int count = 0;
        foreach (var data in upgradeList)
        {
            if (CanPurchase(data))
                count++;
        }
        return count;
    }

    /// <summary>
    /// UpgradeData.CurrencyType → CurrencyType 変換
    /// </summary>
    private CurrencyType ConvertCurrencyType(UpgradeData.CurrencyType type)
    {
        return type switch
        {
            UpgradeData.CurrencyType.LMD => CurrencyType.LMD,
            UpgradeData.CurrencyType.Certificate => CurrencyType.Certificate,
            _ => CurrencyType.LMD
        };
    }

    // ========================================
    // セーブ/ロード用
    // ========================================

    /// <summary>
    /// 強化レベルデータを取得（セーブ用）
    /// </summary>
    public Dictionary<string, int> GetUpgradeData()
    {
        return new Dictionary<string, int>(_upgradeLevels);
    }

    /// <summary>
    /// 強化レベルデータを設定（ロード用）
    /// </summary>
    public void SetUpgradeData(Dictionary<string, int> data)
    {
        _upgradeLevels.Clear();
        if (data != null)
        {
            foreach (var kvp in data)
            {
                _upgradeLevels[kvp.Key] = kvp.Value;
                OnUpgradeLevelChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// 全強化レベルをリセット
    /// </summary>
    public void Reset()
    {
        var ids = new List<string>(_upgradeLevels.Keys);
        _upgradeLevels.Clear();
        foreach (var id in ids)
        {
            OnUpgradeLevelChanged?.Invoke(id, 0);
        }
    }
}

// ========================================
// 強化状態（UI表示用）
// ========================================

public enum UpgradeState
{
    Locked,                 // 前提条件未達成
    CanUnlockButNotAfford,  // 条件OK、資金/素材不足
    ReadyToUpgrade,         // 購入可能
    MaxLevel                // 最大レベル到達
}
