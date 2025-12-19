/// <summary>
/// ショップUIで使用する定数定義
/// </summary>
public static class ShopUIConstants
{
    // ========================================
    // アニメーション設定
    // ========================================

    /// <summary>通貨ドラムロールの更新間隔（ミリ秒）</summary>
    public const int CURRENCY_ANIMATION_INTERVAL_MS = 30;

    /// <summary>タイプライターエフェクトの更新間隔（ミリ秒）</summary>
    public const int TYPEWRITER_INTERVAL_MS = 20;

    /// <summary>通貨アニメーションの収束閾値</summary>
    public const double CURRENCY_ANIMATION_THRESHOLD = 0.1;

    /// <summary>通貨アニメーションのスムージング係数（0-1）</summary>
    public const double CURRENCY_ANIMATION_SMOOTHING = 0.2;

    /// <summary>通貨アニメーションの最小変化量</summary>
    public const double CURRENCY_ANIMATION_MIN_STEP = 1.0;

    // ========================================
    // ホールドボタン設定
    // ========================================

    /// <summary>ホールドボタンの初期遅延（ミリ秒）</summary>
    public const int HOLD_BUTTON_INITIAL_DELAY_MS = 400;

    /// <summary>×1ボタンのリピート間隔（ミリ秒）</summary>
    public const int HOLD_BUTTON_X1_INTERVAL_MS = 80;

    /// <summary>×10ボタンのリピート間隔（ミリ秒）</summary>
    public const int HOLD_BUTTON_X10_INTERVAL_MS = 50;

    // ========================================
    // リスト表示設定
    // ========================================

    /// <summary>リストアイテムの固定高さ</summary>
    public const int LIST_ITEM_HEIGHT = 64;

    // ========================================
    // エフェクト設定
    // ========================================

    /// <summary>アイコンバウンスアニメーションの長さ（ミリ秒）</summary>
    public const int ICON_BOUNCE_DURATION_MS = 300;

    /// <summary>エフェクトフラッシュの長さ（ミリ秒）</summary>
    public const int EFFECT_FLASH_DURATION_MS = 400;

    /// <summary>フラッシュオーバーレイの不透明度</summary>
    public const float FLASH_OVERLAY_OPACITY = 0.4f;

    /// <summary>フラッシュオーバーレイのフェード開始遅延（ミリ秒）</summary>
    public const int FLASH_FADE_START_DELAY_MS = 50;

    /// <summary>フラッシュオーバーレイのフェード時間（ミリ秒）</summary>
    public const int FLASH_FADE_DURATION_MS = 200;

    /// <summary>フラッシュオーバーレイの削除遅延（ミリ秒）</summary>
    public const int FLASH_REMOVE_DELAY_MS = 300;
}
