using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// メッセージウィンドウの表示制御
/// セリフをタイプライター効果で表示
/// </summary>
public class MessageWindowController : IDisposable
{
    private VisualElement _msgWindow;
    private Label _msgSpeaker;
    private Label _msgText;

    private IVisualElementScheduledItem _hideSchedule;
    private IVisualElementScheduledItem _typewriterSchedule;

    // タイプライター設定
    private string _fullText;
    private int _currentCharIndex;
    private float _charInterval = 0.03f; // 1文字あたりの間隔（秒）
    private bool _isTyping;

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
    /// メッセージをタイプライター効果で表示
    /// </summary>
    /// <param name="text">表示するテキスト</param>
    /// <param name="speaker">話者名（nullで非表示）</param>
    /// <param name="duration">表示完了後の表示時間</param>
    /// <param name="useTypewriter">タイプライター効果を使うか</param>
    public void ShowMessage(string text, string speaker = null, float duration = 3f, bool useTypewriter = true)
    {
        if (_msgWindow == null || _msgText == null) return;

        // 既存のスケジュールをキャンセル
        StopAllSchedules();

        // 話者設定
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

        if (useTypewriter && !string.IsNullOrEmpty(text))
        {
            // タイプライター開始
            StartTypewriter(text, duration);
        }
        else
        {
            // 即時表示
            _msgText.text = text;
            ScheduleHide(duration);
        }
    }

    /// <summary>
    /// タイプライター効果を開始
    /// </summary>
    private void StartTypewriter(string text, float durationAfterComplete)
    {
        _fullText = text;
        _currentCharIndex = 0;
        _isTyping = true;
        _msgText.text = "";

        // 1文字ずつ表示するスケジュール
        _typewriterSchedule = _msgWindow.schedule.Execute(() =>
        {
            if (_currentCharIndex < _fullText.Length)
            {
                _currentCharIndex++;
                _msgText.text = _fullText.Substring(0, _currentCharIndex);
            }
            else
            {
                // 完了
                _isTyping = false;
                _typewriterSchedule?.Pause();
                ScheduleHide(durationAfterComplete);
            }
        }).Every((long)(_charInterval * 1000));
    }

    /// <summary>
    /// タイプライターをスキップして全文表示
    /// </summary>
    public void SkipTypewriter()
    {
        if (!_isTyping || string.IsNullOrEmpty(_fullText)) return;

        _typewriterSchedule?.Pause();
        _msgText.text = _fullText;
        _isTyping = false;
    }

    /// <summary>
    /// 自動非表示をスケジュール
    /// </summary>
    private void ScheduleHide(float duration)
    {
        if (_msgWindow == null || duration <= 0) return;
        _hideSchedule = _msgWindow.schedule.Execute(Hide).StartingIn((long)(duration * 1000));
    }

    /// <summary>
    /// メッセージを非表示
    /// </summary>
    public void Hide()
    {
        StopAllSchedules();
        _msgWindow?.RemoveFromClassList("active");
        _isTyping = false;
    }

    private void StopAllSchedules()
    {
        _hideSchedule?.Pause();
        _typewriterSchedule?.Pause();
    }

    // ========================================
    // プロパティ
    // ========================================

    public bool IsTyping => _isTyping;

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        StopAllSchedules();
        _hideSchedule = null;
        _typewriterSchedule = null;
    }
}
