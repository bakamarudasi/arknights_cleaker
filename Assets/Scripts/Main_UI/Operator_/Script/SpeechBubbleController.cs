using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// キャラクターの吹き出し（セリフ表示）を管理
/// OverlayCharacterPresenterと連携して表示
/// </summary>
public class SpeechBubbleController : MonoBehaviour
{
    public static SpeechBubbleController Instance { get; private set; }

    [Header("表示設定")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("位置設定")]
    [SerializeField] private Vector2 bubbleOffset = new Vector2(150f, 200f); // キャラからのオフセット

    // UI要素
    private Canvas bubbleCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform bubbleRect;
    private Image bubbleBackground;
    private TextMeshProUGUI dialogueText;

    // 状態
    private Coroutine _displayCoroutine;
    private bool _isShowing;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CreateBubbleUI();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnsubscribeFromEvents();
    }

    // ========================================
    // UI生成
    // ========================================

    private void CreateBubbleUI()
    {
        // Canvas生成
        var canvasObj = new GameObject("SpeechBubbleCanvas");
        canvasObj.transform.SetParent(transform);

        bubbleCanvas = canvasObj.AddComponent<Canvas>();
        bubbleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        bubbleCanvas.sortingOrder = 150; // キャラより上

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 吹き出しコンテナ
        var bubbleObj = new GameObject("SpeechBubble");
        bubbleObj.transform.SetParent(bubbleCanvas.transform, false);

        bubbleRect = bubbleObj.AddComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0f, 0f); // 左下基準
        bubbleRect.sizeDelta = new Vector2(400, 120);

        canvasGroup = bubbleObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        // 背景
        bubbleBackground = bubbleObj.AddComponent<Image>();
        bubbleBackground.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // 角丸風にする場合はSpriteを設定
        // bubbleBackground.sprite = roundedSprite;
        // bubbleBackground.type = Image.Type.Sliced;

        // テキスト
        var textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(bubbleObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 10);
        textRect.offsetMax = new Vector2(-15, -10);

        dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 24;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.Left;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;

        // 初期非表示
        bubbleCanvas.gameObject.SetActive(false);
    }

    // ========================================
    // イベント購読
    // ========================================

    private void SubscribeToEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnDialogueRequested += ShowDialogue;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnDialogueRequested -= ShowDialogue;
        }
    }

    // ========================================
    // 表示処理
    // ========================================

    /// <summary>
    /// セリフを表示
    /// </summary>
    public void ShowDialogue(string dialogue)
    {
        if (string.IsNullOrEmpty(dialogue)) return;

        // 既存の表示をキャンセル
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        _displayCoroutine = StartCoroutine(DisplayDialogueCoroutine(dialogue));
    }

    private IEnumerator DisplayDialogueCoroutine(string dialogue)
    {
        _isShowing = true;
        bubbleCanvas.gameObject.SetActive(true);

        // 位置を更新
        UpdateBubblePosition();

        // フェードイン
        yield return FadeCoroutine(0, 1, fadeInDuration);

        // タイプライター効果
        yield return TypewriterCoroutine(dialogue);

        // 表示維持
        yield return new WaitForSeconds(displayDuration);

        // フェードアウト
        yield return FadeCoroutine(1, 0, fadeOutDuration);

        bubbleCanvas.gameObject.SetActive(false);
        _isShowing = false;
        _displayCoroutine = null;
    }

    private IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private IEnumerator TypewriterCoroutine(string dialogue)
    {
        dialogueText.text = "";
        foreach (char c in dialogue)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // ========================================
    // 位置更新
    // ========================================

    private void UpdateBubblePosition()
    {
        // OverlayCharacterPresenterの表示エリアを基準に配置
        var presenter = OverlayCharacterPresenter.Instance;
        if (presenter == null) return;

        // キャラの右上あたりに配置
        // TODO: presenterから表示位置を取得して調整
        bubbleRect.anchoredPosition = bubbleOffset;
    }

    /// <summary>
    /// 表示位置を設定（外部から呼び出し用）
    /// </summary>
    public void SetBubblePosition(Vector2 screenPosition)
    {
        if (bubbleRect == null) return;

        // スクリーン座標をCanvas座標に変換
        var scaler = bubbleCanvas.GetComponent<CanvasScaler>();
        float scaleFactor = 1f;
        if (scaler != null)
        {
            float widthScale = Screen.width / scaler.referenceResolution.x;
            float heightScale = Screen.height / scaler.referenceResolution.y;
            scaleFactor = Mathf.Lerp(widthScale, heightScale, scaler.matchWidthOrHeight);
        }

        bubbleRect.anchoredPosition = screenPosition / scaleFactor;
    }

    // ========================================
    // 公開プロパティ
    // ========================================

    public bool IsShowing => _isShowing;
}
