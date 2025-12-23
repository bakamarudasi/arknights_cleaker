using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// チュートリアルステップの定義
/// </summary>
[Serializable]
public class TutorialStep
{
    public string id;
    public string title;
    [TextArea(2, 4)]
    public string message;
    public string highlightElement;  // ハイライトするUI要素名（オプション）
    public TutorialPosition position = TutorialPosition.Center;
}

/// <summary>
/// チュートリアル表示位置
/// </summary>
public enum TutorialPosition
{
    Center,
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// チュートリアルシーケンスのScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New_Tutorial", menuName = "ArknightsClicker/Tutorial Sequence")]
public class TutorialSequenceData : ScriptableObject
{
    [Header("シーケンス情報")]
    public string sequenceId;
    public string sequenceName;

    [Header("ステップ")]
    public List<TutorialStep> steps = new();
}
