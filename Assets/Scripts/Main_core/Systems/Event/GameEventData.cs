using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

/// <summary>
/// ゲームイベントの定義データ（ScriptableObject）
/// 発動条件、表示内容、報酬などを設定
/// </summary>
[CreateAssetMenu(fileName = "New_GameEvent", menuName = "ArknightsClicker/Game Event Data")]
public class GameEventData : ScriptableObject
{
    // ========================================
    // 基本情報
    // ========================================

    [Header("基本情報")]
    [Tooltip("イベントの一意ID（セーブデータで使用）")]
    public string eventId;

    [Tooltip("イベント名（デバッグ/ログ用）")]
    public string eventName;

    [TextArea(2, 4)]
    [Tooltip("イベントの説明")]
    public string description;

    // ========================================
    // 発動条件
    // ========================================

    [Header("発動条件")]
    [Tooltip("トリガーの種類")]
    public EventTriggerType triggerType = EventTriggerType.None;

    [Tooltip("条件達成に必要な数値（LMD額、クリック数など）")]
    public double triggerValue;

    [Tooltip("特定のID指定が必要な場合（強化ID、キャラIDなど）")]
    public string requireId;

    [Tooltip("前提イベント（これが完了していないと発動しない）")]
    public GameEventData prerequisiteEvent;

    // ========================================
    // 発動設定
    // ========================================

    [Header("発動設定")]
    [Tooltip("一度きりのイベントか（false = 条件を満たすたびに発動）")]
    public bool oneTimeOnly = true;

    [Tooltip("発動時にゲームを一時停止するか")]
    public bool pauseGame = false;

    [Tooltip("発動優先度（高いほど先に処理）")]
    public int priority = 0;

    // ========================================
    // 表示設定
    // ========================================

    [Header("表示設定")]
    [Tooltip("イベント発動時に表示するプレハブ（会話UI等）")]
    public GameObject eventPrefab;

    [Tooltip("プレハブを使わない場合の簡易通知テキスト")]
    public string notificationText;

    [Tooltip("通知に使用するアイコン")]
    public Sprite notificationIcon;

    // ========================================
    // 報酬設定
    // ========================================

    [Header("報酬")]
    [Tooltip("解放するメニュー（任意）")]
    public MenuType? unlockMenu;

    [Tooltip("付与するLMD")]
    public double rewardMoney;

    [Tooltip("付与する資格証")]
    public int rewardCertificates;

    [Tooltip("付与するアイテム")]
    public List<ItemReward> rewardItems = new List<ItemReward>();

    // ========================================
    // イベントチャンネル（疎結合通知用）
    // ========================================

    /// <summary>このイベントが発火した時に通知</summary>
    public event Action<GameEventData> OnEventTriggered;

    /// <summary>
    /// イベントを発火させる（EventManagerから呼ばれる）
    /// </summary>
    public void Raise()
    {
        OnEventTriggered?.Invoke(this);
        Debug.Log($"[GameEvent] Raised: {eventName} ({eventId})");
    }

    // ========================================
    // バリデーション
    // ========================================

    private void OnValidate()
    {
        // IDが空なら名前から自動生成
        if (string.IsNullOrEmpty(eventId) && !string.IsNullOrEmpty(name))
        {
            eventId = name.ToLower().Replace(" ", "_");
        }
    }
}

/// <summary>
/// アイテム報酬データ
/// </summary>
[Serializable]
public class ItemReward
{
    [Tooltip("付与するアイテムのID")]
    public string itemId;

    [Tooltip("付与する個数")]
    public int amount = 1;
}
