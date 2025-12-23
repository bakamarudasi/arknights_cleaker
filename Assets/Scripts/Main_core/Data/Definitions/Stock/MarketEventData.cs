using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// グローバル市場イベント（CompanyDataとは別で管理）
/// </summary>
[CreateAssetMenu(fileName = "New_MarketEvent", menuName = "ArknightsClicker/Stock/Market Event")]
public class MarketEventData : ScriptableObject
{
    [Header("イベント基本情報")]
    public string eventId;
    public string eventName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("影響設定")]
    [Tooltip("全体市場への影響 (-0.2 = 20%下落)")]
    [Range(-0.5f, 0.5f)]
    public float globalImpact = 0f;

    [Tooltip("特定セクターへの追加影響")]
    public List<SectorImpact> sectorImpacts = new();

    [Tooltip("特定企業への追加影響")]
    public List<CompanyImpact> companyImpacts = new();

    [Header("発生条件")]
    [Tooltip("発生確率（1日あたり）")]
    [Range(0f, 1f)]
    public float dailyProbability = 0.05f;

    [Tooltip("影響持続時間（秒）")]
    public float durationSeconds = 600f;

    [Tooltip("イベントの重大度")]
    public EventSeverity severity = EventSeverity.Normal;
}

public enum EventSeverity
{
    Minor,      // 小規模（±5%程度）
    Normal,     // 通常（±10%程度）
    Major,      // 大規模（±20%程度）
    Critical    // 危機的（±30%以上）
}

[Serializable]
public class SectorImpact
{
    public CompanyData.StockSector sector;
    [Range(-0.5f, 0.5f)]
    public float impact;
}

[Serializable]
public class CompanyImpact
{
    public CompanyData company;
    [Range(-0.5f, 0.5f)]
    public float impact;
}
