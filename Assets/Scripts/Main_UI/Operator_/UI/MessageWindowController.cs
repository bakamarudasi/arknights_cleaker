using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// メッセージウィンドウの表示制御
/// セリフをフェードイン/アウトで表示
/// </summary>
public class MessageWindowController : IDisposable
{
    private VisualElement _msgWindow;
    private Label _msgSpeaker;
    private Label _msgText;

    private float _displayDuration = 3f;
    private IVisualElementScheduledItem _hideSchedule;

    // ========================================
    // 初期化
    // ========================================

    public void Initialize(VisualElement root)
    {
        _msgWindow = root.Q<VisualElement>("msg-window");
        _msgSpeaker = root.Q<Label>("msg-speaker");
        _msgText = root.Q<Label>("msg-text");

        // 初期状態は非表示
        Hide();
    }

    // ========================================
    // 表示制御
    // ========================================

    /// <summary>
    /// メッセージを表示
    /// </summary>
    public void ShowMessage(string text, string speaker = null, float duration = 3f)
    {
        if (_msgWindow == null || _msgText == null) return;

        // 既存のスケジュールをキャンセル
        _hideSchedule?.Pause();

        // テキスト設定
        _msgText.text = text;

        if (_msgSpeaker != null)
        {
            if (!string.IsNullOrEmpty(speaker))
            {
                _msgSpeaker.text = speaker;
                _msgSpeaker.style.display = DisplayStyle.Flex;
            }
            else
            {
                _msgSpeaker.style.display = DisplayStyle.None;
            }
        }

        // 表示
        _msgWindow.AddToClassList("active");

        // 自動非表示のスケジュール
        _displayDuration = duration;
        _hideSchedule = _msgWindow.schedule.Execute(Hide).StartingIn((long)(duration * 1000));
    }

    /// <summary>
    /// メッセージを非表示
    /// </summary>
    public void Hide()
    {
        _hideSchedule?.Pause();
        _msgWindow?.RemoveFromClassList("active");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        _hideSchedule?.Pause();
        _hideSchedule = null;
    }
}
