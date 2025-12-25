using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 会話の表示モード
/// </summary>
public enum ConversationDisplayMode
{
    /// <summary>全画面会話UI（従来の表示）</summary>
    OverlayUI,
    /// <summary>立ち絵画面 + 吹き出し</summary>
    SpeechBubble,
}

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

    [Header("表示モード")]
    [Tooltip("会話のデフォルト表示モード")]
    public ConversationDisplayMode defaultDisplayMode = ConversationDisplayMode.OverlayUI;

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

    [Header("表示モードオーバーライド")]
    [Tooltip("この行だけ表示モードを変更する")]
    public bool overrideDisplayMode = false;

    [Tooltip("オーバーライド時の表示モード")]
    public ConversationDisplayMode displayModeOverride = ConversationDisplayMode.OverlayUI;

    [Header("シーン・アニメーション切り替え")]
    [Tooltip("この行でシーン（立ち絵）を切り替える（CharacterSceneDataのsceneId）")]
    public string changeToSceneId = "";

    [Tooltip("この行でアニメーションを変更する（Spineアニメーション名）")]
    public string animationTrigger = "";

    [Tooltip("吹き出しモード時のアンカー位置（Spineボーン名: head, mouth など）")]
    public string bubbleAnchor = "head";

    [Header("チュートリアル用")]
    [Tooltip("ハイライトするUI要素名（チュートリアル用）")]
    public string highlightElement;

    [Tooltip("ダイアログの表示位置")]
    public DialogPosition dialogPosition = DialogPosition.Bottom;

    /// <summary>
    /// この行の実際の表示モードを取得
    /// </summary>
    public ConversationDisplayMode GetDisplayMode(ConversationDisplayMode defaultMode)
    {
        return overrideDisplayMode ? displayModeOverride : defaultMode;
    }
}

/// <summary>
/// ダイアログボックスの表示位置
/// </summary>
public enum DialogPosition
{
    Bottom,
    Top,
    Center
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
