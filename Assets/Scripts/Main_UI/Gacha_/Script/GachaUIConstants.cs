using UnityEngine;

/// <summary>
/// ガチャUIで使用する定数定義
/// </summary>
public static class GachaUIConstants
{
    // ========================================
    // HDRカラー（Bloom演出用）
    // ========================================

    /// <summary>UR（★6）用のHDRカラー - オレンジ</summary>
    public static readonly Color HDR_COLOR_UR = new Color(3f, 1f, 0f, 1f);

    /// <summary>SSR（★5）用のHDRカラー - イエロー</summary>
    public static readonly Color HDR_COLOR_SSR = new Color(3f, 2.5f, 0.5f, 1f);

    /// <summary>SR（★4以下）用のHDRカラー - パープル</summary>
    public static readonly Color HDR_COLOR_SR = new Color(1.5f, 1f, 2f, 1f);

    // ========================================
    // ビーム演出設定
    // ========================================

    /// <summary>ライトビームの最大幅</summary>
    public const float BEAM_MAX_WIDTH = 600f;

    /// <summary>ビーム不透明度の倍率</summary>
    public const float BEAM_OPACITY_MULTIPLIER = 1.5f;

    /// <summary>色変化開始の進行度閾値</summary>
    public const float COLOR_TRANSITION_START = 0.2f;

    /// <summary>色変化の範囲</summary>
    public const float COLOR_TRANSITION_RANGE = 0.8f;

    /// <summary>環境光の最大不透明度</summary>
    public const float AMBIENT_LIGHT_MAX_OPACITY = 0.8f;

    // ========================================
    // パーティクル設定
    // ========================================

    /// <summary>パーティクル生成開始の進行度閾値</summary>
    public const float PARTICLE_SPAWN_START = 0.3f;

    /// <summary>パーティクル数の倍率</summary>
    public const float PARTICLE_COUNT_MULTIPLIER = 8f;

    /// <summary>大パーティクル生成確率閾値</summary>
    public const float LARGE_PARTICLE_THRESHOLD = 0.7f;

    /// <summary>ジッパーレールの幅</summary>
    public const float ZIPPER_RAIL_WIDTH = 650f;

    /// <summary>パーティクル基準X位置オフセット</summary>
    public const float PARTICLE_BASE_X_OFFSET = 50f;

    /// <summary>パーティクル基準Y位置</summary>
    public const float PARTICLE_BASE_Y = 200f;

    /// <summary>パーティクルY位置のランダム範囲</summary>
    public const float PARTICLE_Y_RANDOM_RANGE = 30f;

    /// <summary>パーティクル移動距離Y最小値</summary>
    public const float PARTICLE_MOVE_Y_MIN = 80f;

    /// <summary>パーティクル移動距離Y最大値</summary>
    public const float PARTICLE_MOVE_Y_MAX = 200f;

    /// <summary>パーティクル移動距離X範囲</summary>
    public const float PARTICLE_MOVE_X_RANGE = 50f;

    /// <summary>パーティクルアニメーション最短時間（ミリ秒）</summary>
    public const int PARTICLE_ANIM_DURATION_MIN_MS = 400;

    /// <summary>パーティクルアニメーション最長時間（ミリ秒）</summary>
    public const int PARTICLE_ANIM_DURATION_MAX_MS = 800;

    /// <summary>パーティクルアニメーション開始遅延（ミリ秒）</summary>
    public const int PARTICLE_ANIM_START_DELAY_MS = 10;

    /// <summary>パーティクル削除の追加遅延（ミリ秒）</summary>
    public const int PARTICLE_REMOVE_DELAY_MS = 50;

    // ========================================
    // スクリーンシェイク設定
    // ========================================

    /// <summary>シェイク開始の進行度閾値</summary>
    public const float SHAKE_PROGRESS_THRESHOLD = 0.9f;

    /// <summary>★6用のシェイク回数</summary>
    public const int SHAKE_COUNT_UR = 6;

    /// <summary>★5用のシェイク回数</summary>
    public const int SHAKE_COUNT_SSR = 4;

    /// <summary>★4以下用のシェイク回数</summary>
    public const int SHAKE_COUNT_DEFAULT = 2;

    /// <summary>★6用のシェイク強度</summary>
    public const float SHAKE_MAGNITUDE_UR = 12f;

    /// <summary>★5用のシェイク強度</summary>
    public const float SHAKE_MAGNITUDE_SSR = 8f;

    /// <summary>★4以下用のシェイク強度</summary>
    public const float SHAKE_MAGNITUDE_DEFAULT = 4f;

    /// <summary>シェイクY方向の強度倍率</summary>
    public const float SHAKE_Y_MAGNITUDE_RATIO = 0.3f;

    /// <summary>シェイクの間隔（ミリ秒）</summary>
    public const int SHAKE_INTERVAL_MS = 50;

    // ========================================
    // タイミング設定
    // ========================================

    /// <summary>ジッパー完了後の待機時間（ミリ秒）</summary>
    public const int ZIPPER_COMPLETE_DELAY_MS = 200;

    /// <summary>スキップ後の結果表示遅延（ミリ秒）</summary>
    public const int SKIP_RESULT_DELAY_MS = 100;

    /// <summary>結果アニメーションの基本間隔（ミリ秒）</summary>
    public const int RESULT_ANIM_BASE_INTERVAL_MS = 150;

    /// <summary>★6表示時の追加遅延（ミリ秒）</summary>
    public const int RESULT_DELAY_RARITY_6_MS = 500;

    /// <summary>★5表示時の追加遅延（ミリ秒）</summary>
    public const int RESULT_DELAY_RARITY_5_MS = 350;

    /// <summary>★4表示時の追加遅延（ミリ秒）</summary>
    public const int RESULT_DELAY_RARITY_4_MS = 200;

    /// <summary>★3以下表示時の追加遅延（ミリ秒）</summary>
    public const int RESULT_DELAY_DEFAULT_MS = 100;

    /// <summary>結果アイテム可視化の遅延（ミリ秒）</summary>
    public const int RESULT_ITEM_VISIBLE_DELAY_MS = 50;

    // ========================================
    // レア度判定
    // ========================================

    /// <summary>UR判定の最小レア度</summary>
    public const int RARITY_UR_MIN = 6;

    /// <summary>SSR判定の最小レア度</summary>
    public const int RARITY_SSR_MIN = 5;
}
