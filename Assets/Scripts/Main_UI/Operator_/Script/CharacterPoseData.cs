using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// キャラクターのポーズデータ (ScriptableObject)
///
/// 使い方:
/// 1. Project右クリック → Create → Character → PoseData
/// 2. キャラID、名前を設定
/// 3. posesにポーズを追加（poseId, displayName, prefab, thumbnail）
/// 4. defaultPoseIdを設定
/// 5. OverlayCharacterPresenter.LoadCharacter(poseData) で読み込み
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterPoseData", menuName = "Character/PoseData")]
public class CharacterPoseData : ScriptableObject
{
    [Header("=== キャラクター基本情報 ===")]
    [Tooltip("キャラクター識別ID（例: amiya, exusiai）")]
    public string characterId;

    [Tooltip("表示名（例: アーミヤ、エクシア）")]
    public string characterName;

    [Tooltip("キャラクターのサムネイル（オプション）")]
    public Sprite characterThumbnail;

    [Header("=== ポーズ設定 ===")]
    [Tooltip("利用可能なポーズ一覧")]
    public List<PoseEntry> poses = new List<PoseEntry>();

    [Tooltip("デフォルトで表示するポーズID")]
    public string defaultPoseId = "normal";

    /// <summary>
    /// ポーズ1件分のデータ
    /// </summary>
    [System.Serializable]
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
    }

    // ========================================
    // ヘルパーメソッド
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
