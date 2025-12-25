using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

/// <summary>
/// 会話イベントUIのコントローラー
/// IEventDisplayを実装し、EventManagerから呼び出される
///
/// 表示モード:
/// - OverlayUI: 従来の全画面会話UI
/// - SpeechBubble: 立ち絵画面 + 吹き出し
/// </summary>
public class ConversationController : MonoBehaviour, IEventDisplay
{
    [Header("UI設定")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset viewTemplate;

    [Header("会話データ")]
    [SerializeField] private ConversationData conversationData;

    // UI要素（OverlayUIモード用）
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
    private Coroutine speechBubbleCoroutine;
    private Action onCompleteCallback;

    // 現在の表示モード
    private ConversationDisplayMode currentDisplayMode;
    private bool isOverlayUIActive = false;

    // 外部参照（SpeechBubbleモード用）
    private CharacterSpeechBubble speechBubble;
    private SpineLayerController spineController;
    private OverlayCharacterPresenter characterPresenter;

    // 定数
    private const string CLS_INACTIVE = "inactive";
    private const string CLS_SPEAKING = "speaking";
    private const string CLS_VISIBLE = "visible";
    private const string CLS_FADE_IN = "fade-in";

    // イベント
    public event Action<string> OnSceneChangeRequested;
    public event Action<string> OnAnimationChangeRequested;

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
        currentDisplayMode = data.defaultDisplayMode;

        // 外部参照を取得
        FindExternalReferences();

        // 最初の行の表示モードを確認
        if (data.lines.Count > 0)
        {
            currentDisplayMode = data.lines[0].GetDisplayMode(data.defaultDisplayMode);
        }

        // 表示モードに応じて初期化
        if (currentDisplayMode == ConversationDisplayMode.OverlayUI)
        {
            SetupOverlayUI();
        }

        ShowCurrentLine();
    }

    /// <summary>
    /// 外部参照を取得
    /// </summary>
    private void FindExternalReferences()
    {
        if (speechBubble == null)
            speechBubble = CharacterSpeechBubble.Instance;

        if (characterPresenter == null)
            characterPresenter = FindAnyObjectByType<OverlayCharacterPresenter>();

        // SpineLayerControllerはキャラプレハブから取得
        if (characterPresenter?.CurrentInstance != null)
        {
            spineController = characterPresenter.CurrentInstance.GetComponent<SpineLayerController>();
        }
    }

    // ========================================
    // OverlayUI モード
    // ========================================

    private void SetupOverlayUI()
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

        isOverlayUIActive = true;
    }

    private void HideOverlayUI()
    {
        if (!isOverlayUIActive) return;

        overlay?.RemoveFromClassList(CLS_VISIBLE);
        isOverlayUIActive = false;
    }

    private void ShowOverlayUI()
    {
        if (isOverlayUIActive) return;

        SetupOverlayUI();
    }

    // ========================================
    // 行の表示
    // ========================================

    private void ShowCurrentLine()
    {
        if (conversationData == null || currentLineIndex >= conversationData.lines.Count)
        {
            EndConversation();
            return;
        }

        var line = conversationData.lines[currentLineIndex];
        var lineDisplayMode = line.GetDisplayMode(conversationData.defaultDisplayMode);

        // シーン切り替えがあれば実行
        if (!string.IsNullOrEmpty(line.changeToSceneId))
        {
            RequestSceneChange(line.changeToSceneId);
        }

        // アニメーション切り替えがあれば実行
        if (!string.IsNullOrEmpty(line.animationTrigger))
        {
            RequestAnimationChange(line.animationTrigger);
        }

        // 表示モードが変わったら切り替え
        if (lineDisplayMode != currentDisplayMode)
        {
            SwitchDisplayMode(lineDisplayMode);
        }

        // 表示モードに応じて行を表示
        if (currentDisplayMode == ConversationDisplayMode.OverlayUI)
        {
            ShowLineOverlayUI(line);
        }
        else
        {
            ShowLineSpeechBubble(line);
        }
    }

    private void SwitchDisplayMode(ConversationDisplayMode newMode)
    {
        // 現在のモードを終了
        if (currentDisplayMode == ConversationDisplayMode.OverlayUI)
        {
            HideOverlayUI();
        }

        // 新しいモードを開始
        if (newMode == ConversationDisplayMode.OverlayUI)
        {
            ShowOverlayUI();
        }

        currentDisplayMode = newMode;
        Debug.Log($"[Conversation] Display mode changed to: {newMode}");
    }

    // ========================================
    // OverlayUI モードでの行表示
    // ========================================

    private void ShowLineOverlayUI(DialogLine line)
    {
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
        PlaySoundEffect(line.soundEffect);
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

        if (dialogTextLabel != null)
            dialogTextLabel.text = fullText;
        isTyping = false;
        canAdvance = true;

        // 自動進行の場合
        if (conversationData.autoAdvance)
        {
            StartCoroutine(AutoAdvanceAfterDelay());
        }
    }

    // ========================================
    // SpeechBubble モードでの行表示
    // ========================================

    private void ShowLineSpeechBubble(DialogLine line)
    {
        if (speechBubble == null)
        {
            Debug.LogWarning("[Conversation] SpeechBubble not found, falling back to OverlayUI");
            SwitchDisplayMode(ConversationDisplayMode.OverlayUI);
            ShowLineOverlayUI(line);
            return;
        }

        // SE再生
        PlaySoundEffect(line.soundEffect);

        // 吹き出し位置を取得
        Vector3 bubblePosition = GetBubblePosition(line.bubbleAnchor);

        // 吹き出しで表示（タップ待ち）
        speechBubble.ShowDialogueAtWithCallback(bubblePosition, line.text, OnSpeechBubbleComplete);
    }

    private Vector3 GetBubblePosition(string anchorName)
    {
        // SpineLayerControllerがあればボーン位置を取得
        if (spineController != null)
        {
            return spineController.GetBoneWorldPosition(anchorName);
        }

        // CharacterPresenterから位置を取得（PSBの場合）
        if (characterPresenter?.CurrentInstance != null)
        {
            // デフォルトはキャラの上方
            return characterPresenter.CurrentInstance.transform.position + new Vector3(0, 2.5f, 0);
        }

        // フォールバック
        return Vector3.zero;
    }

    private void OnSpeechBubbleComplete()
    {
        AdvanceToNextLine();
    }

    // ========================================
    // シーン・アニメーション切り替え
    // ========================================

    private void RequestSceneChange(string sceneId)
    {
        Debug.Log($"[Conversation] Scene change requested: {sceneId}");

        // イベントで通知
        OnSceneChangeRequested?.Invoke(sceneId);

        // CharacterPresenterがあれば直接切り替え
        if (characterPresenter != null)
        {
            // OverlayCharacterPresenter経由でシーン切り替え
            // 注: SetScene メソッドがあれば呼び出す
            var sceneManager = characterPresenter.GetType().GetProperty("CurrentSceneId");
            // 実際の実装はOverlayCharacterPresenterのAPIに依存
        }
    }

    private void RequestAnimationChange(string animationName)
    {
        Debug.Log($"[Conversation] Animation change requested: {animationName}");

        // イベントで通知
        OnAnimationChangeRequested?.Invoke(animationName);

        // SpineLayerControllerがあれば直接再生
        if (spineController != null)
        {
            spineController.PlayAnimation(animationName, true);
        }
    }

    // ========================================
    // ユーティリティ
    // ========================================

    private void PlaySoundEffect(AudioClip clip)
    {
        if (clip != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    // ========================================
    // 入力処理
    // ========================================

    private void OnTapAreaClicked(ClickEvent evt)
    {
        if (isTyping)
        {
            CompleteTyping();
        }
        else if (canAdvance)
        {
            AdvanceToNextLine();
        }
    }

    private void OnSkipClicked(ClickEvent evt)
    {
        EndConversation();
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
        // OverlayUIを非表示
        if (isOverlayUIActive)
        {
            overlay?.RemoveFromClassList(CLS_VISIBLE);
        }

        // 吹き出しを非表示
        speechBubble?.Hide();

        // 少し待ってからコールバック
        StartCoroutine(EndConversationAfterDelay());
    }

    private IEnumerator EndConversationAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        onCompleteCallback?.Invoke();
    }

    private void StartBlinkAnimation()
    {
        if (nextIndicator == null || root == null) return;

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

        tapArea?.UnregisterCallback<ClickEvent>(OnTapAreaClicked);
        skipButton?.UnregisterCallback<ClickEvent>(OnSkipClicked);
    }
}
