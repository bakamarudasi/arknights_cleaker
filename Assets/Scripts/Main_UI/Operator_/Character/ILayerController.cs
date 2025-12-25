using System;

/// <summary>
/// キャラクターレイヤー制御のインターフェース
/// PSB方式（CharacterLayerController）とSpine方式（SpineLayerController）の共通API
/// </summary>
public interface ILayerController
{
    // ========================================
    // プロパティ
    // ========================================

    /// <summary>現在の透視レベル（0=通常, 1〜5=透視段階）</summary>
    int CurrentPenetrateLevel { get; }

    /// <summary>マスクモード（レンズ透視）が有効か</summary>
    bool IsMaskModeActive { get; }

    /// <summary>マスクモードを使用可能か</summary>
    bool UseMaskMode { get; }

    // ========================================
    // イベント
    // ========================================

    /// <summary>透視レベル変化時</summary>
    event Action<int> OnPenetrateLevelChanged;

    // ========================================
    // 透視レベル制御
    // ========================================

    /// <summary>
    /// 透視レベルを設定（フェードあり）
    /// </summary>
    /// <param name="level">透視レベル（0=通常, 1〜5=透視段階）</param>
    void SetPenetrateLevel(int level);

    /// <summary>
    /// 透視レベルを即座に適用（フェードなし）
    /// </summary>
    void SetPenetrateLevelImmediate(int level);

    // ========================================
    // マスクモード（レンズ透視）
    // ========================================

    /// <summary>
    /// マスクモードを有効化（レンズ透視開始）
    /// </summary>
    /// <param name="penetrateLevel">透視レベル</param>
    void EnableMaskMode(int penetrateLevel);

    /// <summary>
    /// マスクモードを無効化（通常表示に戻す）
    /// </summary>
    void DisableMaskMode();

    // ========================================
    // ユーティリティ
    // ========================================

    /// <summary>
    /// すべてのレイヤーを表示状態にリセット
    /// </summary>
    void ResetToNormal();
}
