using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;
/// <summary>
/// ゲーム全体の司令塔（超スリム版）
/// 各Managerの統括・イベント中継のみを担当
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    // ========================================
    // マネージャー参照
    // ========================================

    [Header("マネージャー")]
    public WalletManager Wallet;
    public InventoryManager Inventory;
    public UpgradeManager Upgrade;
    public IncomeManager Income;
    public SPManager SP;

    public GachaManager Gacha;
    // ========================================
    // UI参照
    // ========================================

    [Header("UI")]
    [SerializeField] private UI_RollingCounter moneyCounter;
    [SerializeField] private TextMeshProUGUI spText;
    [SerializeField] private TextMeshProUGUI certText;

    // ========================================
    // クリック設定
    // ========================================

    [Header("クリック設定")]
    public double clickBase = 10;
    [Range(0f, 1f)] public float baseCriticalChance = 0.05f;
    public double baseCriticalMultiplier = 2.0;
    [Range(0f, 1f)] public float slotTriggerChance = 0.001f;
    public UnityEvent<int> OnSlotTriggered;  // int = 倍率

    // ========================================
    // ボーナス（強化による加算分）
    // ========================================

    [Header("ボーナス (Debug)")]
    public double clickFlatBonus = 0;
    public double clickPercentBonus = 0;
    public double criticalChanceBonus = 0;
    public double criticalPowerBonus = 0;
    public double globalMultiplier = 1.0;

    // ========================================
    // 統計
    // ========================================

    [Header("統計")]
    [SerializeField] private StatisticsData stats = new();

    // ========================================
    // 計算キャッシュ
    // ========================================

    private double _finalClickValue;
    private float _finalCritChance;
    private double _finalCritMultiplier;

    // ========================================
    // イベントコールバック参照（解除用）
    // ========================================

    private Action<double> _onMoneyChangedCallback;
    private Action<double> _onCertificateChangedCallback;
    private Action<double> _onIncomeGeneratedCallback;
    private Action<UpgradeData, int> _onUpgradePurchasedCallback;
    private Action<float> _onSPChangedCallback;
    private Action _onFeverStartedCallback;
    private Action _onFeverEndedCallback;

    // ========================================
    // バフシステム用イベント
    // ========================================

    /// <summary>ステータス再計算が必要な時に発火</summary>
    public event Action OnStatsRecalculated;

    // ========================================
    // 初期化
    // ========================================

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        FetchManagers();
        BindEvents();
        RecalculateStats();
        Income.StartIncome();
        InvokeRepeating(nameof(TrackPlayTime), 1f, 1f);
    }

    private void FetchManagers()
    {
        // Inspectorで未設定なら自動追加
        Wallet ??= GetOrAddManager<WalletManager>();
        Inventory ??= GetOrAddManager<InventoryManager>();
        Upgrade ??= GetOrAddManager<UpgradeManager>();
        Income ??= GetOrAddManager<IncomeManager>();
        SP ??= GetOrAddManager<SPManager>();
        Gacha ??= GetOrAddManager<GachaManager>();
        Upgrade.Initialize(Wallet, Inventory);
    }

    /// <summary>
    /// シーンに既存のManagerがあれば取得、なければこのGameObjectに追加
    /// </summary>
    private T GetOrAddManager<T>() where T : MonoBehaviour
    {
        var existing = FindFirstObjectByType<T>();
        if (existing != null) return existing;
        return gameObject.AddComponent<T>();
    }

    private void BindEvents()
    {
        // コールバックをフィールドに保存（解除用）
        _onMoneyChangedCallback = _ => UpdateMoneyUI();
        _onCertificateChangedCallback = _ => UpdateCertUI();
        _onIncomeGeneratedCallback = amt => Wallet.AddMoney(amt);
        _onUpgradePurchasedCallback = OnUpgradePurchased;
        _onSPChangedCallback = _ => UpdateSPUI();
        _onFeverStartedCallback = UpdateSPUI;
        _onFeverEndedCallback = UpdateSPUI;

        // イベント登録
        Wallet.OnMoneyChanged += _onMoneyChangedCallback;
        Wallet.OnCertificateChanged += _onCertificateChangedCallback;
        Wallet.OnMoneyEarned += amt => stats.totalMoneyEarned += amt;
        Wallet.OnMoneySpent += amt => stats.totalMoneySpent += amt;
        Inventory.OnMaterialsUsed += amt => stats.totalMaterialsUsed += amt;
        Income.OnIncomeGenerated += _onIncomeGeneratedCallback;
        Upgrade.OnUpgradePurchased += _onUpgradePurchasedCallback;
        Upgrade.OnUpgradeCountIncremented += () => stats.totalUpgradesPurchased++;
        SP.OnSPChanged += _onSPChangedCallback;
        SP.OnFeverStarted += _onFeverStartedCallback;
        SP.OnFeverEnded += _onFeverEndedCallback;
    }

    // ========================================
    // クリック処理
    // ========================================

    public void ClickMainButton()
    {
        SP.ChargeSP();

        var ctx = new ClickStatsContext
        {
            BaseClickValue = _finalClickValue,
            CriticalChance = _finalCritChance,
            CriticalMultiplier = _finalCritMultiplier,
            FeverMultiplier = SP.FinalFeverMultiplier,
            IsFeverActive = SP.IsFeverActive,
            SpChargeAmount = SP.FinalChargeAmount,
            SlotTriggerChance = slotTriggerChance
        };

        var result = ClickManager.Calculate(ctx);

        Wallet.AddMoney(result.EarnedAmount);

        // スロット発動時はルーレットで倍率を決定してボーナス報酬を追加
        if (result.TriggeredSlot)
        {
            int slotMultiplier = RollSlotMultiplier();
            double slotBonus = result.EarnedAmount * slotMultiplier;
            Wallet.AddMoney(slotBonus);
            OnSlotTriggered?.Invoke(slotMultiplier);
        }

        // 統計
        stats.totalClicks++;
        if (result.WasCritical) stats.totalCriticalHits++;
        if (result.EarnedAmount > stats.highestClickDamage)
            stats.highestClickDamage = result.EarnedAmount;

        FloatingTextManager.Instance?.Spawn(result.EarnedAmount, Input.mousePosition, result.WasCritical);
    }

    // ========================================
    // 強化購入時
    // ========================================

    private void OnUpgradePurchased(UpgradeData data, int newLevel)
    {
        switch (data.upgradeType)
        {
            case UpgradeData.UpgradeType.Click_FlatAdd:
                clickFlatBonus += data.effectValue;
                break;
            case UpgradeData.UpgradeType.Click_PercentAdd:
                clickPercentBonus += data.effectValue;
                break;
            case UpgradeData.UpgradeType.Income_FlatAdd:
                Income.AddFlatBonus(data.effectValue);
                break;
            case UpgradeData.UpgradeType.Income_PercentAdd:
                Income.AddPercentBonus(data.effectValue);
                break;
            case UpgradeData.UpgradeType.Critical_ChanceAdd:
                criticalChanceBonus += data.effectValue;
                break;
            case UpgradeData.UpgradeType.Critical_PowerAdd:
                criticalPowerBonus += data.effectValue;
                break;
            case UpgradeData.UpgradeType.SP_ChargeAdd:
                SP.AddChargeBonus((float)data.effectValue);
                break;
            case UpgradeData.UpgradeType.Fever_PowerAdd:
                SP.AddFeverPowerBonus((float)data.effectValue);
                break;
        }
        RecalculateStats();
    }

    // ========================================
    // 計算
    // ========================================

    /// <summary>
    /// ステータスを再計算（バフ変更時などに外部から呼び出し可能）
    /// </summary>
    public void RecalculateStats()
    {
        _finalClickValue = (clickBase + clickFlatBonus) * (1 + clickPercentBonus) * globalMultiplier;
        _finalCritChance = Mathf.Clamp01(baseCriticalChance + (float)criticalChanceBonus);
        _finalCritMultiplier = baseCriticalMultiplier + criticalPowerBonus;

        // 再計算完了を通知（バフシステム等で利用可能）
        OnStatsRecalculated?.Invoke();
    }

    /// <summary>
    /// 一時バフを適用してステータスを再計算
    /// </summary>
    /// <param name="clickMultiplier">クリック倍率（1.0 = 100%）</param>
    /// <param name="critChanceBonus">クリティカル確率ボーナス</param>
    /// <param name="critPowerBonus">クリティカル倍率ボーナス</param>
    public void ApplyTemporaryBuff(double clickMultiplier = 1.0, double critChanceBonus = 0, double critPowerBonus = 0)
    {
        // globalMultiplierに一時バフを反映
        globalMultiplier *= clickMultiplier;
        criticalChanceBonus += critChanceBonus;
        criticalPowerBonus += critPowerBonus;
        RecalculateStats();
    }

    /// <summary>
    /// グローバル倍率を直接設定（株機能等で使用）
    /// </summary>
    public void SetGlobalMultiplier(double multiplier)
    {
        globalMultiplier = multiplier;
        RecalculateStats();
    }

    /// <summary>
    /// スロットのルーレット倍率を決定
    /// 確率分布:
    /// - x2-10:     50%
    /// - x11-50:    30%
    /// - x51-100:   15%
    /// - x101-999:  4%
    /// - x1000-9999: 1%
    /// </summary>
    private int RollSlotMultiplier()
    {
        float roll = UnityEngine.Random.value;

        if (roll < 0.50f)
            return UnityEngine.Random.Range(2, 11);       // x2-10
        else if (roll < 0.80f)
            return UnityEngine.Random.Range(11, 51);      // x11-50
        else if (roll < 0.95f)
            return UnityEngine.Random.Range(51, 101);     // x51-100
        else if (roll < 0.99f)
            return UnityEngine.Random.Range(101, 1000);   // x101-999
        else
            return UnityEngine.Random.Range(1000, 10000); // x1000-9999
    }

    // ========================================
    // UI更新
    // ========================================

    private void UpdateMoneyUI() => moneyCounter?.SetValue(Wallet.Money, false);
    private void UpdateCertUI() => certText?.SetText(Wallet.Certificates.ToString("N0"));
    private void UpdateSPUI() => spText?.SetText(SP.IsFeverActive ? "<color=red>FEVER!!</color>" : $"SP: {SP.CurrentSP:F0} / {SP.MaxSP}");

    // ========================================
    // 統計
    // ========================================

    private void TrackPlayTime() => stats.totalPlayTimeSeconds += 1.0;
    public StatisticsData GetStatistics() => stats;

    // ========================================
    // クリーンアップ
    // ========================================

    private void OnDestroy()
    {
        // InvokeRepeating停止
        CancelInvoke(nameof(TrackPlayTime));

        // イベント解除
        if (Wallet != null)
        {
            Wallet.OnMoneyChanged -= _onMoneyChangedCallback;
            Wallet.OnCertificateChanged -= _onCertificateChangedCallback;
        }

        if (Income != null)
        {
            Income.OnIncomeGenerated -= _onIncomeGeneratedCallback;
        }

        if (Upgrade != null)
        {
            Upgrade.OnUpgradePurchased -= _onUpgradePurchasedCallback;
        }

        if (SP != null)
        {
            SP.OnSPChanged -= _onSPChangedCallback;
            SP.OnFeverStarted -= _onFeverStartedCallback;
            SP.OnFeverEnded -= _onFeverEndedCallback;
        }

        // コールバック参照クリア
        _onMoneyChangedCallback = null;
        _onCertificateChangedCallback = null;
        _onIncomeGeneratedCallback = null;
        _onUpgradePurchasedCallback = null;
        _onSPChangedCallback = null;
        _onFeverStartedCallback = null;
        _onFeverEndedCallback = null;
    }
}