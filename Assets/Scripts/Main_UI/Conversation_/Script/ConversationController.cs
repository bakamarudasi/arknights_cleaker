using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

/// <summary>
/// 会話イベントUIのコントローラー
/// IEventDisplayを実装し、EventManagerから呼び出される
/// </summary>
public class ConversationController : MonoBehaviour, IEventDisplay
{
    [Header("UI設定")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset viewTemplate;

    [Header("会話データ")]
    [SerializeField] private ConversationData conversationData;

    // UI要素
    private VisualElement root;
    private VisualElement overlay;
    private VisualElement tapArea;
    private VisualElement characterLeft;
    private VisualElement characterRight;
    private Label speakerNameLabel;
    private Label dialogTextLabel;
    private VisualElement nextIndicator;
    private Button skipButton;

    // 状態
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool canAdvance = false;
    private string fullText = "";
    private Coroutine typewriterCoroutine;
    private Action onCompleteCallback;

    // 定数
    private const string CLS_INACTIVE = "inactive";
    private const string CLS_SPEAKING = "speaking";
    private const string CLS_VISIBLE = "visible";
    private const string CLS_FADE_IN = "fade-in";

    void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    void Start()
    {
        // 直接シーンに配置された場合の初期化
        if (conversationData != null && onCompleteCallback == null)
        {
            Initialize(conversationData, null);
        }
    }

    /// <summary>
    /// IEventDisplay実装 - EventManagerから呼ばれる
    /// </summary>
    public void Setup(GameEventData eventData, Action onComplete)
    {
        onCompleteCallback = onComplete;

        // GameEventDataからConversationDataを取得する方法はいくつかある
        // 1. eventDataにConversationDataへの参照を持たせる（拡張が必要）
        // 2. eventIdからResourcesで検索する
        // 3. 直接ConversationDataをこのコンポーネントに設定しておく

        // 今はシンプルに、このGameObjectにアタッチされたConversationDataを使う
        if (conversationData != null)
        {
            Initialize(conversationData, onComplete);
        }
        else
        {
            Debug.LogError("[Conversation] No ConversationData assigned!");
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 会話を初期化して開始
    /// </summary>
    public void Initialize(ConversationData data, Action onComplete)
    {
        conversationData = data;
        onCompleteCallback = onComplete;
        currentLineIndex = 0;

        SetupUI();
        ShowCurrentLine();
    }

    private void SetupUI()
    {
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        // テンプレートを使う場合
        if (viewTemplate != null)
        {
            root.Clear();
            var instance = viewTemplate.Instantiate();
            root.Add(instance);
        }

        // 要素を取得
        overlay = root.Q<VisualElement>("conversation-overlay");
        tapArea = root.Q<VisualElement>("tap-area");
        characterLeft = root.Q<VisualElement>("character-left");
        characterRight = root.Q<VisualElement>("character-right");
        speakerNameLabel = root.Q<Label>("speaker-name");
        dialogTextLabel = root.Q<Label>("dialog-text");
        nextIndicator = root.Q<VisualElement>("next-indicator");
        skipButton = root.Q<Button>("skip-button");

        // イベント登録
        tapArea?.RegisterCallback<ClickEvent>(OnTapAreaClicked);
        skipButton?.RegisterCallback<ClickEvent>(OnSkipClicked);

        // フェードイン
        overlay?.AddToClassList(CLS_FADE_IN);
        root.schedule.Execute(() => overlay?.AddToClassList(CLS_VISIBLE)).ExecuteLater(50);

        // 次へインジケーターの点滅
        StartBlinkAnimation();
    }

    private void OnTapAreaClicked(ClickEvent evt)
    {
        if (isTyping)
        {
            // タイプライター中ならスキップして全文表示
            CompleteTyping();
        }
        else if (canAdvance)
        {
            // 次の行へ
            AdvanceToNextLine();
        }
    }

    private void OnSkipClicked(ClickEvent evt)
    {
        // 会話をスキップして終了
        EndConversation();
    }

    private void ShowCurrentLine()
    {
        if (conversationData == null || currentLineIndex >= conversationData.lines.Count)
        {
            EndConversation();
            return;
        }

        var line = conversationData.lines[currentLineIndex];

        // 話者名を設定
        if (speakerNameLabel != null)
        {
            speakerNameLabel.text = line.speakerName;
        }

        // キャラクター立ち絵を設定
        UpdateCharacterSprites(line);

        // テキストをタイプライター表示
        fullText = line.text;
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterEffect());

        // SE再生
        if (line.soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(line.soundEffect, Camera.main.transform.position);
        }
    }

    private void UpdateCharacterSprites(DialogLine line)
    {
        // 両方のキャラをリセット
        characterLeft?.RemoveFromClassList(CLS_SPEAKING);
        characterLeft?.AddToClassList(CLS_INACTIVE);
        characterRight?.RemoveFromClassList(CLS_SPEAKING);
        characterRight?.AddToClassList(CLS_INACTIVE);

        // 話者の位置に応じて設定
        VisualElement activeCharacter = null;

        switch (line.position)
        {
            case SpeakerPosition.Left:
                activeCharacter = characterLeft;
                if (line.characterSprite != null && characterLeft != null)
                    characterLeft.style.backgroundImage = new StyleBackground(line.characterSprite);
                break;

            case SpeakerPosition.Right:
                activeCharacter = characterRight;
                if (line.characterSprite != null && characterRight != null)
                    characterRight.style.backgroundImage = new StyleBackground(line.characterSprite);
                break;

            case SpeakerPosition.Center:
                // センターは左側を使用
                activeCharacter = characterLeft;
                if (line.characterSprite != null && characterLeft != null)
                    characterLeft.style.backgroundImage = new StyleBackground(line.characterSprite);
                break;
        }

        // アクティブなキャラをハイライト
        if (activeCharacter != null)
        {
            activeCharacter.RemoveFromClassList(CLS_INACTIVE);
            activeCharacter.AddToClassList(CLS_SPEAKING);
        }
    }

    private IEnumerator TypewriterEffect()
    {
        isTyping = true;
        canAdvance = false;
        dialogTextLabel.text = "";

        int charIndex = 0;
        float interval = conversationData.typewriterSpeed / 1000f;

        while (charIndex < fullText.Length)
        {
            dialogTextLabel.text = fullText.Substring(0, charIndex + 1);
            charIndex++;
            yield return new WaitForSecondsRealtime(interval);
        }

        CompleteTyping();
    }

    private void CompleteTyping()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        dialogTextLabel.text = fullText;
        isTyping = false;
        canAdvance = true;

        // 自動進行の場合
        if (conversationData.autoAdvance)
        {
            StartCoroutine(AutoAdvanceAfterDelay());
        }
    }

    private IEnumerator AutoAdvanceAfterDelay()
    {
        yield return new WaitForSecondsRealtime(conversationData.autoAdvanceDelay);
        if (canAdvance)
        {
            AdvanceToNextLine();
        }
    }

    private void AdvanceToNextLine()
    {
        currentLineIndex++;
        canAdvance = false;

        if (currentLineIndex >= conversationData.lines.Count)
        {
            EndConversation();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    private void EndConversation()
    {
        // フェードアウト
        overlay?.RemoveFromClassList(CLS_VISIBLE);

        // 少し待ってからコールバック
        root?.schedule.Execute(() =>
        {
            onCompleteCallback?.Invoke();
        }).ExecuteLater(300);
    }

    private void StartBlinkAnimation()
    {
        if (nextIndicator == null) return;

        // UI Toolkitのスケジューラーで点滅
        var scheduler = root.schedule;
        scheduler.Execute(() =>
        {
            nextIndicator.ToggleInClassList("blink");
        }).Every(500);
    }

    private void OnDestroy()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        // イベント解除
        tapArea?.UnregisterCallback<ClickEvent>(OnTapAreaClicked);
        skipButton?.UnregisterCallback<ClickEvent>(OnSkipClicked);
    }
}
