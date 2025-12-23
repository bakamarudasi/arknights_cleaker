using System;
using UnityEngine;

/// <summary>
/// 経営権ボーナス（ロドス株専用）：保有比率に応じた特殊効果
/// </summary>
[Serializable]
public class OwnershipBonus
{
    [Tooltip("必要な保有率 (0.51 = 51%で過半数)")]
    [Range(0.01f, 1f)]
    public float requiredOwnership = 0.51f;

    [Tooltip("ボーナスの種類")]
    public OwnershipBonusType bonusType;

    [Tooltip("効果量")]
    public float effectValue = 0.1f;

    [Tooltip("ボーナスの説明")]
    public string description;

    [Tooltip("喪失時の演出メッセージ")]
    public string lostMessage;
}

public enum OwnershipBonusType
{
    ClickEfficiencyBase,    // クリック効率の基本値
    AllFacilityBoost,       // 全施設効率アップ
    GachaDiscount,          // ガチャコスト割引
    AutoClickUnlock,        // オートクリック解除
    UICustomization,        // UI変更権限（乗っ取られ演出防止）
    DoctorTitle,            // ドクター称号表示
    KelseySupportBonus      // ケルシーサポートボーナス
}
