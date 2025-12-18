using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクターの好感度を管理するマネージャー
/// </summary>
public class AffectionManager : MonoBehaviour
{
    public static AffectionManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private float clickAffectionCooldown = 1f; // クリックで好感度が上がるクールダウン
    [SerializeField] private int clickAffectionAmount = 1;

    [Header("現在のキャラクター")]
    [SerializeField] private CharacterData currentCharacter;

    // 好感度データ（キャラID → 好感度値）
    private Dictionary<string, int> _affectionData = new();

    // クールダウン管理
    private float _lastClickTime;

    // ========================================
    // イベント
    // ========================================

    /// <summary>好感度変化時 (characterId, newValue, delta)</summary>
    public event Action<string, int, int> OnAffectionChanged;

    /// <summary>好感度レベルアップ時 (characterId, newLevel)</summary>
    public event Action<string, AffectionLevel> OnAffectionLevelUp;

    /// <summary>セリフ表示リクエスト (dialogue)</summary>
    public event Action<string> OnDialogueRequested;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ========================================
    // 公開メソッド
    // ========================================

    /// <summary>
    /// 現在のキャラクターを設定
    /// </summary>
    public void SetCurrentCharacter(CharacterData character)
    {
        currentCharacter = character;
    }

    /// <summary>
    /// クリック時の好感度処理
    /// </summary>
    public void OnCharacterClicked()
    {
        if (currentCharacter == null) return;

        // クールダウンチェック
        if (Time.time - _lastClickTime >= clickAffectionCooldown)
        {
            AddAffection(currentCharacter.characterId, clickAffectionAmount);
            _lastClickTime = Time.time;
        }

        // セリフ表示（クールダウン関係なく）
        TriggerDialogue(DialogueType.Click);
    }

    /// <summary>
    /// プレゼントを渡す
    /// </summary>
    public void GiveGift(string itemId)
    {
        if (currentCharacter == null) return;

        // アイテム消費
        if (!GameController.Instance.Inventory.Use(itemId, 1))
        {
            LogUIController.Msg("アイテムが足りません");
            return;
        }

        // 好感度ボーナス取得
        int bonus = currentCharacter.GetGiftBonus(itemId);
        var reaction = GetGiftReaction(itemId);

        // 好感度追加
        AddAffection(currentCharacter.characterId, bonus);

        // 反応セリフ
        switch (reaction)
        {
            case GiftReaction.Love:
            case GiftReaction.Like:
                TriggerDialogue(DialogueType.GiftLiked);
                break;
            case GiftReaction.Dislike:
                TriggerDialogue(DialogueType.GiftDisliked);
                break;
            default:
                TriggerDialogue(DialogueType.Gift);
                break;
        }
    }

    /// <summary>
    /// 好感度を追加
    /// </summary>
    public void AddAffection(string characterId, int amount)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        int oldValue = GetAffection(characterId);
        int oldLevel = GetAffectionLevelIndex(characterId);

        int newValue = Mathf.Clamp(oldValue + amount, 0, GetMaxAffection(characterId));
        _affectionData[characterId] = newValue;

        int newLevel = GetAffectionLevelIndex(characterId);

        // イベント発火
        OnAffectionChanged?.Invoke(characterId, newValue, amount);

        // レベルアップチェック
        if (newLevel > oldLevel && currentCharacter != null)
        {
            var levelData = currentCharacter.GetAffectionLevel(newValue);
            if (levelData != null)
            {
                OnAffectionLevelUp?.Invoke(characterId, levelData);
                TriggerDialogue(DialogueType.LevelUp);
                LogUIController.Msg($"<color=#FFD700>好感度レベルアップ！ → {levelData.levelName}</color>");
            }
        }
    }

    /// <summary>
    /// 好感度を取得
    /// </summary>
    public int GetAffection(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return 0;
        return _affectionData.TryGetValue(characterId, out int value) ? value : 0;
    }

    /// <summary>
    /// 現在のキャラの好感度を取得
    /// </summary>
    public int GetCurrentAffection()
    {
        if (currentCharacter == null) return 0;
        return GetAffection(currentCharacter.characterId);
    }

    /// <summary>
    /// 現在のキャラの好感度レベルを取得
    /// </summary>
    public AffectionLevel GetCurrentAffectionLevel()
    {
        if (currentCharacter == null) return null;
        return currentCharacter.GetAffectionLevel(GetCurrentAffection());
    }

    // ========================================
    // 内部メソッド
    // ========================================

    private int GetMaxAffection(string characterId)
    {
        if (currentCharacter != null && currentCharacter.characterId == characterId)
        {
            return currentCharacter.maxAffection;
        }
        return 200; // デフォルト
    }

    private int GetAffectionLevelIndex(string characterId)
    {
        if (currentCharacter == null || currentCharacter.characterId != characterId)
            return 0;

        var level = currentCharacter.GetAffectionLevel(GetAffection(characterId));
        return level?.level ?? 0;
    }

    private GiftReaction GetGiftReaction(string itemId)
    {
        if (currentCharacter == null) return GiftReaction.Neutral;

        foreach (var pref in currentCharacter.giftPreferences)
        {
            if (pref.itemId == itemId)
            {
                return pref.reaction;
            }
        }
        return GiftReaction.Neutral;
    }

    private void TriggerDialogue(DialogueType type)
    {
        if (currentCharacter == null) return;

        string dialogue = currentCharacter.GetRandomDialogue(GetCurrentAffection(), type);
        if (!string.IsNullOrEmpty(dialogue))
        {
            OnDialogueRequested?.Invoke(dialogue);
        }
    }

    // ========================================
    // セーブ/ロード
    // ========================================

    public Dictionary<string, int> GetSaveData()
    {
        return new Dictionary<string, int>(_affectionData);
    }

    public void LoadSaveData(Dictionary<string, int> data)
    {
        _affectionData.Clear();
        if (data != null)
        {
            foreach (var kvp in data)
            {
                _affectionData[kvp.Key] = kvp.Value;
            }
        }
    }
}
