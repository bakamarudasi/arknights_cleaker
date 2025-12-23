using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 株主総会の評価結果
/// </summary>
[CreateAssetMenu(fileName = "New_MeetingResult", menuName = "ArknightsClicker/Stock/Shareholder Meeting Result")]
public class ShareholderMeetingResult : ScriptableObject
{
    [Header("評価条件")]
    [Tooltip("この評価が適用される株価の下限")]
    public float minStockPrice = 0f;

    [Tooltip("この評価が適用される株価の上限")]
    public float maxStockPrice = 100f;

    [Header("評価内容")]
    public MeetingGrade grade;

    [Tooltip("評価者（ケルシーなど）")]
    public string evaluatorName = "ケルシー";

    [Tooltip("評価コメント")]
    [TextArea]
    public string comment;

    [Tooltip("追加の演出メッセージ（Mon3tr召喚など）")]
    public string extraMessage;

    [Header("効果")]
    [Tooltip("報酬/罰則の種類")]
    public List<MeetingEffect> effects = new();
}

public enum MeetingGrade
{
    Excellent,  // 優秀：株価が非常に高い
    Good,       // 良好：株価が高め
    Normal,     // 普通：株価が平均的
    Poor,       // 不調：株価が低め
    Critical    // 危機的：株価が暴落状態
}

[Serializable]
public class MeetingEffect
{
    public MeetingEffectType effectType;
    public float value;
    public float durationSeconds; // 0で永続（次の総会まで）
}

public enum MeetingEffectType
{
    // 報酬系
    BonusLMD,               // LMDボーナス
    FacilityBoost,          // 施設効率アップ
    ClickEfficiencyBoost,   // クリック効率アップ
    GachaTicket,            // ガチャチケット付与

    // 罰則系
    FacilityDebuff,         // 施設効率ダウン（士気低下）
    ClickEfficiencyDebuff,  // クリック効率ダウン
    IncomeReduction         // 収入減少
}
