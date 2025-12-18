using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターデータ（ScriptableObject）
/// 好感度、セリフ、立ち絵などを管理
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Arknights/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public string characterId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;

    [Header("Prefab")]
    public GameObject psbPrefab;

    [Header("好感度設定")]
    public int maxAffection = 200;
    public List<AffectionLevel> affectionLevels = new();

    [Header("セリフ設定")]
    public List<DialogueGroup> dialogueGroups = new();

    [Header("プレゼントアイテム")]
    public List<GiftPreference> giftPreferences = new();

    // ========================================
    // ヘルパーメソッド
    // ========================================

    /// <summary>
    /// 現在の好感度からレベルを取得
    /// </summary>
    public AffectionLevel GetAffectionLevel(int affection)
    {
        AffectionLevel current = null;
        foreach (var level in affectionLevels)
        {
            if (affection >= level.requiredAffection)
            {
                current = level;
            }
            else
            {
                break;
            }
        }
        return current;
    }

    /// <summary>
    /// 指定好感度レベルで利用可能なセリフを取得
    /// </summary>
    public List<string> GetAvailableDialogues(int affection, DialogueType type)
    {
        var result = new List<string>();
        var currentLevel = GetAffectionLevel(affection);
        if (currentLevel == null) return result;

        foreach (var group in dialogueGroups)
        {
            if (group.dialogueType == type && group.requiredAffectionLevel <= currentLevel.level)
            {
                result.AddRange(group.dialogues);
            }
        }
        return result;
    }

    /// <summary>
    /// ランダムなセリフを取得
    /// </summary>
    public string GetRandomDialogue(int affection, DialogueType type)
    {
        var dialogues = GetAvailableDialogues(affection, type);
        if (dialogues.Count == 0) return null;
        return dialogues[UnityEngine.Random.Range(0, dialogues.Count)];
    }

    /// <summary>
    /// アイテムの好感度ボーナスを取得
    /// </summary>
    public int GetGiftBonus(string itemId)
    {
        foreach (var pref in giftPreferences)
        {
            if (pref.itemId == itemId)
            {
                return pref.affectionBonus;
            }
        }
        return 1; // デフォルト+1
    }
}

/// <summary>
/// 好感度レベル定義
/// </summary>
[Serializable]
public class AffectionLevel
{
    public int level;
    public string levelName; // "知り合い", "友人", "親友", etc.
    public int requiredAffection;

    [Header("解放要素")]
    public bool unlocksNewOutfit;
    public int outfitIndex;
    public bool unlocksSpecialDialogue;
}

/// <summary>
/// セリフタイプ
/// </summary>
public enum DialogueType
{
    Click,          // クリック時
    Idle,           // 放置時
    LevelUp,        // 好感度レベルアップ時
    Gift,           // プレゼント時
    GiftLiked,      // 好きなプレゼント時
    GiftDisliked,   // 嫌いなプレゼント時
    Morning,        // 朝
    Afternoon,      // 昼
    Evening,        // 夜
    Special         // 特別イベント
}

/// <summary>
/// セリフグループ
/// </summary>
[Serializable]
public class DialogueGroup
{
    public DialogueType dialogueType;
    public int requiredAffectionLevel; // このレベル以上で解放

    [TextArea(1, 3)]
    public List<string> dialogues = new();
}

/// <summary>
/// プレゼント好み
/// </summary>
[Serializable]
public class GiftPreference
{
    public string itemId;
    public GiftReaction reaction;
    public int affectionBonus;
}

/// <summary>
/// プレゼント反応
/// </summary>
public enum GiftReaction
{
    Neutral,    // 普通 (+1)
    Like,       // 好き (+3)
    Love,       // 大好き (+5)
    Dislike     // 嫌い (+0 or -1)
}
