using System;

/// <summary>
/// ショップのビジネスロジックを担当
/// UI層から分離された購入計算・実行ロジック
/// </summary>
public class ShopService
{
    // ========================================
    // 依存関係
    // ========================================

    private readonly GameController _gameController;

    // ========================================
    // イベント
    // ========================================

    /// <summary>購入成功時に発火（購入したアップグレード, 購入回数）</summary>
    public event Action<UpgradeData, int> OnPurchaseSuccess;

    // ========================================
    // コンストラクタ
    // ========================================

    public ShopService(GameController gameController)
    {
        _gameController = gameController ?? throw new ArgumentNullException(nameof(gameController));
    }

    // ========================================
    // 購入計算
    // ========================================

    /// <summary>
    /// 所持金で買える最大回数を計算
    /// </summary>
    public int CalculateMaxBuyCount(UpgradeData upgrade, double availableMoney)
    {
        if (upgrade == null) return 0;

        int currentLevel = _gameController.Upgrade.GetLevel(upgrade.id);
        int maxLevel = upgrade.maxLevel;
        bool isUnlimited = maxLevel <= 0;

        int count = 0;
        double totalCost = 0;
        int level = currentLevel;

        // 最大100回まで（無限ループ防止）
        int safetyLimit = isUnlimited ? 100 : (maxLevel - currentLevel);

        while (count < safetyLimit)
        {
            double nextCost = upgrade.GetCostAtLevel(level);
            if (totalCost + nextCost > availableMoney) break;

            totalCost += nextCost;
            level++;
            count++;

            // 有限の場合、MAXに達したら終了
            if (!isUnlimited && level >= maxLevel) break;
        }

        return count;
    }

    /// <summary>
    /// 指定回数購入時の合計コストを計算
    /// </summary>
    public double CalculateTotalCost(UpgradeData upgrade, int count)
    {
        if (upgrade == null || count <= 0) return 0;

        int currentLevel = _gameController.Upgrade.GetLevel(upgrade.id);
        double total = 0;

        for (int i = 0; i < count; i++)
        {
            total += upgrade.GetCostAtLevel(currentLevel + i);
        }

        return total;
    }

    /// <summary>
    /// 単価コストを取得
    /// </summary>
    public double GetSingleCost(UpgradeData upgrade)
    {
        if (upgrade == null) return 0;
        int level = _gameController.Upgrade.GetLevel(upgrade.id);
        return upgrade.GetCostAtLevel(level);
    }

    // ========================================
    // 購入実行
    // ========================================

    /// <summary>
    /// 一括購入を実行
    /// </summary>
    /// <returns>実際に購入できた回数</returns>
    public int ExecuteBulkPurchase(UpgradeData upgrade, int requestedCount)
    {
        if (upgrade == null) return 0;

        double money = _gameController.Wallet.Money;
        int maxBuyable = CalculateMaxBuyCount(upgrade, money);
        int buyCount = System.Math.Min(requestedCount, maxBuyable);

        if (buyCount <= 0) return 0;

        int successCount = 0;
        for (int i = 0; i < buyCount; i++)
        {
            bool success = _gameController.Upgrade.TryPurchase(upgrade);
            if (success)
            {
                successCount++;
            }
            else
            {
                break;
            }
        }

        if (successCount > 0)
        {
            OnPurchaseSuccess?.Invoke(upgrade, successCount);
        }

        return successCount;
    }

    /// <summary>
    /// MAX購入を実行（買えるだけ買う）
    /// </summary>
    /// <returns>実際に購入できた回数</returns>
    public int ExecuteMaxPurchase(UpgradeData upgrade)
    {
        if (upgrade == null) return 0;

        double money = _gameController.Wallet.Money;
        int maxBuyable = CalculateMaxBuyCount(upgrade, money);

        if (maxBuyable <= 0) return 0;

        return ExecuteBulkPurchase(upgrade, maxBuyable);
    }

    // ========================================
    // 状態取得（GameControllerへの委譲）
    // ========================================

    public int GetUpgradeLevel(string upgradeId)
    {
        return _gameController.Upgrade.GetLevel(upgradeId);
    }

    public UpgradeState GetUpgradeState(UpgradeData upgrade)
    {
        return _gameController.Upgrade.GetState(upgrade);
    }

    public double GetMoney()
    {
        return _gameController.Wallet.Money;
    }

    public double GetCertificates()
    {
        return _gameController.Wallet.Certificates;
    }

    public int GetItemCount(string itemId)
    {
        return _gameController.Inventory.GetCount(itemId);
    }

    public bool IsMaxLevel(UpgradeData upgrade)
    {
        if (upgrade == null) return true;
        int level = _gameController.Upgrade.GetLevel(upgrade.id);
        return upgrade.IsMaxLevel(level);
    }

    public bool CanPurchase(UpgradeData upgrade)
    {
        if (upgrade == null) return false;
        return GetUpgradeState(upgrade) == UpgradeState.ReadyToUpgrade;
    }
}
