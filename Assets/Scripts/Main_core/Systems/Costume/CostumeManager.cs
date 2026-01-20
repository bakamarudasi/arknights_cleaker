using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターの衣装（コスチューム）を管理するマネージャー
///
/// 責務:
/// - 衣装の解放状態管理
/// - 装備中の衣装管理（永続化）
/// - 解放条件チェック（好感度 or アイテム）
/// </summary>
public class CostumeManager : BaseSingleton<CostumeManager>
{
    protected override bool Persistent => false;

    // ========================================
    // 定数
    // ========================================

    /// <summary>デフォルト衣装のID</summary>
    public const string DEFAULT_COSTUME_ID = "default";

    /// <summary>最大衣装数</summary>
    public const int MAX_COSTUMES = 3;

    // ========================================
    // データ
    // ========================================

    // 解放済み衣装（キャラID → 解放済み衣装IDセット）
    private Dictionary<string, HashSet<string>> _unlockedCostumes = new();

    // 装備中の衣装（キャラID → 衣装ID）
    private Dictionary<string, string> _equippedCostumes = new();

    // ========================================
    // イベント
    // ========================================

    /// <summary>衣装が解放された時 (characterId, costumeId)</summary>
    public event Action<string, string> OnCostumeUnlocked;

    /// <summary>衣装が装備された時 (characterId, costumeId)</summary>
    public event Action<string, string> OnCostumeEquipped;

    // ========================================
    // 解放状態チェック
    // ========================================

    /// <summary>
    /// 衣装が解放済みかチェック
    /// </summary>
    public bool IsCostumeUnlocked(string characterId, string costumeId)
    {
        if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(costumeId))
            return false;

        // デフォルト衣装は常に解放
        if (costumeId == DEFAULT_COSTUME_ID || costumeId == "normal")
            return true;

        if (!_unlockedCostumes.TryGetValue(characterId, out var unlocked))
            return false;

        return unlocked.Contains(costumeId);
    }

    /// <summary>
    /// 衣装インデックスが解放済みかチェック（0=default, 1=skin1, 2=skin2）
    /// </summary>
    public bool IsCostumeUnlockedByIndex(string characterId, int costumeIndex)
    {
        if (costumeIndex == 0) return true; // デフォルトは常に解放
        string costumeId = GetCostumeIdFromIndex(costumeIndex);
        return IsCostumeUnlocked(characterId, costumeId);
    }

    /// <summary>
    /// 解放済み衣装の一覧を取得
    /// </summary>
    public List<string> GetUnlockedCostumes(string characterId)
    {
        var result = new List<string> { DEFAULT_COSTUME_ID };

        if (_unlockedCostumes.TryGetValue(characterId, out var unlocked))
        {
            result.AddRange(unlocked);
        }

        return result;
    }

    // ========================================
    // 衣装解放
    // ========================================

    /// <summary>
    /// 衣装を解放する
    /// </summary>
    public bool UnlockCostume(string characterId, string costumeId)
    {
        if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(costumeId))
            return false;

        // 既に解放済み
        if (IsCostumeUnlocked(characterId, costumeId))
            return false;

        // 解放
        if (!_unlockedCostumes.ContainsKey(characterId))
        {
            _unlockedCostumes[characterId] = new HashSet<string>();
        }
        _unlockedCostumes[characterId].Add(costumeId);

        OnCostumeUnlocked?.Invoke(characterId, costumeId);

        return true;
    }

    /// <summary>
    /// インデックス指定で衣装を解放
    /// </summary>
    public bool UnlockCostumeByIndex(string characterId, int costumeIndex)
    {
        string costumeId = GetCostumeIdFromIndex(costumeIndex);
        return UnlockCostume(characterId, costumeId);
    }

    /// <summary>
    /// アイテムを使用して衣装を解放
    /// </summary>
    public bool TryUnlockWithItem(string characterId, string costumeId, ItemData item)
    {
        if (item == null) return false;

        // 既に解放済み
        if (IsCostumeUnlocked(characterId, costumeId))
        {
            LogUIController.Msg("この衣装は既に解放済みです");
            return false;
        }

        // アイテム消費
        var inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.Use(item.id, 1))
        {
            LogUIController.Msg("アイテムが足りません");
            return false;
        }

        // 解放
        UnlockCostume(characterId, costumeId);
        LogUIController.Msg($"<color=#FFD700>新しい衣装を解放しました！</color>");

        return true;
    }

    /// <summary>
    /// 衣装解放アイテムを使用（ItemDataのtargetCharacterId/targetCostumeIndexを使用）
    /// </summary>
    public bool UseCostumeUnlockItem(ItemData item)
    {
        if (item == null || item.type != ItemData.ItemType.CostumeUnlock)
        {
            Debug.LogWarning("[CostumeManager] Invalid costume unlock item");
            return false;
        }

        string costumeId = GetCostumeIdFromIndex(item.targetCostumeIndex);
        return TryUnlockWithItem(item.targetCharacterId, costumeId, item);
    }

    // ========================================
    // 衣装装備
    // ========================================

    /// <summary>
    /// 衣装を装備する
    /// </summary>
    public bool EquipCostume(string characterId, string costumeId)
    {
        if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(costumeId))
            return false;

        // 未解放チェック
        if (!IsCostumeUnlocked(characterId, costumeId))
        {
            Debug.LogWarning($"[CostumeManager] Cannot equip locked costume: {characterId} / {costumeId}");
            return false;
        }

        // 既に装備中
        if (GetEquippedCostume(characterId) == costumeId)
            return true;

        // 装備
        _equippedCostumes[characterId] = costumeId;

        OnCostumeEquipped?.Invoke(characterId, costumeId);

        return true;
    }

    /// <summary>
    /// インデックス指定で衣装を装備
    /// </summary>
    public bool EquipCostumeByIndex(string characterId, int costumeIndex)
    {
        string costumeId = GetCostumeIdFromIndex(costumeIndex);
        return EquipCostume(characterId, costumeId);
    }

    /// <summary>
    /// 現在装備中の衣装IDを取得
    /// </summary>
    public string GetEquippedCostume(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return DEFAULT_COSTUME_ID;

        return _equippedCostumes.TryGetValue(characterId, out var costumeId)
            ? costumeId
            : DEFAULT_COSTUME_ID;
    }

    /// <summary>
    /// 現在装備中の衣装インデックスを取得
    /// </summary>
    public int GetEquippedCostumeIndex(string characterId)
    {
        string costumeId = GetEquippedCostume(characterId);
        return GetIndexFromCostumeId(costumeId);
    }

    // ========================================
    // 好感度連動解放
    // ========================================

    /// <summary>
    /// 好感度レベルに応じた衣装解放をチェック・実行
    /// AffectionManager.OnAffectionLevelUp から呼び出す
    /// </summary>
    public void CheckAffectionUnlock(string characterId, AffectionLevel level)
    {
        if (level == null || !level.unlocksNewOutfit) return;

        string costumeId = GetCostumeIdFromIndex(level.outfitIndex);

        if (UnlockCostume(characterId, costumeId))
        {
            LogUIController.Msg($"<color=#FFD700>好感度レベルアップで新衣装解放！</color>");
        }
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// インデックスから衣装IDを取得
    /// </summary>
    public static string GetCostumeIdFromIndex(int index)
    {
        return index switch
        {
            0 => "default",
            1 => "skin1",
            2 => "skin2",
            _ => $"skin{index}"
        };
    }

    /// <summary>
    /// 衣装IDからインデックスを取得
    /// </summary>
    public static int GetIndexFromCostumeId(string costumeId)
    {
        return costumeId switch
        {
            "default" or "normal" => 0,
            "skin1" => 1,
            "skin2" => 2,
            _ when costumeId.StartsWith("skin") && int.TryParse(costumeId.Substring(4), out int idx) => idx,
            _ => 0
        };
    }

    /// <summary>
    /// 衣装IDをシーンIDに変換（CharacterSceneData用）
    /// </summary>
    public static string CostumeIdToSceneId(string costumeId)
    {
        // 衣装IDとシーンIDが同じ想定
        // 必要に応じてマッピングを追加
        return costumeId switch
        {
            "default" => "normal",
            _ => costumeId
        };
    }

    /// <summary>
    /// 後方互換性のためのエイリアス
    /// </summary>
    public static string CostumeIdToPoseId(string costumeId) => CostumeIdToSceneId(costumeId);

    // ========================================
    // セーブ/ロード
    // ========================================

    /// <summary>
    /// セーブデータを取得
    /// </summary>
    public CostumeSaveData GetSaveData()
    {
        var data = new CostumeSaveData();

        // 解放済み衣装
        foreach (var kvp in _unlockedCostumes)
        {
            data.unlockedCostumes[kvp.Key] = new List<string>(kvp.Value);
        }

        // 装備中衣装
        foreach (var kvp in _equippedCostumes)
        {
            data.equippedCostumes[kvp.Key] = kvp.Value;
        }

        return data;
    }

    /// <summary>
    /// セーブデータを読み込み
    /// </summary>
    public void LoadSaveData(CostumeSaveData data)
    {
        _unlockedCostumes.Clear();
        _equippedCostumes.Clear();

        if (data == null) return;

        // 解放済み衣装
        foreach (var kvp in data.unlockedCostumes)
        {
            _unlockedCostumes[kvp.Key] = new HashSet<string>(kvp.Value);
        }

        // 装備中衣装
        foreach (var kvp in data.equippedCostumes)
        {
            _equippedCostumes[kvp.Key] = kvp.Value;
        }
    }
}

/// <summary>
/// 衣装データのセーブ用クラス
/// </summary>
[Serializable]
public class CostumeSaveData
{
    public Dictionary<string, List<string>> unlockedCostumes = new();
    public Dictionary<string, string> equippedCostumes = new();
}
