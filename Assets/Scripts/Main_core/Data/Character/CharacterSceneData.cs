using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターシーンデータ（ScriptableObject）
/// 衣装別・イベント別のシーン情報を管理
///
/// 使い方:
/// 1. Project右クリック → Create → Arknights/Character Scene
/// 2. sceneId, displayName, prefab を設定
/// 3. CharacterData.scenes に追加
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterScene", menuName = "Arknights/Character Scene")]
public class CharacterSceneData : ScriptableObject
{
    [Header("=== 基本情報 ===")]
    [Tooltip("シーン識別ID（例: default, swimsuit, event_summer）")]
    public string sceneId;

    [Tooltip("UI表示名（例: 通常, 水着, 夏イベント）")]
    public string displayName;

    [Tooltip("このシーン用のPSBプレハブ")]
    public GameObject prefab;

    [Tooltip("シーン選択UI用のサムネイル")]
    public Sprite thumbnail;

    [Header("=== カメラ設定 ===")]
    [Tooltip("推奨カメラサイズ（0で自動調整）")]
    public float recommendedCameraSize = 0f;

    [Header("=== 解放条件 ===")]
    [Tooltip("初期状態でロックされているか")]
    public bool isLocked = false;

    [Tooltip("解放に必要な好感度レベル（0 = 条件なし）")]
    public int requiredAffectionLevel = 0;

    [Tooltip("解放に必要なアイテムID（空 = 条件なし）")]
    public string requiredItemId;

    [Header("=== UI設定 ===")]
    [Tooltip("サイドパネルを非表示にする（フルスクリーンモード）")]
    public bool hideSidePanel = false;

    [Tooltip("戻るボタンを非表示にする")]
    public bool hideBackButton = false;

    [Tooltip("カスタムシーンUIのタイプ（空の場合はデフォルト）")]
    public string sceneUIType = "";

    [Header("=== シーン専用会話 ===")]
    [Tooltip("このシーンで利用可能な会話リスト")]
    public List<SceneConversation> conversations = new List<SceneConversation>();

    [Header("=== シーン専用セリフ（オプション）===")]
    [Tooltip("シーン専用のセリフ（空の場合はCharacterDataの共通セリフを使用）")]
    public List<DialogueGroup> dialogueGroups = new List<DialogueGroup>();

    [Header("=== ズームアップ設定 ===")]
    [Tooltip("ゾーンタッチ時のズームアップ窓設定")]
    public List<ZoomTargetConfig> zoomTargets = new List<ZoomTargetConfig>();

    [Header("=== 反応セリフ ===")]
    [Tooltip("プレゼント時の反応（triggerId = アイテムID）")]
    public List<ReactionDialogue> giftReactions = new List<ReactionDialogue>();

    [Tooltip("衣装変更時の反応（triggerId = 変更先のsceneId）")]
    public List<ReactionDialogue> costumeReactions = new List<ReactionDialogue>();

    // ========================================
    // ヘルパーメソッド
    // ========================================

    /// <summary>
    /// このシーンに専用セリフがあるか
    /// </summary>
    public bool HasCustomDialogues => dialogueGroups != null && dialogueGroups.Count > 0;

    /// <summary>
    /// ズームターゲット設定があるか
    /// </summary>
    public bool HasZoomTargets => zoomTargets != null && zoomTargets.Count > 0;

    /// <summary>
    /// 指定ゾーンのズーム設定を取得
    /// </summary>
    public ZoomTargetConfig GetZoomConfig(CharacterInteractionZone.ZoneType zone)
    {
        if (zoomTargets == null) return null;
        return zoomTargets.Find(z => z.triggerZone == zone);
    }

    /// <summary>
    /// シーン専用のセリフを取得（好感度レベル考慮）
    /// </summary>
    public List<string> GetDialogues(int affectionLevel, DialogueType type)
    {
        var result = new List<string>();
        if (dialogueGroups == null) return result;

        foreach (var group in dialogueGroups)
        {
            if (group.dialogueType == type && group.requiredAffectionLevel <= affectionLevel)
            {
                result.AddRange(group.dialogues);
            }
        }
        return result;
    }

    /// <summary>
    /// シーン専用のランダムセリフを取得
    /// </summary>
    public string GetRandomDialogue(int affectionLevel, DialogueType type)
    {
        var dialogues = GetDialogues(affectionLevel, type);
        if (dialogues.Count == 0) return null;
        return dialogues[UnityEngine.Random.Range(0, dialogues.Count)];
    }

    /// <summary>
    /// 解放済みの会話を取得
    /// </summary>
    public List<SceneConversation> GetUnlockedConversations(int currentAffectionLevel)
    {
        return conversations.FindAll(c => c.requiredAffectionLevel <= currentAffectionLevel);
    }

    /// <summary>
    /// ランダム雑談用の会話を取得
    /// </summary>
    public List<SceneConversation> GetRandomTalks(int currentAffectionLevel)
    {
        return conversations.FindAll(c => c.isRandomTalk && c.requiredAffectionLevel <= currentAffectionLevel);
    }

    /// <summary>
    /// このシーンがアンロック済みかチェック
    /// </summary>
    public bool IsUnlocked(int currentAffectionLevel, Func<string, bool> hasItem = null)
    {
        if (!isLocked) return true;

        // 好感度レベル条件
        if (requiredAffectionLevel > 0 && currentAffectionLevel < requiredAffectionLevel)
        {
            return false;
        }

        // アイテム条件
        if (!string.IsNullOrEmpty(requiredItemId) && hasItem != null)
        {
            if (!hasItem(requiredItemId)) return false;
        }

        return true;
    }

    /// <summary>
    /// プレゼント時の反応を取得
    /// </summary>
    public ReactionDialogue GetGiftReaction(string itemId, int currentAffection)
    {
        if (giftReactions == null) return null;
        return giftReactions.Find(r =>
            r.triggerId == itemId && r.requiredAffection <= currentAffection);
    }

    /// <summary>
    /// 衣装変更時の反応を取得
    /// </summary>
    public ReactionDialogue GetCostumeReaction(string costumeId, int currentAffection)
    {
        if (costumeReactions == null) return null;
        return costumeReactions.Find(r =>
            r.triggerId == costumeId && r.requiredAffection <= currentAffection);
    }
}

/// <summary>
/// シーンごとの会話データ
/// </summary>
[Serializable]
public class SceneConversation
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

    [Tooltip("Hシーンかどうか（専用演出で再生）")]
    public bool isHScene = false;
}

/// <summary>
/// プレゼント・衣装変更時などの反応セリフ
/// </summary>
[Serializable]
public class ReactionDialogue
{
    [Tooltip("トリガーID（アイテムID or 衣装ID）")]
    public string triggerId;

    [Tooltip("反応セリフ（複数行対応）")]
    [TextArea(1, 3)]
    public string[] lines;

    [Tooltip("この反応に必要な好感度（0 = 条件なし）")]
    public int requiredAffection = 0;
}
