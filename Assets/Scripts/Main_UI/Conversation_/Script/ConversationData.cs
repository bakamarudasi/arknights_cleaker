using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 会話イベントのデータ定義（ScriptableObject）
/// 複数のダイアログ行を持つ会話シーケンスを定義
/// </summary>
[CreateAssetMenu(fileName = "New_Conversation", menuName = "ArknightsClicker/Conversation Data")]
public class ConversationData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("会話の識別ID")]
    public string conversationId;

    [Tooltip("会話のタイトル（デバッグ用）")]
    public string title;

    [Header("ダイアログ")]
    [Tooltip("会話の各行")]
    public List<DialogLine> lines = new List<DialogLine>();

    [Header("表示設定")]
    [Tooltip("タイプライター演出の速度（1文字あたりのミリ秒）")]
    public int typewriterSpeed = 30;

    [Tooltip("自動で次に進む（falseならタップ待ち）")]
    public bool autoAdvance = false;

    [Tooltip("自動進行時の待機時間（秒）")]
    public float autoAdvanceDelay = 2f;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(conversationId) && !string.IsNullOrEmpty(name))
        {
            conversationId = name.ToLower().Replace(" ", "_");
        }
    }
}

/// <summary>
/// 会話の1行分のデータ
/// </summary>
[Serializable]
public class DialogLine
{
    [Header("話者")]
    [Tooltip("話者の名前")]
    public string speakerName;

    [Tooltip("話者の立ち位置")]
    public SpeakerPosition position = SpeakerPosition.Left;

    [Tooltip("話者の立ち絵（null = 非表示）")]
    public Sprite characterSprite;

    [Header("テキスト")]
    [TextArea(2, 5)]
    [Tooltip("セリフ内容")]
    public string text;

    [Header("演出")]
    [Tooltip("表示時に再生するSE")]
    public AudioClip soundEffect;

    [Tooltip("表情変更（キャラに複数表情がある場合）")]
    public int expressionIndex = 0;
}

/// <summary>
/// キャラクターの立ち位置
/// </summary>
public enum SpeakerPosition
{
    Left,
    Right,
    Center,
    None  // 立ち絵なし（ナレーション等）
}
