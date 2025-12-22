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
    [Header("=== 自動セットアップ設定 ===")]
    [Tooltip("レイヤー検出用のプレフィックス（カンマ区切りで複数指定可）")]
    [SerializeField]
    private string layerPrefixes = "layer_,lv_,l_";

    [Header("=== デバッグ ===")]
    [SerializeField]
    private int debugPenetrateLevel = 0;

    // 既知のレイヤー名と透視レベルのマッピング
    private static readonly Dictionary<string, int> KnownLayerLevels = new Dictionary<string, int>
    {
        // 素体系（常に表示）
        { "body", 0 },
        { "base", 0 },
        { "skin", 0 },
        { "face", 0 },
        { "hair", 0 },

        // 下着系（Lv3で非表示）
        { "underwear", 3 },
        { "uw", 3 },
        { "bra", 3 },
        { "panty", 3 },
        { "panties", 3 },

        // インナー系（Lv2で非表示）
        { "inner", 2 },
        { "undershirt", 2 },
        { "tanktop", 2 },

        // 上着系（Lv1で非表示）
        { "outer", 1 },
        { "clothes", 1 },
        { "jacket", 1 },
        { "shirt", 1 },
        { "dress", 1 },
        { "uniform", 1 },
        { "top", 1 },
        { "bottom", 1 },
        { "skirt", 1 },
        { "pants", 1 },
    };

    [ContextMenu("Apply Debug Penetrate Level")]
    private void ApplyDebugLevel()
    {
        SetPenetrateLevelImmediate(debugPenetrateLevel);
    }

    [ContextMenu("Auto Setup - プレフィックス検出")]
    private void AutoSetupByPrefix()
    {
        AutoSetupLayers(usePrefix: true, useKeyword: false);
    }

    [ContextMenu("Auto Setup - キーワード検出")]
    private void AutoSetupByKeyword()
    {
        AutoSetupLayers(usePrefix: false, useKeyword: true);
    }

    [ContextMenu("Auto Setup - 両方で検出")]
    private void AutoSetupBoth()
    {
        AutoSetupLayers(usePrefix: true, useKeyword: true);
    }

    [ContextMenu("全SpriteRendererを「その他」に追加")]
    private void AutoSetupAllAsOther()
    {
        var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        // 既存のレイヤーに含まれていないものだけ追加
        var existingSprites = new HashSet<SpriteRenderer>();
        foreach (var layer in layers)
        {
            foreach (var sr in layer.sprites)
            {
                existingSprites.Add(sr);
            }
        }

        var others = new List<SpriteRenderer>();
        foreach (var sr in allRenderers)
        {
            if (!existingSprites.Contains(sr))
            {
                others.Add(sr);
            }
        }

        if (others.Count > 0)
        {
            var entry = new LayerEntry
            {
                layerName = "other",
                sprites = others.ToArray(),
                hideAtPenetrateLevel = 0,
                fadeDuration = 0.3f
            };
            layers.Add(entry);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[LayerController] Added {others.Count} sprites to 'other' layer");
        }
    }

    private void AutoSetupLayers(bool usePrefix, bool useKeyword)
    {
        var prefixList = layerPrefixes.Split(',');
        var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var layerGroups = new Dictionary<string, List<SpriteRenderer>>();

        foreach (var sr in allRenderers)
        {
            string name = sr.gameObject.name.ToLower();
            string detectedLayer = null;

            // プレフィックス検出: "layer_body_arm" → "body"
            if (usePrefix)
            {
                foreach (var prefix in prefixList)
                {
                    string p = prefix.Trim().ToLower();
                    if (string.IsNullOrEmpty(p)) continue;

                    if (name.StartsWith(p))
                    {
                        string rest = name.Substring(p.Length);
                        // 次のセパレータまでを取得
                        int sepIndex = rest.IndexOfAny(new char[] { '_', '-', '.' });
                        detectedLayer = sepIndex > 0 ? rest.Substring(0, sepIndex) : rest;
                        break;
                    }
                }
            }

            // キーワード検出: "jacket_front" → "jacket" (→ outer扱い)
            if (useKeyword && detectedLayer == null)
            {
                foreach (var kvp in KnownLayerLevels)
                {
                    if (name.Contains(kvp.Key))
                    {
                        detectedLayer = kvp.Key;
                        break;
                    }
                }
            }

            if (detectedLayer != null)
            {
                if (!layerGroups.ContainsKey(detectedLayer))
                {
                    layerGroups[detectedLayer] = new List<SpriteRenderer>();
                }
                layerGroups[detectedLayer].Add(sr);
            }
        }

        // 既存レイヤーをクリアせず、マージするかどうか
        if (layers.Count > 0)
        {
            bool merge = UnityEditor.EditorUtility.DisplayDialog(
                "レイヤー設定",
                "既存のレイヤー設定があります。\n\n・上書き：既存設定をクリアして新規作成\n・マージ：既存設定に追加",
                "上書き", "マージ");

            if (merge)
            {
                // 既存レイヤー名を取得
                var existingNames = new HashSet<string>();
                foreach (var l in layers)
                {
                    existingNames.Add(l.layerName.ToLower());
                }

                // 新規のみ追加
                foreach (var kvp in layerGroups)
                {
                    if (!existingNames.Contains(kvp.Key))
                    {
                        var entry = CreateLayerEntry(kvp.Key, kvp.Value);
                        layers.Add(entry);
                    }
                }

                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"[LayerController] Merged: {layerGroups.Count} layer groups processed");
                return;
            }
        }

        // 上書きモード
        layers.Clear();
        foreach (var kvp in layerGroups)
        {
            var entry = CreateLayerEntry(kvp.Key, kvp.Value);
            layers.Add(entry);
        }

        // hideAtPenetrateLevelでソート（0が先頭）
        layers.Sort((a, b) => a.hideAtPenetrateLevel.CompareTo(b.hideAtPenetrateLevel));

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[LayerController] Auto setup complete: {layers.Count} layers found");

        // 結果をログ出力
        foreach (var layer in layers)
        {
            Debug.Log($"  - {layer.layerName}: {layer.sprites.Length} sprites, hideAt={layer.hideAtPenetrateLevel}");
        }
    }

    private LayerEntry CreateLayerEntry(string layerName, List<SpriteRenderer> sprites)
    {
        // 既知のレイヤー名からhideAtPenetrateLevelを決定
        int hideLevel = 0;
        string lowerName = layerName.ToLower();

        foreach (var kvp in KnownLayerLevels)
        {
            if (lowerName.Contains(kvp.Key))
            {
                hideLevel = kvp.Value;
                break;
            }
        }

        return new LayerEntry
        {
            layerName = layerName,
            sprites = sprites.ToArray(),
            hideAtPenetrateLevel = hideLevel,
            fadeDuration = 0.3f
        };
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
