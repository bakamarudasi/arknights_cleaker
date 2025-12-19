using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// チュートリアルマネージャー
/// 初回起動時のガイドを管理
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // ========================================
    // チュートリアルステップ定義
    // ========================================

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

    public enum TutorialPosition
    {
        Center,
        Top,
        Bottom,
        Left,
        Right
    }

    // ========================================
    // チュートリアルシーケンス
    // ========================================

    [Serializable]
    public class TutorialSequence
    {
        public string sequenceId;
        public string sequenceName;
        public List<TutorialStep> steps = new();
    }

    // ========================================
    // 設定
    // ========================================

    [Header("チュートリアルデータ")]
    [SerializeField] private List<TutorialSequence> tutorials = new();

    [Header("表示設定")]
    [SerializeField] private float autoAdvanceDelay = 0f;  // 0 = 手動進行

    // ========================================
    // 状態
    // ========================================

    private HashSet<string> completedTutorials = new();
    private TutorialSequence currentSequence;
    private int currentStepIndex;
    private bool isActive;

    // UI要素
    private VisualElement tutorialOverlay;
    private VisualElement tutorialPanel;
    private Label titleLabel;
    private Label messageLabel;
    private Label stepIndicator;
    private Button nextButton;
    private Button skipButton;

    // イベント
    public event Action<string> OnTutorialStarted;
    public event Action<string> OnTutorialCompleted;
    public event Action<string> OnTutorialSkipped;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDefaultTutorials();
    }

    private void InitializeDefaultTutorials()
    {
        // マーケットチュートリアル
        var marketTutorial = new TutorialSequence
        {
            sequenceId = "market_basic",
            sequenceName = "株式市場入門",
            steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    id = "market_1",
                    title = "📈 マーケットへようこそ！",
                    message = "ここでは株式を売買できます。\n安く買って高く売れば利益が出ます。\n逆だと損失です...気をつけて！",
                    position = TutorialPosition.Center
                },
                new TutorialStep
                {
                    id = "market_2",
                    title = "💰 所持金（LMD）",
                    message = "左上に所持金が表示されています。\n株を買うにはLMDが必要です。\nまずは少額から始めましょう！",
                    highlightElement = "lmd-value",
                    position = TutorialPosition.Right
                },
                new TutorialStep
                {
                    id = "market_3",
                    title = "📊 銘柄リスト",
                    message = "右側に売買できる銘柄が並んでいます。\nクリックすると詳細が見れます。\n各企業には特徴があります。",
                    highlightElement = "stock-list",
                    position = TutorialPosition.Left
                },
                new TutorialStep
                {
                    id = "market_4",
                    title = "📉 チャート",
                    message = "中央のチャートで価格推移が見れます。\n緑 = 上昇トレンド\n赤 = 下落トレンド",
                    highlightElement = "chart-canvas",
                    position = TutorialPosition.Bottom
                },
                new TutorialStep
                {
                    id = "market_5",
                    title = "🛒 売買方法",
                    message = "1. 数量を入力（10, 100, MAXボタンも便利）\n2. BUYで購入 / SELLで売却\n\n保有中は「利確」「損切り」表示に変わります。",
                    highlightElement = "trade-panel",
                    position = TutorialPosition.Top
                },
                new TutorialStep
                {
                    id = "market_6",
                    title = "🔔 ニュース＆イベント",
                    message = "株価はニュースやイベントで変動します。\n・ポジティブニュース → 上昇傾向\n・ネガティブニュース → 下落傾向\n\nログをチェックしよう！",
                    position = TutorialPosition.Center
                },
                new TutorialStep
                {
                    id = "market_7",
                    title = "✨ スキル（イカサマ）",
                    message = "困ったときの奥の手！\n・物理買い支え: 株価を少し上げる\n・インサイダー: 数秒先が見える\n\nクールダウンがあるので計画的に！",
                    highlightElement = "skill-panel",
                    position = TutorialPosition.Left
                },
                new TutorialStep
                {
                    id = "market_8",
                    title = "🏆 ロドス株",
                    message = "左側の「RHODES ISLAND」は特別な株。\nクリック連打で株価が上がり、\n定期的に配当がもらえます！\n\nランクを上げると報酬UP！",
                    highlightElement = "rhodos-panel",
                    position = TutorialPosition.Right
                },
                new TutorialStep
                {
                    id = "market_9",
                    title = "📝 まとめ",
                    message = "・安く買って高く売る\n・ニュースに注目\n・損切りは早めに\n・ロドス株で安定収入\n\nグッドラック！💪",
                    position = TutorialPosition.Center
                }
            }
        };

        // ホームチュートリアル
        var homeTutorial = new TutorialSequence
        {
            sequenceId = "home_basic",
            sequenceName = "ホーム画面の使い方",
            steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    id = "home_1",
                    title = "🏠 ホーム画面",
                    message = "ここがメイン画面です。\nサイドバーから各機能にアクセスできます。",
                    position = TutorialPosition.Center
                },
                new TutorialStep
                {
                    id = "home_2",
                    title = "📱 サイドバー",
                    message = "左のアイコンから：\n・ホーム\n・オペレーター\n・マーケット\n・ショップ\nなどにアクセス！",
                    position = TutorialPosition.Right
                }
            }
        };

        // オペレーターチュートリアル
        var operatorTutorial = new TutorialSequence
        {
            sequenceId = "operator_basic",
            sequenceName = "オペレーター画面",
            steps = new List<TutorialStep>
            {
                new TutorialStep
                {
                    id = "operator_1",
                    title = "👋 オペレーター",
                    message = "ここでキャラクターと交流できます。\nクリックして話しかけてみよう！",
                    position = TutorialPosition.Center
                },
                new TutorialStep
                {
                    id = "operator_2",
                    title = "💕 好感度",
                    message = "クリックや撫でると好感度UP！\n好感度が上がると：\n・新しいセリフ解放\n・新しい衣装解放\n・特別なイベント発生",
                    position = TutorialPosition.Center
                },
                new TutorialStep
                {
                    id = "operator_3",
                    title = "🎁 プレゼント",
                    message = "アイテムをプレゼントすると\n好感度が大きく上がります！\nキャラの好みを覚えよう。",
                    position = TutorialPosition.Center
                }
            }
        };

        tutorials.Add(marketTutorial);
        tutorials.Add(homeTutorial);
        tutorials.Add(operatorTutorial);
    }

    // ========================================
    // チュートリアル開始
    // ========================================

    /// <summary>
    /// チュートリアルを開始（未完了の場合のみ）
    /// </summary>
    public bool TryStartTutorial(string sequenceId, VisualElement rootElement)
    {
        if (completedTutorials.Contains(sequenceId))
        {
            Debug.Log($"[Tutorial] Already completed: {sequenceId}");
            return false;
        }

        return StartTutorial(sequenceId, rootElement);
    }

    /// <summary>
    /// チュートリアルを強制開始
    /// </summary>
    public bool StartTutorial(string sequenceId, VisualElement rootElement)
    {
        var sequence = tutorials.Find(t => t.sequenceId == sequenceId);
        if (sequence == null || sequence.steps.Count == 0)
        {
            Debug.LogWarning($"[Tutorial] Sequence not found: {sequenceId}");
            return false;
        }

        currentSequence = sequence;
        currentStepIndex = 0;
        isActive = true;

        CreateTutorialUI(rootElement);
        ShowCurrentStep();

        OnTutorialStarted?.Invoke(sequenceId);
        Debug.Log($"[Tutorial] Started: {sequenceId}");

        return true;
    }

    // ========================================
    // UI作成
    // ========================================

    private void CreateTutorialUI(VisualElement root)
    {
        // 既存のオーバーレイを削除
        var existing = root.Q<VisualElement>("tutorial-overlay");
        existing?.RemoveFromHierarchy();

        // オーバーレイ作成
        tutorialOverlay = new VisualElement();
        tutorialOverlay.name = "tutorial-overlay";
        tutorialOverlay.style.position = Position.Absolute;
        tutorialOverlay.style.top = 0;
        tutorialOverlay.style.left = 0;
        tutorialOverlay.style.right = 0;
        tutorialOverlay.style.bottom = 0;
        tutorialOverlay.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        tutorialOverlay.style.justifyContent = Justify.Center;
        tutorialOverlay.style.alignItems = Align.Center;

        // パネル作成
        tutorialPanel = new VisualElement();
        tutorialPanel.style.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        tutorialPanel.style.borderTopWidth = 2;
        tutorialPanel.style.borderBottomWidth = 2;
        tutorialPanel.style.borderLeftWidth = 2;
        tutorialPanel.style.borderRightWidth = 2;
        tutorialPanel.style.borderTopColor = new Color(1f, 0.31f, 0f, 1f);
        tutorialPanel.style.borderBottomColor = new Color(1f, 0.31f, 0f, 1f);
        tutorialPanel.style.borderLeftColor = new Color(1f, 0.31f, 0f, 1f);
        tutorialPanel.style.borderRightColor = new Color(1f, 0.31f, 0f, 1f);
        tutorialPanel.style.paddingTop = 20;
        tutorialPanel.style.paddingBottom = 20;
        tutorialPanel.style.paddingLeft = 30;
        tutorialPanel.style.paddingRight = 30;
        tutorialPanel.style.minWidth = 400;
        tutorialPanel.style.maxWidth = 500;

        // タイトル
        titleLabel = new Label();
        titleLabel.style.fontSize = 24;
        titleLabel.style.color = new Color(1f, 0.31f, 0f, 1f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 15;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        // メッセージ
        messageLabel = new Label();
        messageLabel.style.fontSize = 16;
        messageLabel.style.color = Color.white;
        messageLabel.style.whiteSpace = WhiteSpace.Normal;
        messageLabel.style.marginBottom = 20;

        // ステップインジケーター
        stepIndicator = new Label();
        stepIndicator.style.fontSize = 12;
        stepIndicator.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        stepIndicator.style.unityTextAlign = TextAnchor.MiddleCenter;
        stepIndicator.style.marginBottom = 15;

        // ボタンコンテナ
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;

        // スキップボタン
        skipButton = new Button(() => SkipTutorial());
        skipButton.text = "スキップ";
        skipButton.style.backgroundColor = Color.clear;
        skipButton.style.borderTopWidth = 1;
        skipButton.style.borderBottomWidth = 1;
        skipButton.style.borderLeftWidth = 1;
        skipButton.style.borderRightWidth = 1;
        skipButton.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        skipButton.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        skipButton.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        skipButton.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        skipButton.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        skipButton.style.paddingTop = 8;
        skipButton.style.paddingBottom = 8;
        skipButton.style.paddingLeft = 20;
        skipButton.style.paddingRight = 20;

        // 次へボタン
        nextButton = new Button(() => NextStep());
        nextButton.text = "次へ →";
        nextButton.style.backgroundColor = new Color(1f, 0.31f, 0f, 0.2f);
        nextButton.style.borderTopWidth = 2;
        nextButton.style.borderBottomWidth = 2;
        nextButton.style.borderLeftWidth = 2;
        nextButton.style.borderRightWidth = 2;
        nextButton.style.borderTopColor = new Color(1f, 0.31f, 0f, 1f);
        nextButton.style.borderBottomColor = new Color(1f, 0.31f, 0f, 1f);
        nextButton.style.borderLeftColor = new Color(1f, 0.31f, 0f, 1f);
        nextButton.style.borderRightColor = new Color(1f, 0.31f, 0f, 1f);
        nextButton.style.color = new Color(1f, 0.31f, 0f, 1f);
        nextButton.style.paddingTop = 8;
        nextButton.style.paddingBottom = 8;
        nextButton.style.paddingLeft = 25;
        nextButton.style.paddingRight = 25;
        nextButton.style.unityFontStyleAndWeight = FontStyle.Bold;

        buttonContainer.Add(skipButton);
        buttonContainer.Add(nextButton);

        tutorialPanel.Add(titleLabel);
        tutorialPanel.Add(messageLabel);
        tutorialPanel.Add(stepIndicator);
        tutorialPanel.Add(buttonContainer);

        tutorialOverlay.Add(tutorialPanel);
        root.Add(tutorialOverlay);
    }

    // ========================================
    // ステップ表示
    // ========================================

    private void ShowCurrentStep()
    {
        if (currentSequence == null || currentStepIndex >= currentSequence.steps.Count)
        {
            CompleteTutorial();
            return;
        }

        var step = currentSequence.steps[currentStepIndex];

        titleLabel.text = step.title;
        messageLabel.text = step.message;
        stepIndicator.text = $"{currentStepIndex + 1} / {currentSequence.steps.Count}";

        // 最後のステップは「完了」ボタンに
        if (currentStepIndex >= currentSequence.steps.Count - 1)
        {
            nextButton.text = "完了 ✓";
        }
        else
        {
            nextButton.text = "次へ →";
        }
    }

    private void NextStep()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    // ========================================
    // 完了/スキップ
    // ========================================

    private void CompleteTutorial()
    {
        if (currentSequence != null)
        {
            completedTutorials.Add(currentSequence.sequenceId);
            OnTutorialCompleted?.Invoke(currentSequence.sequenceId);
            Debug.Log($"[Tutorial] Completed: {currentSequence.sequenceId}");
        }

        CloseTutorialUI();
    }

    private void SkipTutorial()
    {
        if (currentSequence != null)
        {
            completedTutorials.Add(currentSequence.sequenceId);
            OnTutorialSkipped?.Invoke(currentSequence.sequenceId);
            Debug.Log($"[Tutorial] Skipped: {currentSequence.sequenceId}");
        }

        CloseTutorialUI();
    }

    private void CloseTutorialUI()
    {
        tutorialOverlay?.RemoveFromHierarchy();
        tutorialOverlay = null;
        currentSequence = null;
        isActive = false;
    }

    // ========================================
    // 状態確認
    // ========================================

    public bool IsCompleted(string sequenceId)
    {
        return completedTutorials.Contains(sequenceId);
    }

    public bool IsActive => isActive;

    /// <summary>
    /// 完了状態をリセット（デバッグ用）
    /// </summary>
    public void ResetTutorial(string sequenceId)
    {
        completedTutorials.Remove(sequenceId);
    }

    public void ResetAllTutorials()
    {
        completedTutorials.Clear();
    }

    // ========================================
    // セーブ/ロード
    // ========================================

    public List<string> GetSaveData()
    {
        return new List<string>(completedTutorials);
    }

    public void LoadSaveData(List<string> data)
    {
        completedTutorials.Clear();
        if (data != null)
        {
            foreach (var id in data)
            {
                completedTutorials.Add(id);
            }
        }
    }
}
