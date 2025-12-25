using UnityEngine.UIElements;

/// <summary>
/// オペレーター画面の好感度UI表示を担当するコントローラー
/// 単一責任: 好感度レベルとバーの表示
/// </summary>
public class OperatorAffectionController
{
    // ========================================
    // UI要素
    // ========================================

    private Label affectionLevelLabel;
    private VisualElement affectionBarFill;
    private Label affectionValueLabel;

    // ========================================
    // 定数
    // ========================================

    private static readonly int[] LevelThresholds = { 0, 50, 100, 150, 200 };
    private const int DefaultMaxAffection = 200;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// コントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement root)
    {
        affectionLevelLabel = root.Q<Label>("affection-level");
        affectionBarFill = root.Q<VisualElement>("affection-bar-fill");
        affectionValueLabel = root.Q<Label>("affection-value");

        UpdateAffectionUI();
    }

    // ========================================
    // 表示更新
    // ========================================

    /// <summary>
    /// 好感度UIを更新
    /// </summary>
    public void UpdateAffectionUI()
    {
        var affectionManager = AffectionManager.Instance;
        if (affectionManager == null) return;

        int currentAffection = affectionManager.GetCurrentAffection();
        var currentLevel = affectionManager.GetCurrentAffectionLevel();

        UpdateLevelDisplay(currentLevel);
        UpdateBarDisplay(currentAffection, currentLevel);
        UpdateValueDisplay(currentAffection);
    }

    private void UpdateLevelDisplay(AffectionLevel currentLevel)
    {
        if (affectionLevelLabel == null || currentLevel == null) return;
        affectionLevelLabel.text = $"Lv.{currentLevel.level} {currentLevel.levelName}";
    }

    private void UpdateBarDisplay(int currentAffection, AffectionLevel currentLevel)
    {
        if (affectionBarFill == null || currentLevel == null) return;

        int levelStart = currentLevel.requiredAffection;
        int levelEnd = GetNextLevelRequirement(currentLevel.level);
        float progress = 0f;

        if (levelEnd > levelStart)
        {
            progress = (float)(currentAffection - levelStart) / (levelEnd - levelStart);
        }
        else
        {
            progress = 1f; // 最大レベル
        }

        affectionBarFill.style.width = new StyleLength(new Length(progress * 100f, LengthUnit.Percent));
    }

    private void UpdateValueDisplay(int currentAffection)
    {
        if (affectionValueLabel == null) return;
        affectionValueLabel.text = $"{currentAffection} / {DefaultMaxAffection}";
    }

    private int GetNextLevelRequirement(int currentLevelIndex)
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < LevelThresholds.Length)
        {
            return LevelThresholds[nextIndex];
        }
        return LevelThresholds[LevelThresholds.Length - 1];
    }

    // ========================================
    // クリーンアップ
    // ========================================

    /// <summary>
    /// リソースを解放
    /// </summary>
    public void Dispose()
    {
        // 特にクリーンアップは不要
    }
}
