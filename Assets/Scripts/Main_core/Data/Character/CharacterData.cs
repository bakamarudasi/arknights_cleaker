using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターデータ（ScriptableObject）
/// キャラクターの全情報を統合管理する主データ
///
/// 構成:
/// - 基本情報（ID, 名前, 説明）
/// - シーン一覧（衣装別・イベント別のプレハブとUI設定）
/// - 好感度設定
/// - セリフ設定（共通）
/// - プレゼント好み
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Arknights/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("=== 基本情報 ===")]
    public string characterId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite thumbnail;

    [Header("=== シーン設定 ===")]
    [Tooltip("利用可能なシーン一覧（衣装別・イベント別）")]
    public List<CharacterSceneData> scenes = new();

    [Tooltip("デフォルトで表示するシーンID")]
    public string defaultSceneId = "default";

    [Header("=== 好感度設定 ===")]
    public int maxAffection = 200;
    public List<AffectionLevel> affectionLevels = new();

    [Header("=== セリフ設定（共通）===")]
    public List<DialogueGroup> dialogueGroups = new();

    [Header("=== プレゼント設定 ===")]
    public List<GiftPreference> giftPreferences = new();

    // ========================================
    // シーン関連ヘルパーメソッド
    // ========================================

    /// <summary>
    /// シーンIDからシーンデータを取得
    /// </summary>
    public CharacterSceneData GetScene(string sceneId)
    {
        return scenes.Find(s => s != null && s.sceneId == sceneId);
    }

    /// <summary>
    /// デフォルトシーンを取得
    /// </summary>
    public CharacterSceneData GetDefaultScene()
    {
        var scene = GetScene(defaultSceneId);
        if (scene == null && scenes.Count > 0)
        {
            scene = scenes[0];
        }
        return scene;
    }

    /// <summary>
    /// 利用可能（アンロック済み）なシーン一覧を取得
    /// </summary>
    public List<CharacterSceneData> GetUnlockedScenes(int currentAffectionLevel, Func<string, bool> hasItem = null)
    {
        return scenes.FindAll(s => s != null && s.IsUnlocked(currentAffectionLevel, hasItem));
    }

    /// <summary>
    /// シーンが存在するか確認
    /// </summary>
    public bool HasScene(string sceneId)
    {
        return scenes.Exists(s => s != null && s.sceneId == sceneId);
    }

    /// <summary>
    /// シーン数を取得
    /// </summary>
    public int SceneCount => scenes.Count;

    /// <summary>
    /// インデックスからシーンを取得（衣装ボタン互換用）
    /// </summary>
    public CharacterSceneData GetSceneByIndex(int index)
    {
        if (index < 0 || index >= scenes.Count) return null;
        return scenes[index];
    }

    // ========================================
    // 好感度関連ヘルパーメソッド
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
