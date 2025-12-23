using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// チュートリアルマネージャー
/// 初回起動時のガイドを管理
///
/// 関連ファイル:
/// - TutorialSequenceData.cs : ScriptableObject定義
/// - TutorialUIBuilder.cs : UIスタイル構築
/// - DefaultTutorialData.cs : デフォルトチュートリアルデータ
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // ========================================
    // 設定
    // ========================================

    [Header("チュートリアルデータ")]
    [SerializeField] private List<TutorialSequenceData> tutorialAssets = new();

    [Header("表示設定")]
#pragma warning disable CS0414
    [SerializeField] private float autoAdvanceDelay = 0f;  // 0 = 手動進行
#pragma warning restore CS0414

    // ========================================
    // 状態
    // ========================================

    private List<TutorialSequenceRuntime> tutorials = new();
    private HashSet<string> completedTutorials = new();
    private TutorialSequenceRuntime currentSequence;
    private int currentStepIndex;
    private bool isActive;

    // UI要素
    private VisualElement tutorialOverlay;
    private Label titleLabel;
    private Label messageLabel;
    private Label stepIndicator;
    private Button nextButton;

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

        LoadTutorials();
    }

    private void LoadTutorials()
    {
        // ScriptableObjectからロード
        foreach (var asset in tutorialAssets)
        {
            if (asset != null)
            {
                tutorials.Add(new TutorialSequenceRuntime
                {
                    sequenceId = asset.sequenceId,
                    sequenceName = asset.sequenceName,
                    steps = new List<TutorialStep>(asset.steps)
                });
            }
        }

        // アセットがない場合はデフォルトを使用
        if (tutorials.Count == 0)
        {
            tutorials = DefaultTutorialData.GetDefaultTutorials();
        }
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
    // UI作成（UIBuilderを使用）
    // ========================================

    private void CreateTutorialUI(VisualElement root)
    {
        // 既存のオーバーレイを削除
        root.Q<VisualElement>("tutorial-overlay")?.RemoveFromHierarchy();

        // UI構築
        tutorialOverlay = TutorialUIBuilder.CreateOverlay();
        var panel = TutorialUIBuilder.CreatePanel();

        titleLabel = TutorialUIBuilder.CreateTitleLabel();
        messageLabel = TutorialUIBuilder.CreateMessageLabel();
        stepIndicator = TutorialUIBuilder.CreateStepIndicator();

        var buttonContainer = TutorialUIBuilder.CreateButtonContainer();
        var skipButton = TutorialUIBuilder.CreateSkipButton(SkipTutorial);
        nextButton = TutorialUIBuilder.CreateNextButton(NextStep);

        // 組み立て
        buttonContainer.Add(skipButton);
        buttonContainer.Add(nextButton);

        panel.Add(titleLabel);
        panel.Add(messageLabel);
        panel.Add(stepIndicator);
        panel.Add(buttonContainer);

        tutorialOverlay.Add(panel);
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
        nextButton.text = currentStepIndex >= currentSequence.steps.Count - 1
            ? "完了 ✓"
            : "次へ →";
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

    public bool IsCompleted(string sequenceId) => completedTutorials.Contains(sequenceId);
    public bool IsActive => isActive;

    /// <summary>
    /// 完了状態をリセット（デバッグ用）
    /// </summary>
    public void ResetTutorial(string sequenceId) => completedTutorials.Remove(sequenceId);
    public void ResetAllTutorials() => completedTutorials.Clear();

    // ========================================
    // セーブ/ロード
    // ========================================

    public List<string> GetSaveData() => new List<string>(completedTutorials);

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
