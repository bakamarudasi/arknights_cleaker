using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// キャラクタープレハブ内の透視レイヤーを制御するコントローラー
///
/// 使い方:
/// 1. キャラプレハブのルートにアタッチ
/// 2. Inspectorでレイヤー設定を行う（layers配列）
/// 3. SetPenetrateLevel(int) で透視レベルを変更
///
/// レイヤー構成例:
/// - Layer 0: Body（素体）       → 常に表示
/// - Layer 1: Underwear（下着）  → penetrateLevel >= 3 で非表示
/// - Layer 2: Inner（インナー）  → penetrateLevel >= 2 で非表示
/// - Layer 3: Outer（上着）      → penetrateLevel >= 1 で非表示
/// </summary>
public class CharacterLayerController : MonoBehaviour
{
    [Serializable]
    public class LayerEntry
    {
        [Tooltip("レイヤー識別名（デバッグ用）")]
        public string layerName;

        [Tooltip("このレイヤーに含まれるSpriteRenderer群")]
        public SpriteRenderer[] sprites;

        [Tooltip("このレイヤーが非表示になる透視レベル（0=常に表示）")]
        [Range(0, 5)]
        public int hideAtPenetrateLevel = 0;

        [Tooltip("フェード時間（0で即時切り替え）")]
        public float fadeDuration = 0.3f;
    }

    [Header("=== レイヤー設定 ===")]
    [Tooltip("透視レイヤーの定義")]
    [SerializeField]
    private List<LayerEntry> layers = new List<LayerEntry>();

    [Header("=== 状態 ===")]
    [SerializeField, ReadOnly]
    private int _currentPenetrateLevel = 0;

    // フェード処理用
    private Dictionary<SpriteRenderer, Coroutine> _fadeCoroutines = new Dictionary<SpriteRenderer, Coroutine>();

    // イベント
    public event Action<int> OnPenetrateLevelChanged;

    // プロパティ
    public int CurrentPenetrateLevel => _currentPenetrateLevel;
    public IReadOnlyList<LayerEntry> Layers => layers;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        // 初期状態を適用（レベル0 = 通常表示）
        ApplyPenetrateLevel(0, immediate: true);
    }

    // ========================================
    // 透視レベル制御
    // ========================================

    /// <summary>
    /// 透視レベルを設定
    /// </summary>
    /// <param name="level">透視レベル（0=通常, 1〜5=透視段階）</param>
    public void SetPenetrateLevel(int level)
    {
        level = Mathf.Clamp(level, 0, 5);

        if (_currentPenetrateLevel == level) return;

        int previousLevel = _currentPenetrateLevel;
        _currentPenetrateLevel = level;

        ApplyPenetrateLevel(level, immediate: false);

        Debug.Log($"[LayerController] Penetrate level: {previousLevel} → {level}");
        OnPenetrateLevelChanged?.Invoke(level);
    }

    /// <summary>
    /// 透視レベルを即座に適用（フェードなし）
    /// </summary>
    public void SetPenetrateLevelImmediate(int level)
    {
        level = Mathf.Clamp(level, 0, 5);
        _currentPenetrateLevel = level;
        ApplyPenetrateLevel(level, immediate: true);
    }

    private void ApplyPenetrateLevel(int level, bool immediate)
    {
        foreach (var layer in layers)
        {
            bool shouldHide = layer.hideAtPenetrateLevel > 0 && level >= layer.hideAtPenetrateLevel;
            float targetAlpha = shouldHide ? 0f : 1f;

            foreach (var sprite in layer.sprites)
            {
                if (sprite == null) continue;

                if (immediate || layer.fadeDuration <= 0)
                {
                    SetSpriteAlpha(sprite, targetAlpha);
                }
                else
                {
                    StartFade(sprite, targetAlpha, layer.fadeDuration);
                }
            }
        }
    }

    // ========================================
    // フェード処理
    // ========================================

    private void StartFade(SpriteRenderer sprite, float targetAlpha, float duration)
    {
        // 既存のフェードを停止
        if (_fadeCoroutines.TryGetValue(sprite, out var existing))
        {
            if (existing != null) StopCoroutine(existing);
        }

        var coroutine = StartCoroutine(FadeCoroutine(sprite, targetAlpha, duration));
        _fadeCoroutines[sprite] = coroutine;
    }

    private System.Collections.IEnumerator FadeCoroutine(SpriteRenderer sprite, float targetAlpha, float duration)
    {
        float startAlpha = sprite.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetSpriteAlpha(sprite, alpha);
            yield return null;
        }

        SetSpriteAlpha(sprite, targetAlpha);
        _fadeCoroutines.Remove(sprite);
    }

    private void SetSpriteAlpha(SpriteRenderer sprite, float alpha)
    {
        var color = sprite.color;
        color.a = alpha;
        sprite.color = color;
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// すべてのレイヤーを表示状態にリセット
    /// </summary>
    public void ResetToNormal()
    {
        SetPenetrateLevelImmediate(0);
    }

    /// <summary>
    /// 指定レイヤー名のエントリを取得
    /// </summary>
    public LayerEntry GetLayer(string layerName)
    {
        return layers.Find(l => l.layerName == layerName);
    }

    /// <summary>
    /// レイヤー数を取得
    /// </summary>
    public int LayerCount => layers.Count;

#if UNITY_EDITOR
    [Header("=== デバッグ ===")]
    [SerializeField]
    private int debugPenetrateLevel = 0;

    [ContextMenu("Apply Debug Penetrate Level")]
    private void ApplyDebugLevel()
    {
        SetPenetrateLevelImmediate(debugPenetrateLevel);
    }

    [ContextMenu("Auto Setup Layers From Children")]
    private void AutoSetupLayers()
    {
        layers.Clear();

        // 子オブジェクトから "layer_" プレフィックスを持つものを検索
        var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var layerGroups = new Dictionary<string, List<SpriteRenderer>>();

        foreach (var sr in allRenderers)
        {
            string name = sr.gameObject.name.ToLower();

            // layer_body, layer_underwear などのパターンを検出
            if (name.StartsWith("layer_"))
            {
                string layerName = name.Substring(6); // "layer_" を除去
                if (!layerGroups.ContainsKey(layerName))
                {
                    layerGroups[layerName] = new List<SpriteRenderer>();
                }
                layerGroups[layerName].Add(sr);
            }
        }

        // レイヤーエントリを作成
        int hideLevel = 0;
        foreach (var kvp in layerGroups)
        {
            var entry = new LayerEntry
            {
                layerName = kvp.Key,
                sprites = kvp.Value.ToArray(),
                hideAtPenetrateLevel = hideLevel,
                fadeDuration = 0.3f
            };
            layers.Add(entry);
            hideLevel++;
        }

        Debug.Log($"[LayerController] Auto setup: {layers.Count} layers found");
    }
#endif
}

/// <summary>
/// ReadOnly属性（Inspector表示用）
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        UnityEditor.EditorGUI.BeginDisabledGroup(true);
        UnityEditor.EditorGUI.PropertyField(position, property, label);
        UnityEditor.EditorGUI.EndDisabledGroup();
    }
}
#endif
