using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// キャラクターのインタラクションゾーン
/// キャラクタープレハブに配置して、タッチ/クリックを検出
/// </summary>
public class CharacterInteractionZone : MonoBehaviour
{
    public enum ZoneType
    {
        Head,       // 頭（なでなで）
        Body,       // 体（タッチ）
        Hand,       // 手（握手）
        Special     // 特殊
    }

    [Header("ゾーン設定")]
    [SerializeField] private ZoneType zoneType = ZoneType.Head;
    [SerializeField] private string zoneName = "Head";

    [Header("反応設定")]
    [Tooltip("連続タッチのコンボタイムアウト（秒）")]
    [SerializeField] private float comboTimeout = 1.5f;

    [Tooltip("好感度ボーナス（基本値）")]
    [SerializeField] private int baseAffectionBonus = 1;

    [Tooltip("コンボボーナス倍率")]
    [SerializeField] private float comboMultiplier = 0.5f;

    [Header("演出設定")]
    [Tooltip("タッチ時のパーティクル")]
    [SerializeField] private ParticleSystem touchParticle;

    [Tooltip("タッチ時の効果音")]
    [SerializeField] private AudioClip touchSound;

    [Header("セリフ設定")]
    [SerializeField] private string[] reactions = new string[]
    {
        "「...？」",
        "「...悪くないわね」",
        "「えへへ...♪」",
        "「もう...///」"
    };

    // 状態
    private int comboCount = 0;
    private float lastTouchTime;

    // イベント
    public event Action<ZoneType, int> OnZoneTouched; // (ゾーンタイプ, コンボ数)

    // Unity Events（Inspector設定用）
    [Header("イベント")]
    public UnityEvent OnTouch;
    public UnityEvent<int> OnComboTouch; // コンボ数

    // ========================================
    // プロパティ
    // ========================================

    public ZoneType Type => zoneType;
    public string ZoneName => zoneName;
    public int ComboCount => comboCount;

    // ========================================
    // タッチ検出
    // ========================================

    private void OnMouseDown()
    {
        HandleTouch();
    }

    /// <summary>
    /// タッチ処理（外部から呼び出し可能）
    /// </summary>
    public void HandleTouch()
    {
        float currentTime = Time.time;

        // コンボ判定
        if (currentTime - lastTouchTime > comboTimeout)
        {
            comboCount = 0;
        }

        comboCount++;
        lastTouchTime = currentTime;

        // 好感度処理
        int bonus = CalculateBonus();
        if (AffectionManager.Instance != null)
        {
            AffectionManager.Instance.OnHeadPetted(bonus);
        }

        // 演出
        PlayEffects();

        // セリフ表示
        ShowReaction();

        // イベント発火
        OnZoneTouched?.Invoke(zoneType, comboCount);
        OnTouch?.Invoke();
        OnComboTouch?.Invoke(comboCount);

        // コンボ表示
        if (comboCount > 1)
        {
            LogUIController.Msg($"<color=#FFD700>{zoneName}コンボ x{comboCount}!</color>");
        }
    }

    /// <summary>
    /// ボーナス計算
    /// </summary>
    private int CalculateBonus()
    {
        // 基本ボーナス + コンボボーナス
        float bonus = baseAffectionBonus + (comboCount - 1) * comboMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(bonus));
    }

    /// <summary>
    /// エフェクト再生
    /// </summary>
    private void PlayEffects()
    {
        // パーティクル
        if (touchParticle != null)
        {
            touchParticle.Play();
        }

        // 効果音
        if (touchSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(touchSound, Camera.main.transform.position, 0.5f);
        }
    }

    /// <summary>
    /// リアクション表示
    /// </summary>
    private void ShowReaction()
    {
        if (reactions == null || reactions.Length == 0) return;

        // コンボ数に応じてリアクションを選択
        int index = Mathf.Min(comboCount - 1, reactions.Length - 1);
        index = Mathf.Max(0, index);

        string reaction = reactions[index];

        // 吹き出しに表示（SpeechBubbleControllerがあれば）
        if (SpeechBubbleController.Instance != null)
        {
            SpeechBubbleController.Instance.ShowDialogue(reaction);
        }

        // ログにも表示
        LogUIController.Msg(reaction);
    }

    // ========================================
    // コンボリセット
    // ========================================

    /// <summary>
    /// コンボをリセット
    /// </summary>
    public void ResetCombo()
    {
        comboCount = 0;
    }

    // ========================================
    // Gizmo（Editor表示用）
    // ========================================

    private void OnDrawGizmos()
    {
        // ゾーンタイプに応じた色
        Color gizmoColor = zoneType switch
        {
            ZoneType.Head => new Color(1f, 0.7f, 0.8f, 0.3f),    // ピンク
            ZoneType.Body => new Color(0.7f, 0.8f, 1f, 0.3f),    // 水色
            ZoneType.Hand => new Color(1f, 0.9f, 0.7f, 0.3f),    // クリーム
            ZoneType.Special => new Color(1f, 0.8f, 0.3f, 0.3f), // 金色
            _ => new Color(1f, 1f, 1f, 0.3f)
        };

        Gizmos.color = gizmoColor;

        // Colliderがあればその範囲を表示
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            Gizmos.DrawCube(collider2D.bounds.center, collider2D.bounds.size);
        }
        else
        {
            // なければデフォルトサイズ
            Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
        }

        // ラベル表示
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, zoneName);
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        // 選択時はより濃い色で表示
        Color gizmoColor = zoneType switch
        {
            ZoneType.Head => new Color(1f, 0.5f, 0.6f, 0.6f),
            ZoneType.Body => new Color(0.5f, 0.6f, 1f, 0.6f),
            ZoneType.Hand => new Color(1f, 0.8f, 0.5f, 0.6f),
            ZoneType.Special => new Color(1f, 0.7f, 0.2f, 0.6f),
            _ => new Color(1f, 1f, 1f, 0.6f)
        };

        Gizmos.color = gizmoColor;

        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            Gizmos.DrawWireCube(collider2D.bounds.center, collider2D.bounds.size);
        }
    }
}
