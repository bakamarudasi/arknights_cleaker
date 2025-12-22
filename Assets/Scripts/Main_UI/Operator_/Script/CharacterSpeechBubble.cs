using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// キャラクタープレハブ内に配置する吹き出し
/// RenderTexture内で一緒に描画される
/// </summary>
public class CharacterSpeechBubble : MonoBehaviour
{
    public static CharacterSpeechBubble Instance { get; private set; }

    [Header("表示設定")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("コンポーネント参照")]
    [SerializeField] private SpriteRenderer bubbleSprite;
    [SerializeField] private TextMeshPro bubbleText;

    [Header("サイズ設定")]
    [SerializeField] private float padding = 0.2f;
    [SerializeField] private float minWidth = 2f;
    [SerializeField] private float maxWidth = 5f;
    [SerializeField] private float heightPerLine = 0.5f;

    [Header("レイヤー設定")]
    [SerializeField] private int sortingOrder = 100; // PSBより上に表示

    // 状態
    private Coroutine _displayCoroutine;
    private bool _isShowing;
    private Color _originalSpriteColor;
    private Color _originalTextColor;

    // ========================================
    // プロパティ
    // ========================================

    public bool IsShowing => _isShowing;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        // シングルトン（現在表示中のキャラ用）
        Instance = this;

        // 自動で子オブジェクトを検索
        if (bubbleSprite == null)
            bubbleSprite = GetComponentInChildren<SpriteRenderer>();
        if (bubbleText == null)
            bubbleText = GetComponentInChildren<TextMeshPro>();

        // 初期色を保存
        if (bubbleSprite != null)
        {
            _originalSpriteColor = bubbleSprite.color;
            bubbleSprite.sortingOrder = sortingOrder;
        }
        if (bubbleText != null)
        {
            _originalTextColor = bubbleText.color;
            bubbleText.sortingOrder = sortingOrder + 1; // テキストは吹き出しより上
        }

        // 初期状態は非表示
        SetAlpha(0f);
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

        // テキスト設定（サイズ計算用に先に設定）
        if (bubbleText != null)
        {
            bubbleText.text = "";
        }

        // フェードイン
        yield return StartCoroutine(FadeCoroutine(0f, 1f, fadeInDuration));

        // タイプライター効果
        yield return StartCoroutine(TypewriterCoroutine(dialogue));

        // 表示維持
        yield return new WaitForSeconds(displayDuration);

        // フェードアウト
        yield return StartCoroutine(FadeCoroutine(1f, 0f, fadeOutDuration));

        _isShowing = false;
        _displayCoroutine = null;
    }

    private IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(from, to, t);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(to);
    }

    private IEnumerator TypewriterCoroutine(string dialogue)
    {
        if (bubbleText == null) yield break;

        bubbleText.text = "";
        foreach (char c in dialogue)
        {
            bubbleText.text += c;
            AdjustBubbleSize();
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // ========================================
    // ヘルパー
    // ========================================

    private void SetAlpha(float alpha)
    {
        if (bubbleSprite != null)
        {
            Color c = _originalSpriteColor;
            c.a = _originalSpriteColor.a * alpha;
            bubbleSprite.color = c;
        }

        if (bubbleText != null)
        {
            Color c = _originalTextColor;
            c.a = _originalTextColor.a * alpha;
            bubbleText.color = c;
        }
    }

    private void AdjustBubbleSize()
    {
        if (bubbleSprite == null || bubbleText == null) return;

        // テキストのサイズを取得
        Vector2 textSize = bubbleText.GetPreferredValues();

        // 吹き出しサイズを計算
        float width = Mathf.Clamp(textSize.x + padding * 2, minWidth, maxWidth);
        float height = textSize.y + padding * 2;

        // SpriteRendererのサイズを変更（9-sliceスプライトの場合）
        bubbleSprite.size = new Vector2(width, height);
    }

    /// <summary>
    /// 即座に非表示
    /// </summary>
    public void Hide()
    {
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
            _displayCoroutine = null;
        }

        SetAlpha(0f);
        _isShowing = false;
    }
}
