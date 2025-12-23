using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターデータ（ScriptableObject）
/// 好感度、セリフ、立ち絵、衣装（ポーズ）などを統合管理
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Arknights/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public string characterId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite characterThumbnail;

    [Header("=== ポーズ（衣装）設定 ===")]
    [Tooltip("利用可能なポーズ一覧")]
    public List<PoseEntry> poses = new List<PoseEntry>();

    [Tooltip("デフォルトで表示するポーズID")]
    public string defaultPoseId = "normal";

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

    // ========================================
    // ポーズ関連メソッド
    // ========================================

    /// <summary>
    /// ポーズIDからPoseEntryを取得
    /// </summary>
    public PoseEntry GetPose(string poseId)
    {
        return poses.Find(p => p.poseId == poseId);
    }

    /// <summary>
    /// デフォルトポーズを取得
    /// </summary>
    public PoseEntry GetDefaultPose()
    {
        var pose = GetPose(defaultPoseId);
        if (pose == null && poses.Count > 0)
        {
            pose = poses[0];
        }
        return pose;
    }

    /// <summary>
    /// 利用可能（アンロック済み）なポーズ一覧を取得
    /// </summary>
    public List<PoseEntry> GetUnlockedPoses(int currentAffectionLevel = 999)
    {
        return poses.FindAll(p => !p.isLocked || p.requiredAffectionLevel <= currentAffectionLevel);
    }

    /// <summary>
    /// ポーズが存在するか確認
    /// </summary>
    public bool HasPose(string poseId)
    {
        return poses.Exists(p => p.poseId == poseId);
    }

    /// <summary>
    /// ポーズ数を取得
    /// </summary>
    public int PoseCount => poses.Count;
}

/// <summary>
/// ポーズ1件分のデータ
/// </summary>
[Serializable]
public class PoseEntry
{
    [Tooltip("ポーズ識別ID（例: normal, swimsuit, casual）")]
    public string poseId;

    [Tooltip("UI表示名（例: 通常, 水着, 私服）")]
    public string displayName;

    [Tooltip("このポーズ用のプレハブ（PSBキャラ）")]
    public GameObject prefab;

    [Tooltip("ポーズ選択UI用のサムネイル")]
    public Sprite thumbnail;

    [Tooltip("推奨カメラサイズ（0で自動調整）")]
    public float recommendedCameraSize = 0f;

    [Tooltip("解放条件（将来の拡張用）")]
    public bool isLocked = false;

    [Tooltip("解放に必要な好感度レベル（将来の拡張用）")]
    public int requiredAffectionLevel = 0;

    [Header("=== UI設定 ===")]
    [Tooltip("サイドパネルを非表示にする（フルスクリーンモード）")]
    public bool hideSidePanel = false;

    [Tooltip("戻るボタンを非表示にする")]
    public bool hideBackButton = false;

    [Tooltip("カスタムシーンUIのタイプ（空の場合はデフォルト）")]
    public string sceneUIType = "";

    [Header("=== 会話設定 ===")]
    [Tooltip("このポーズで利用可能な会話リスト")]
    public List<PoseConversation> conversations = new List<PoseConversation>();

    /// <summary>
    /// 解放済みの会話を取得
    /// </summary>
    public List<PoseConversation> GetUnlockedConversations(int currentAffectionLevel)
    {
        return conversations.FindAll(c => c.requiredAffectionLevel <= currentAffectionLevel);
    }

    /// <summary>
    /// ランダム雑談用の会話を取得
    /// </summary>
    public List<PoseConversation> GetRandomTalks(int currentAffectionLevel)
    {
        return conversations.FindAll(c => c.isRandomTalk && c.requiredAffectionLevel <= currentAffectionLevel);
    }
}

/// <summary>
/// ポーズごとの会話データ
/// </summary>
[Serializable]
public class PoseConversation
{
    [Tooltip("会話タイトル（UIに表示）")]
    public string title;

    [Tooltip("会話データ")]
    public ConversationData conversationData;

    [Tooltip("解放に必要な好感度レベル")]
    public int requiredAffectionLevel = 0;

    [Tooltip("ランダム雑談として使用するか")]
    public bool isRandomTalk = false;

    [Tooltip("一度だけ再生する（イベント会話用）")]
    public bool playOnce = false;
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
