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
    /// セリフを表示（現在位置）
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

    /// <summary>
    /// 指定位置にセリフを表示
    /// </summary>
    /// <param name="worldPosition">ワールド座標での表示位置</param>
    /// <param name="dialogue">セリフ内容</param>
    /// <param name="offset">位置オフセット（デフォルト: 上方向に少しずらす）</param>
    public void ShowDialogueAt(Vector3 worldPosition, string dialogue, Vector3? offset = null)
    {
        if (string.IsNullOrEmpty(dialogue)) return;

        // 位置を設定
        Vector3 finalOffset = offset ?? new Vector3(0, 0.5f, 0);
        transform.position = worldPosition + finalOffset;

        ShowDialogue(dialogue);
    }

    /// <summary>
    /// 会話モード用：タップ待ちでセリフを表示
    /// </summary>
    /// <param name="dialogue">セリフ内容</param>
    /// <param name="onComplete">表示完了後のコールバック</param>
    public void ShowDialogueWithCallback(string dialogue, System.Action onComplete)
    {
        if (string.IsNullOrEmpty(dialogue))
        {
            onComplete?.Invoke();
            return;
        }

        // 既存の表示をキャンセル
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }

        _displayCoroutine = StartCoroutine(DisplayDialogueWithCallbackCoroutine(dialogue, onComplete));
    }

    /// <summary>
    /// 指定位置に会話モード用セリフを表示
    /// </summary>
    public void ShowDialogueAtWithCallback(Vector3 worldPosition, string dialogue, System.Action onComplete, Vector3? offset = null)
    {
        Vector3 finalOffset = offset ?? new Vector3(0, 0.5f, 0);
        transform.position = worldPosition + finalOffset;

        ShowDialogueWithCallback(dialogue, onComplete);
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

    private IEnumerator DisplayDialogueWithCallbackCoroutine(string dialogue, System.Action onComplete)
    {
        _isShowing = true;
        _waitingForTap = true;
        _skipRequested = false;

        // テキスト設定
        if (bubbleText != null)
        {
            bubbleText.text = "";
        }

        // フェードイン
        yield return StartCoroutine(FadeCoroutine(0f, 1f, fadeInDuration));

        // タイプライター効果（スキップ可能）
        yield return StartCoroutine(TypewriterCoroutineSkippable(dialogue));

        // タップ待ち
        _skipRequested = false;
        while (!_skipRequested)
        {
            // 画面タップで次へ
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                _skipRequested = true;
            }
            yield return null;
        }

        // フェードアウト
        yield return StartCoroutine(FadeCoroutine(1f, 0f, fadeOutDuration));

        _isShowing = false;
        _waitingForTap = false;
        _displayCoroutine = null;

        onComplete?.Invoke();
    }

    private IEnumerator TypewriterCoroutineSkippable(string dialogue)
    {
        if (bubbleText == null) yield break;

        bubbleText.text = "";
        foreach (char c in dialogue)
        {
            // スキップリクエストがあれば全文表示
            if (_skipRequested || Input.GetMouseButtonDown(0))
            {
                bubbleText.text = dialogue;
                AdjustBubbleSize();
                _skipRequested = false; // スキップは消費
                yield break;
            }

            bubbleText.text += c;
            AdjustBubbleSize();
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    // タップ待ち状態
    private bool _waitingForTap = false;
    private bool _skipRequested = false;

    /// <summary>
    /// タップ待ち中か
    /// </summary>
    public bool IsWaitingForTap => _waitingForTap;

    /// <summary>
    /// 次へ進む（外部からタップをシミュレート）
    /// </summary>
    public void RequestSkip()
    {
        _skipRequested = true;
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
