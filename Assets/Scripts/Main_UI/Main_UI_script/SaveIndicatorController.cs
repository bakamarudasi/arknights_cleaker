using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// セーブインジケーターコントローラー
/// 画面右上に「Saving...」「Saved!」を表示
/// </summary>
public class SaveIndicatorController : MonoBehaviour
{
    public static SaveIndicatorController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Settings")]
    [SerializeField] private float showDuration = 1.5f;

    // UI要素
    private VisualElement saveIndicator;
    private Label saveText;
    private VisualElement saveSpinner;

    // 状態
    private IVisualElementScheduledItem _hideSchedule;
    private IVisualElementScheduledItem _spinnerSchedule;
    private float _spinnerRotation;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null)
        {
            Initialize(uiDocument.rootVisualElement);
        }
    }

    public void Initialize(VisualElement root)
    {
        saveIndicator = root.Q<VisualElement>("save-indicator");
        saveText = root.Q<Label>("save-text");
        saveSpinner = root.Q<VisualElement>("save-spinner");

        // スピナーアニメーション設定
        if (saveSpinner != null && root != null)
        {
            _spinnerSchedule = root.schedule.Execute(() =>
            {
                if (saveIndicator != null && saveIndicator.ClassListContains("show") && !saveIndicator.ClassListContains("saved"))
                {
                    _spinnerRotation += 15f;
                    saveSpinner.style.rotate = new Rotate(_spinnerRotation);
                }
            }).Every(30);
        }
    }

    /// <summary>
    /// セーブ開始を表示
    /// </summary>
    public void ShowSaving()
    {
        if (saveIndicator == null) return;

        // 前のスケジュールをキャンセル
        _hideSchedule?.Pause();

        // 状態リセット
        saveIndicator.RemoveFromClassList("saved");
        saveIndicator.AddToClassList("show");

        if (saveText != null)
        {
            saveText.text = "Saving...";
        }
    }

    /// <summary>
    /// セーブ完了を表示（その後自動で非表示）
    /// </summary>
    public void ShowSaved()
    {
        if (saveIndicator == null) return;

        // 完了状態に変更
        saveIndicator.AddToClassList("saved");

        if (saveText != null)
        {
            saveText.text = "Saved!";
        }

        // 一定時間後に非表示
        _hideSchedule = saveIndicator.schedule.Execute(() =>
        {
            Hide();
        }).ExecuteLater((long)(showDuration * 1000));
    }

    /// <summary>
    /// セーブして完了表示（一連の流れ）
    /// </summary>
    public void ShowSaveSequence(float savingDuration = 0.3f)
    {
        ShowSaving();

        // 少し待ってから完了表示
        saveIndicator?.schedule.Execute(() =>
        {
            ShowSaved();
        }).ExecuteLater((long)(savingDuration * 1000));
    }

    /// <summary>
    /// インジケーターを非表示
    /// </summary>
    public void Hide()
    {
        if (saveIndicator == null) return;

        saveIndicator.RemoveFromClassList("show");
        saveIndicator.RemoveFromClassList("saved");
    }

    private void OnDestroy()
    {
        _hideSchedule?.Pause();
        _spinnerSchedule?.Pause();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
