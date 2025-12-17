using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class LogUIController : MonoBehaviour
{
    public static LogUIController Instance { get; private set; }

    private ScrollView _logScrollView;
    private bool _isInitialized = false;

    // UXMLに合わせてクラス名を指定
    private const string SCROLL_VIEW_CLASS = "log-scroll-view";

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Initialize(VisualElement root)
    {
        if (_isInitialized) return;

        // ★修正点: 名前ではなくクラス名で探す
        // MainLayout.uxml では <ui:ScrollView class="log-scroll-view" /> となっているため
        _logScrollView = root.Q<ScrollView>(className: SCROLL_VIEW_CLASS);

        if (_logScrollView == null)
        {
            Debug.LogError($"[LogUI] ScrollView with class '{SCROLL_VIEW_CLASS}' not found in MainLayout!");
            return;
        }

        _isInitialized = true;
        Post("Log System Initialized.", "#00FF00"); // 緑色で開始合図
    }

    public void Post(string message, string colorHex = "#e5e5e5") // デフォルト色をCSSに合わせる
    {
        if (!_isInitialized || _logScrollView == null) return;

        // USSの .log-entry 構造を作る
        var entry = new VisualElement();
        entry.AddToClassList("log-entry"); // USS: .log-entry

        // 時間
        var timeLabel = new Label($"[{System.DateTime.Now:HH:mm:ss}]");
        timeLabel.AddToClassList("log-time"); // USS: .log-time
        
        // メッセージ
        var msgLabel = new Label(message);
        msgLabel.AddToClassList("log-msg"); // USS: .log-msg
        
        // 色指定があれば上書き
        if (ColorUtility.TryParseHtmlString(colorHex, out Color c))
        {
            msgLabel.style.color = c;
        }

        entry.Add(timeLabel);
        entry.Add(msgLabel);

        _logScrollView.Add(entry);
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        if (_logScrollView != null)
        {
            _logScrollView.scrollOffset = new Vector2(0, _logScrollView.contentContainer.layout.height);
        }
    }

    // ショートカット
    public static void Msg(string message) => Instance?.Post(message);
    public static void Error(string message) => Instance?.Post(message, "#ff3b30"); // Arknightsっぽい赤
    public static void LogSystem(string message) => Instance?.Post(message, "#d97706"); // USSのオレンジ
}