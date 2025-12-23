using UnityEngine;

/// <summary>
/// Market関連のサウンド定義
/// 「何を鳴らすか」を定義し、AudioManagerに再生を委譲
/// </summary>
public class MarketSound : MonoBehaviour
{
    public static MarketSound Instance { get; private set; }

    // ========================================
    // 売買サウンド
    // ========================================
    [Header("売買")]
    [Tooltip("株購入時")]
    [SerializeField] private AudioClip buySound;

    [Tooltip("株売却時（利確）")]
    [SerializeField] private AudioClip sellProfitSound;

    [Tooltip("株売却時（損切り）")]
    [SerializeField] private AudioClip sellLossSound;

    // ========================================
    // 価格変動サウンド
    // ========================================
    [Header("価格変動")]
    [Tooltip("暴落時")]
    [SerializeField] private AudioClip crashSound;

    [Tooltip("急騰時")]
    [SerializeField] private AudioClip surgeSound;

    // ========================================
    // イベントサウンド
    // ========================================
    [Header("イベント")]
    [Tooltip("ニュース通知")]
    [SerializeField] private AudioClip newsSound;

    [Tooltip("配当支払い")]
    [SerializeField] private AudioClip dividendSound;

    [Tooltip("銘柄解放")]
    [SerializeField] private AudioClip unlockSound;

    // ========================================
    // PVEサウンド
    // ========================================
    [Header("PVE")]
    [Tooltip("防衛戦開始")]
    [SerializeField] private AudioClip defenseStartSound;

    [Tooltip("防衛成功")]
    [SerializeField] private AudioClip defenseSuccessSound;

    [Tooltip("防衛失敗")]
    [SerializeField] private AudioClip defenseFailSound;

    [Tooltip("敵対的買収開始")]
    [SerializeField] private AudioClip takeoverStartSound;

    [Tooltip("買収防衛成功")]
    [SerializeField] private AudioClip takeoverWinSound;

    [Tooltip("買収防衛失敗")]
    [SerializeField] private AudioClip takeoverLoseSound;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ========================================
    // 再生メソッド
    // ========================================

    /// <summary>株購入時</summary>
    public void PlayBuy()
    {
        PlaySE(buySound);
    }

    /// <summary>株売却時</summary>
    public void PlaySell(bool isProfit)
    {
        PlaySE(isProfit ? sellProfitSound : sellLossSound);
    }

    /// <summary>暴落時</summary>
    public void PlayCrash()
    {
        PlaySE(crashSound);
    }

    /// <summary>急騰時</summary>
    public void PlaySurge()
    {
        PlaySE(surgeSound);
    }

    /// <summary>ニュース通知</summary>
    public void PlayNews()
    {
        PlaySE(newsSound);
    }

    /// <summary>配当支払い</summary>
    public void PlayDividend()
    {
        PlaySE(dividendSound);
    }

    /// <summary>銘柄解放</summary>
    public void PlayUnlock()
    {
        PlaySE(unlockSound);
    }

    /// <summary>防衛戦開始</summary>
    public void PlayDefenseStart()
    {
        PlaySE(defenseStartSound);
    }

    /// <summary>防衛結果</summary>
    public void PlayDefenseResult(bool success)
    {
        PlaySE(success ? defenseSuccessSound : defenseFailSound);
    }

    /// <summary>敵対的買収開始</summary>
    public void PlayTakeoverStart()
    {
        PlaySE(takeoverStartSound);
    }

    /// <summary>買収防衛結果</summary>
    public void PlayTakeoverResult(bool playerWon)
    {
        PlaySE(playerWon ? takeoverWinSound : takeoverLoseSound);
    }

    // ========================================
    // ヘルパー
    // ========================================

    private void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        AudioManager.Instance?.PlaySE(clip);
    }
}
