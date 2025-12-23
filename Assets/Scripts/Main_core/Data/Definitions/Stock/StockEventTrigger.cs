using System;
using UnityEngine;

/// <summary>
/// 株価変動イベントトリガー
/// </summary>
[Serializable]
public class StockEventTrigger
{
    [Tooltip("イベント名")]
    public string eventName;

    [Tooltip("イベントの説明")]
    [TextArea]
    public string description;

    [Tooltip("トリガー条件")]
    public StockEventTriggerType triggerType;

    [Tooltip("株価への影響 (-0.3 = 30%下落, 0.5 = 50%上昇)")]
    [Range(-0.5f, 1f)]
    public float priceImpact = 0.1f;

    [Tooltip("影響の持続時間（秒）- 0で恒久")]
    public float durationSeconds = 300f;

    [Tooltip("発生確率 (1日あたり) - RandomDaily時のみ")]
    [Range(0f, 1f)]
    public float dailyProbability = 0.1f;

    [Tooltip("同セクター連動率 (0.5 = 50%の影響)")]
    [Range(0f, 1f)]
    public float sectorCorrelation = 0.3f;

    [Tooltip("イベント発生時の通知メッセージ")]
    public string notificationMessage;
}

public enum StockEventTriggerType
{
    RandomDaily,            // 毎日ランダムで発生
    OnGachaResult,          // ガチャでこの企業キャラが出た時
    OnUpgradePurchase,      // この企業関連の強化購入時
    OnMilestone,            // 特定マイルストーン達成時
    OnStockPurchase,        // 大量株式購入時（需要増）
    OnStockSell,            // 大量株式売却時（供給増）
    OnMarketCrash,          // 市場暴落時
    OnMarketBoom,           // 市場好況時
    Scheduled               // 特定時間に発生（決算発表など）
}
