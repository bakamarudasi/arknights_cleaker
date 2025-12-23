/// <summary>
/// イベント発動条件の種類
/// EventManagerがこれらの条件を監視し、達成時にイベントを発火する
/// </summary>
public enum EventTriggerType
{
    /// <summary>条件なし（手動発火用）</summary>
    None,

    // ========================================
    // 通貨関連
    // ========================================

    /// <summary>LMD累計獲得額が指定値に到達</summary>
    TotalMoneyEarned,

    /// <summary>現在のLMD所持額が指定値に到達</summary>
    CurrentMoneyReached,

    /// <summary>資格証が指定値に到達</summary>
    CertificateReached,

    // ========================================
    // クリック関連
    // ========================================

    /// <summary>累計クリック数が指定値に到達</summary>
    TotalClicks,

    /// <summary>累計クリティカル回数が指定値に到達</summary>
    TotalCriticalHits,

    /// <summary>最大単発ダメージが指定値に到達</summary>
    HighestClickDamage,

    // ========================================
    // 強化関連
    // ========================================

    /// <summary>累計強化購入回数が指定値に到達</summary>
    TotalUpgradesPurchased,

    /// <summary>特定の強化を購入（requireIdで指定）</summary>
    SpecificUpgradePurchased,

    /// <summary>特定の強化が指定レベルに到達</summary>
    UpgradeLevelReached,

    // ========================================
    // ガチャ関連
    // ========================================

    /// <summary>初めてガチャを引いた</summary>
    FirstGachaPull,

    /// <summary>累計ガチャ回数が指定値に到達</summary>
    TotalGachaPulls,

    /// <summary>特定レアリティのキャラを入手</summary>
    CharacterRarityObtained,

    /// <summary>特定キャラを入手（requireIdで指定）</summary>
    SpecificCharacterObtained,

    // ========================================
    // インベントリ関連
    // ========================================

    /// <summary>特定アイテムを入手（requireIdで指定）</summary>
    SpecificItemObtained,

    /// <summary>キャラクター所持数が指定値に到達</summary>
    TotalCharactersOwned,

    // ========================================
    // SP/フィーバー関連
    // ========================================

    /// <summary>初めてフィーバーを発動</summary>
    FirstFeverActivated,

    /// <summary>累計フィーバー発動回数が指定値に到達</summary>
    TotalFeverActivations,

    // ========================================
    // 株式市場関連
    // ========================================

    /// <summary>初めて株を購入</summary>
    FirstStockPurchased,

    /// <summary>累計株式利益が指定値に到達</summary>
    TotalStockProfit,

    /// <summary>累計配当金が指定値に到達</summary>
    TotalDividendsReceived,

    // ========================================
    // 時間関連
    // ========================================

    /// <summary>累計プレイ時間が指定秒数に到達</summary>
    PlayTimeReached,

    /// <summary>ゲーム開始から指定秒数経過（セッション内）</summary>
    SessionTimeReached,

    // ========================================
    // 好感度関連
    // ========================================

    /// <summary>特定キャラの好感度が指定値に到達</summary>
    AffectionLevelReached,

    /// <summary>いずれかのキャラの好感度がMAXに到達</summary>
    AnyCharacterMaxAffection,
}
