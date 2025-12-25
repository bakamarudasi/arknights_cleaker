using UnityEngine;
using System;

/// <summary>
/// ズームアップ窓の設定データ
/// CharacterSceneDataに含めてシーンごとに設定
/// </summary>
[Serializable]
public class ZoomTargetConfig
{
    [Header("=== 基本設定 ===")]
    [Tooltip("トリガーとなるゾーン")]
    public CharacterInteractionZone.ZoneType triggerZone;

    [Tooltip("ズーム表示用のプレハブ（SpineLayerController付き）")]
    public GameObject zoomPrefab;

    [Header("=== 表示設定 ===")]
    [Tooltip("窓サイズ（画面比率: 0.3 = 30%）")]
    public Vector2 windowSizeRatio = new Vector2(0.35f, 0.45f);

    [Tooltip("窓位置アンカー（0,0=左下, 1,1=右上）")]
    public Vector2 windowAnchor = new Vector2(0.7f, 0.5f);

    [Tooltip("ズーム用カメラのOrthographicSize")]
    public float cameraSize = 2f;

    [Header("=== アニメーション名 ===")]
    [Tooltip("待機アニメーション")]
    public string animIdle = "idle";

    [Tooltip("開始アニメーション（1回再生）")]
    public string animEnter = "touch_start";

    [Tooltip("ループアニメーション")]
    public string animLoop = "touch_loop";

    [Tooltip("クライマックスアニメーション（1回再生）")]
    public string animClimax = "touch_climax";

    [Tooltip("終了アニメーション（1回再生）")]
    public string animExit = "touch_end";

    [Header("=== メイン立ち絵連動 ===")]
    [Tooltip("ズーム中のメイン立ち絵アニメーション")]
    public string mainAnimWhileZoom = "being_touched";

    [Header("=== トリガー条件 ===")]
    [Tooltip("ズーム窓を表示する最小コンボ数")]
    public int minComboToShow = 2;

    [Tooltip("クライマックスに移行するコンボ数")]
    public int comboForClimax = 10;

    [Tooltip("興奮度がこの値以上でクライマックス")]
    public float excitementForClimax = 80f;

    [Header("=== タイミング ===")]
    [Tooltip("タッチがない場合のタイムアウト（秒）")]
    public float timeout = 3f;

    [Tooltip("フェードイン時間（秒）")]
    public float fadeInDuration = 0.3f;

    [Tooltip("フェードアウト時間（秒）")]
    public float fadeOutDuration = 0.2f;
}

/// <summary>
/// ズーム窓のアニメーション状態
/// </summary>
public enum ZoomAnimationState
{
    Idle,       // 非表示
    Enter,      // 開始アニメ中
    Loop,       // ループ中
    Climax,     // クライマックス中
    Exit        // 終了アニメ中
}
