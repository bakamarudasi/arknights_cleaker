using System;
using UnityEngine;

/// <summary>
/// 通貨（LMD・資格証）の管理を担当するマネージャー
/// 残高の加算・減算・チェックを行う
/// </summary>
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    // ========================================
    // 通貨残高
    // ========================================

    [Header("通貨残高 (ReadOnly)")]
    [SerializeField] private double _money = 1000;
    [SerializeField] private double _certificates = 0;

    /// <summary>現在の所持金（LMD）</summary>
    public double Money => _money;

    /// <summary>現在の資格証</summary>
    public double Certificates => _certificates;

    // ========================================
    // イベント
    // ========================================

    /// <summary>所持金が変化した時</summary>
    public event Action<double> OnMoneyChanged;

    /// <summary>資格証が変化した時</summary>
    public event Action<double> OnCertificateChanged;

    // ========================================
    // 統計用コールバック（外部から設定）
    // ========================================

    /// <summary>お金を獲得した時の統計更新用</summary>
    public Action<double> OnMoneyEarned;

    /// <summary>お金を使った時の統計更新用</summary>
    public Action<double> OnMoneySpent;

    // ========================================
    // 初期化
    // ========================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========================================
    // LMD（メイン通貨）操作
    // ========================================

    /// <summary>
    /// お金を加算する
    /// </summary>
    public void AddMoney(double amount)
    {
        if (amount <= 0) return;

        _money += amount;
        OnMoneyChanged?.Invoke(_money);
        OnMoneyEarned?.Invoke(amount);
    }

    /// <summary>
    /// お金を減算する（残高チェックなし、内部用）
    /// </summary>
    private void SubtractMoney(double amount)
    {
        _money -= amount;
        if (_money < 0) _money = 0;
        OnMoneyChanged?.Invoke(_money);
    }

    /// <summary>
    /// お金を使う（残高チェックあり）
    /// </summary>
    /// <returns>成功したらtrue</returns>
    public bool SpendMoney(double amount)
    {
        if (!CanAffordMoney(amount)) return false;

        SubtractMoney(amount);
        OnMoneySpent?.Invoke(amount);
        return true;
    }

    /// <summary>
    /// 指定額を支払えるかチェック
    /// </summary>
    public bool CanAffordMoney(double amount)
    {
        return _money >= amount;
    }

    // ========================================
    // 資格証（プレミアム通貨）操作
    // ========================================

    /// <summary>
    /// 資格証を加算する
    /// </summary>
    public void AddCertificates(double amount)
    {
        if (amount <= 0) return;

        _certificates += amount;
        OnCertificateChanged?.Invoke(_certificates);
        Debug.Log($"[Wallet] 資格証を入手: +{amount}");
    }

    /// <summary>
    /// 資格証を使う（残高チェックあり）
    /// </summary>
    public bool SpendCertificates(double amount)
    {
        if (!CanAffordCertificates(amount)) return false;

        _certificates -= amount;
        if (_certificates < 0) _certificates = 0;
        OnCertificateChanged?.Invoke(_certificates);
        return true;
    }

    /// <summary>
    /// 資格証を支払えるかチェック
    /// </summary>
    public bool CanAffordCertificates(double amount)
    {
        return _certificates >= amount;
    }

    // ========================================
    // 汎用メソッド（通貨タイプ指定）
    // ========================================

    /// <summary>
    /// 通貨タイプを指定して支払えるかチェック
    /// </summary>
    public bool CanAfford(double amount, CurrencyType type)
    {
        return type switch
        {
            CurrencyType.LMD => CanAffordMoney(amount),
            CurrencyType.Certificate => CanAffordCertificates(amount),
            _ => false
        };
    }

    /// <summary>
    /// 通貨タイプを指定して支払う
    /// </summary>
    public bool Spend(double amount, CurrencyType type)
    {
        return type switch
        {
            CurrencyType.LMD => SpendMoney(amount),
            CurrencyType.Certificate => SpendCertificates(amount),
            _ => false
        };
    }

    /// <summary>
    /// 通貨タイプを指定して加算
    /// </summary>
    public void Add(double amount, CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.LMD:
                AddMoney(amount);
                break;
            case CurrencyType.Certificate:
                AddCertificates(amount);
                break;
        }
    }

    /// <summary>
    /// 通貨タイプを指定して現在値を取得
    /// </summary>
    public double GetBalance(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.LMD => _money,
            CurrencyType.Certificate => _certificates,
            _ => 0
        };
    }

    // ========================================
    // セーブ/ロード用
    // ========================================

    /// <summary>
    /// 残高を直接設定（ロード用）
    /// </summary>
    public void SetBalances(double money, double certificates)
    {
        _money = money;
        _certificates = certificates;
        OnMoneyChanged?.Invoke(_money);
        OnCertificateChanged?.Invoke(_certificates);
    }

    /// <summary>
    /// 残高をリセット（デバッグ・ニューゲーム用）
    /// </summary>
    public void Reset()
    {
        _money = 0;
        _certificates = 0;
        OnMoneyChanged?.Invoke(_money);
        OnCertificateChanged?.Invoke(_certificates);
    }
}

// ========================================
// 通貨タイプ（UpgradeDataから独立させる）
// ========================================

/// <summary>
/// 通貨の種類
/// </summary>
public enum CurrencyType
{
    LMD,            // メイン通貨（龍門幣）
    Certificate     // プレミアム通貨（資格証）
}
