using UnityEngine;
using System;
using System.Collections.Generic;

// ============================================
// spine-unity 導入後に有効化
// ============================================
// using Spine.Unity;

/// <summary>
/// Spine用レイヤーコントローラー
/// 複数のSkeletonAnimationを重ねてレンズ透視機能を実現
///
/// 使い方:
/// 1. キャラプレハブのルートにアタッチ
/// 2. Inspectorでレイヤー設定を行う（layers配列）
/// 3. 各レイヤーにSkeletonAnimationとスキン名を設定
/// 4. SetPenetrateLevel(int) で透視レベルを変更
///
/// レイヤー構成例:
/// - Layer 0: skin="naked"      → 常に表示（最背面）
/// - Layer 1: skin="underwear"  → penetrateLevel >= 3 で非表示
/// - Layer 2: skin="normal"     → penetrateLevel >= 1 で非表示（最前面）
/// </summary>
public class SpineLayerController : MonoBehaviour, ILayerController
{
    // ========================================
    // spine-unity 導入後に有効化
    // ========================================
    // [Serializable]
    // public class SpineLayerEntry
    // {
    //     [Tooltip("レイヤー識別名（デバッグ用）")]
    //     public string layerName;
    //
    //     [Tooltip("このレイヤーのSkeletonAnimation")]
    //     public SkeletonAnimation skeleton;
    //
    //     [Tooltip("使用するスキン名")]
    //     public string skinName;
    //
    //     [Tooltip("このレイヤーが非表示になる透視レベル（0=常に表示）")]
    //     [Range(0, 5)]
    //     public int hideAtPenetrateLevel = 0;
    //
    //     [Tooltip("通常時のマテリアル")]
    //     public Material normalMaterial;
    //
    //     [Tooltip("マスクモード時のマテリアル（ステンシル対応）")]
    //     public Material maskedMaterial;
    // }

    [Serializable]
    public class SpineLayerEntry
    {
        [Tooltip("レイヤー識別名（デバッグ用）")]
        public string layerName;

        [Tooltip("このレイヤーのSkeletonAnimation（spine-unity導入後にSkeletonAnimation型に変更）")]
        public GameObject skeletonObject;

        [Tooltip("使用するスキン名")]
        public string skinName;

        [Tooltip("このレイヤーが非表示になる透視レベル（0=常に表示）")]
        [Range(0, 5)]
        public int hideAtPenetrateLevel = 0;

        [Tooltip("通常時のマテリアル")]
        public Material normalMaterial;

        [Tooltip("マスクモード時のマテリアル（ステンシル対応）")]
        public Material maskedMaterial;
    }

    [Header("=== レイヤー設定 ===")]
    [Tooltip("Spineレイヤーの定義（背面から前面の順）")]
    [SerializeField]
    private List<SpineLayerEntry> layers = new List<SpineLayerEntry>();

    [Header("=== 共通設定 ===")]
    [Tooltip("マスクモードを使用可能か")]
    [SerializeField]
    private bool useMaskMode = true;

    [Header("=== 状態（ReadOnly）===")]
    [SerializeField]
    private int _currentPenetrateLevel = 0;

    [SerializeField]
    private bool _isMaskModeActive = false;

    // イベント
    public event Action<int> OnPenetrateLevelChanged;

    // ILayerController プロパティ
    public int CurrentPenetrateLevel => _currentPenetrateLevel;
    public bool IsMaskModeActive => _isMaskModeActive;
    public bool UseMaskMode => useMaskMode;

    // ========================================
    // 初期化
    // ========================================

    private void Awake()
    {
        InitializeLayers();
        ApplyPenetrateLevel(0);
    }

    private void InitializeLayers()
    {
        foreach (var layer in layers)
        {
            if (layer.skeletonObject == null) continue;

            // スキンを適用
            // spine-unity導入後:
            // var skeleton = layer.skeletonObject.GetComponent<SkeletonAnimation>();
            // skeleton.skeleton.SetSkin(layer.skinName);
            // skeleton.skeleton.SetSlotsToSetupPose();

            // 通常マテリアルを適用
            ApplyMaterial(layer, false);
        }
    }

    // ========================================
    // ILayerController 実装
    // ========================================

    public void SetPenetrateLevel(int level)
    {
        level = Mathf.Clamp(level, 0, 5);
        if (_currentPenetrateLevel == level) return;

        int previousLevel = _currentPenetrateLevel;
        _currentPenetrateLevel = level;

        ApplyPenetrateLevel(level);

        Debug.Log($"[SpineLayerController] Penetrate level: {previousLevel} → {level}");
        OnPenetrateLevelChanged?.Invoke(level);
    }

    public void SetPenetrateLevelImmediate(int level)
    {
        level = Mathf.Clamp(level, 0, 5);
        _currentPenetrateLevel = level;
        ApplyPenetrateLevel(level);
    }

    public void EnableMaskMode(int penetrateLevel)
    {
        if (!useMaskMode) return;

        _isMaskModeActive = true;
        _currentPenetrateLevel = Mathf.Clamp(penetrateLevel, 1, 5);

        // マスク対応マテリアルを適用
        foreach (var layer in layers)
        {
            bool isTarget = layer.hideAtPenetrateLevel > 0
                         && _currentPenetrateLevel >= layer.hideAtPenetrateLevel;

            if (isTarget)
            {
                // 透視対象 → マスクマテリアル（穴が開く）
                ApplyMaterial(layer, true);
            }
            else
            {
                // 透視結果 → 通常マテリアル
                ApplyMaterial(layer, false);
            }
        }

        Debug.Log($"[SpineLayerController] MaskMode enabled - Level: {_currentPenetrateLevel}");
    }

    public void DisableMaskMode()
    {
        if (!_isMaskModeActive) return;

        _isMaskModeActive = false;
        _currentPenetrateLevel = 0;

        // 全レイヤーを通常マテリアルに戻す
        foreach (var layer in layers)
        {
            ApplyMaterial(layer, false);
            if (layer.skeletonObject != null)
            {
                layer.skeletonObject.SetActive(true);
            }
        }

        Debug.Log("[SpineLayerController] MaskMode disabled");
    }

    public void ResetToNormal()
    {
        SetPenetrateLevelImmediate(0);
        DisableMaskMode();
    }

    // ========================================
    // 内部処理
    // ========================================

    private void ApplyPenetrateLevel(int level)
    {
        foreach (var layer in layers)
        {
            if (layer.skeletonObject == null) continue;

            bool shouldHide = layer.hideAtPenetrateLevel > 0
                           && level >= layer.hideAtPenetrateLevel;

            layer.skeletonObject.SetActive(!shouldHide);
        }
    }

    private void ApplyMaterial(SpineLayerEntry layer, bool useMasked)
    {
        if (layer.skeletonObject == null) return;

        var material = useMasked ? layer.maskedMaterial : layer.normalMaterial;
        if (material == null) return;

        // MeshRendererにマテリアルを適用
        var renderer = layer.skeletonObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    // ========================================
    // アニメーション同期
    // ========================================

    /// <summary>
    /// 全レイヤーで同じアニメーションを同期再生
    /// </summary>
    public void PlayAnimation(string animationName, bool loop)
    {
        // spine-unity導入後に実装:
        // foreach (var layer in layers)
        // {
        //     if (layer.skeletonObject == null) continue;
        //     var skeleton = layer.skeletonObject.GetComponent<SkeletonAnimation>();
        //     var track = skeleton.AnimationState.SetAnimation(0, animationName, loop);
        // }

        Debug.Log($"[SpineLayerController] PlayAnimation: {animationName} (loop={loop})");
    }

    /// <summary>
    /// 全レイヤーのアニメーション時間を同期
    /// </summary>
    public void SyncAnimationTime()
    {
        // spine-unity導入後に実装:
        // if (layers.Count < 2) return;
        //
        // var primary = layers[0].skeletonObject?.GetComponent<SkeletonAnimation>();
        // if (primary == null) return;
        //
        // float trackTime = primary.AnimationState.GetCurrent(0)?.TrackTime ?? 0f;
        //
        // for (int i = 1; i < layers.Count; i++)
        // {
        //     var skeleton = layers[i].skeletonObject?.GetComponent<SkeletonAnimation>();
        //     if (skeleton == null) continue;
        //     var track = skeleton.AnimationState.GetCurrent(0);
        //     if (track != null) track.TrackTime = trackTime;
        // }
    }

    // ========================================
    // ボーン位置取得（吹き出し配置用）
    // ========================================

    [Header("=== 吹き出しアンカー設定 ===")]
    [Tooltip("ボーン名が見つからない場合のデフォルト位置（ローカル座標）")]
    [SerializeField]
    private Vector3 defaultBubbleOffset = new Vector3(0, 2f, 0);

    /// <summary>
    /// 指定ボーン名のワールド座標を取得
    /// </summary>
    /// <param name="boneName">Spineボーン名（head, mouth など）</param>
    /// <returns>ワールド座標</returns>
    public Vector3 GetBoneWorldPosition(string boneName)
    {
        if (string.IsNullOrEmpty(boneName) || layers.Count == 0)
        {
            return transform.position + defaultBubbleOffset;
        }

        // spine-unity導入後に実装:
        // var skeleton = layers[0].skeletonObject?.GetComponent<SkeletonAnimation>();
        // if (skeleton != null)
        // {
        //     var bone = skeleton.Skeleton.FindBone(boneName);
        //     if (bone != null)
        //     {
        //         return new Vector3(bone.WorldX, bone.WorldY, 0) + transform.position;
        //     }
        // }

        // 仮実装: 固定位置を返す
        // ボーン名に応じてオフセットを調整
        Vector3 offset = boneName.ToLower() switch
        {
            "head" => new Vector3(0, 2.5f, 0),
            "mouth" => new Vector3(0, 2.0f, 0),
            "top" => new Vector3(0, 3.0f, 0),
            _ => defaultBubbleOffset
        };

        return transform.position + offset;
    }

    /// <summary>
    /// 吹き出し表示用のデフォルト位置を取得
    /// </summary>
    public Vector3 GetDefaultBubblePosition()
    {
        return GetBoneWorldPosition("head");
    }

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// レイヤー数を取得
    /// </summary>
    public int LayerCount => layers.Count;

    /// <summary>
    /// 指定レイヤー名のエントリを取得
    /// </summary>
    public SpineLayerEntry GetLayer(string layerName)
    {
        return layers.Find(l => l.layerName == layerName);
    }

    /// <summary>
    /// 指定インデックスのレイヤーを取得
    /// </summary>
    public SpineLayerEntry GetLayerByIndex(int index)
    {
        if (index < 0 || index >= layers.Count) return null;
        return layers[index];
    }

#if UNITY_EDITOR
    [Header("=== デバッグ ===")]
    [SerializeField]
    private int debugPenetrateLevel = 0;

    [ContextMenu("Apply Debug Penetrate Level")]
    private void ApplyDebugLevel()
    {
        SetPenetrateLevelImmediate(debugPenetrateLevel);
    }

    [ContextMenu("Test MaskMode")]
    private void TestMaskMode()
    {
        if (_isMaskModeActive)
        {
            DisableMaskMode();
        }
        else
        {
            EnableMaskMode(debugPenetrateLevel);
        }
    }
#endif
}
