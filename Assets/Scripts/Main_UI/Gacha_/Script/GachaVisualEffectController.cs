using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ガチャUIの視覚効果を担当するコントローラー
/// - ライトビーム演出
/// - パーティクル生成
/// - 画面シェイク
/// </summary>
public class GachaVisualEffectController
{
    // ========================================
    // UI要素
    // ========================================

    private VisualElement root;
    private VisualElement bagScreen;
    private VisualElement lightBeam;
    private VisualElement ambientLight;
    private VisualElement particleContainer;

    // ========================================
    // 状態
    // ========================================

    private bool hasShaken = false;
    private int maxRarity = 3;

    // ========================================
    // 初期化
    // ========================================

    /// <summary>
    /// コントローラーを初期化
    /// </summary>
    public void Initialize(VisualElement rootElement, VisualElement bag, VisualElement beam, VisualElement ambient, VisualElement particles)
    {
        root = rootElement;
        bagScreen = bag;
        lightBeam = beam;
        ambientLight = ambient;
        particleContainer = particles;
    }

    /// <summary>
    /// 演出開始前のリセット
    /// </summary>
    public void Reset()
    {
        hasShaken = false;
        if (lightBeam != null)
        {
            lightBeam.style.width = 0;
            lightBeam.style.opacity = 0;
        }
        if (ambientLight != null)
        {
            ambientLight.style.backgroundColor = new StyleColor(Color.clear);
        }
        particleContainer?.Clear();
    }

    /// <summary>
    /// 最高レア度を設定
    /// </summary>
    public void SetMaxRarity(int rarity)
    {
        maxRarity = rarity;
    }

    // ========================================
    // ビジュアル更新
    // ========================================

    /// <summary>
    /// ジッパーの進行度に応じた視覚更新
    /// </summary>
    /// <param name="progress">0-1の進行度</param>
    /// <returns>シェイクが発生したかどうか</returns>
    public bool UpdateVisuals(float progress)
    {
        if (lightBeam == null) return false;

        // 光のビームを伸ばす
        lightBeam.style.width = GachaUIConstants.BEAM_MAX_WIDTH * progress;
        lightBeam.style.opacity = Mathf.Clamp01(progress * GachaUIConstants.BEAM_OPACITY_MULTIPLIER);

        // 進行度が一定を超えたら、最高レア度に応じた色を漏れ出させる
        if (progress > GachaUIConstants.COLOR_TRANSITION_START)
        {
            Color baseColor = Color.white;
            Color targetColor = GetHDRColor(maxRarity);
            float t = (progress - GachaUIConstants.COLOR_TRANSITION_START) / GachaUIConstants.COLOR_TRANSITION_RANGE;

            // ビーム色の遷移
            lightBeam.style.unityBackgroundImageTintColor = Color.Lerp(baseColor, targetColor, t);

            // 周囲の環境光
            if (ambientLight != null)
            {
                ambientLight.style.backgroundColor = targetColor;
                ambientLight.style.opacity = t * GachaUIConstants.AMBIENT_LIGHT_MAX_OPACITY;
            }

            // パーティクル発生
            SpawnParticles(progress);
        }

        // 高レア時：完了間近でシェイク（1回だけ）
        if (progress > GachaUIConstants.SHAKE_PROGRESS_THRESHOLD && maxRarity >= GachaUIConstants.RARITY_SSR_MIN && !hasShaken)
        {
            hasShaken = true;
            PlayScreenShake(maxRarity);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 演出を即座に完了させる（スキップ時）
    /// </summary>
    public void ForceComplete()
    {
        if (lightBeam != null)
        {
            lightBeam.style.width = GachaUIConstants.BEAM_MAX_WIDTH;
            lightBeam.style.opacity = 1f;
            lightBeam.style.unityBackgroundImageTintColor = GetHDRColor(maxRarity);
        }

        if (maxRarity >= GachaUIConstants.RARITY_SSR_MIN)
        {
            PlayScreenShake(maxRarity);
        }
    }

    // ========================================
    // パーティクル演出
    // ========================================

    private void SpawnParticles(float progress)
    {
        if (particleContainer == null || progress < GachaUIConstants.PARTICLE_SPAWN_START) return;

        int particleCount = (int)((progress - GachaUIConstants.PARTICLE_SPAWN_START) * GachaUIConstants.PARTICLE_COUNT_MULTIPLIER);

        for (int i = 0; i < particleCount; i++)
        {
            var particle = new VisualElement();
            particle.AddToClassList("particle");

            // 高レアリティなら大きいパーティクル
            if (maxRarity >= GachaUIConstants.RARITY_UR_MIN && Random.value > GachaUIConstants.LARGE_PARTICLE_THRESHOLD)
            {
                particle.AddToClassList("particle-large");
            }

            // レアリティに応じた色
            Color particleColor = GetHDRColor(maxRarity);
            particle.style.backgroundColor = particleColor;

            // ランダム位置
            float openWidth = GachaUIConstants.ZIPPER_RAIL_WIDTH * progress;
            float xPos = Random.Range(0f, openWidth) + GachaUIConstants.PARTICLE_BASE_X_OFFSET;
            float yPos = GachaUIConstants.PARTICLE_BASE_Y + Random.Range(-GachaUIConstants.PARTICLE_Y_RANDOM_RANGE, GachaUIConstants.PARTICLE_Y_RANDOM_RANGE);

            particle.style.position = Position.Absolute;
            particle.style.left = xPos;
            particle.style.top = yPos;
            particle.style.opacity = 1f;

            particleContainer.Add(particle);

            // アニメーション
            float targetY = yPos - Random.Range(GachaUIConstants.PARTICLE_MOVE_Y_MIN, GachaUIConstants.PARTICLE_MOVE_Y_MAX);
            float targetX = xPos + Random.Range(-GachaUIConstants.PARTICLE_MOVE_X_RANGE, GachaUIConstants.PARTICLE_MOVE_X_RANGE);
            int duration = Random.Range(GachaUIConstants.PARTICLE_ANIM_DURATION_MIN_MS, GachaUIConstants.PARTICLE_ANIM_DURATION_MAX_MS);

            root.schedule.Execute(() =>
            {
                particle.style.translate = new Translate(targetX - xPos, targetY - yPos, 0);
                particle.style.opacity = 0f;
                particle.style.transitionProperty = new List<StylePropertyName>
                {
                    new StylePropertyName("translate"),
                    new StylePropertyName("opacity")
                };
                particle.style.transitionDuration = new List<TimeValue>
                {
                    new TimeValue(duration, TimeUnit.Millisecond)
                };
            }).ExecuteLater(GachaUIConstants.PARTICLE_ANIM_START_DELAY_MS);

            // 削除
            root.schedule.Execute(() =>
            {
                if (particleContainer.Contains(particle))
                    particleContainer.Remove(particle);
            }).ExecuteLater(duration + GachaUIConstants.PARTICLE_REMOVE_DELAY_MS);
        }
    }

    // ========================================
    // 画面シェイク演出
    // ========================================

    private void PlayScreenShake(int intensity)
    {
        if (bagScreen == null) return;

        int shakeCount = intensity >= GachaUIConstants.RARITY_UR_MIN ? GachaUIConstants.SHAKE_COUNT_UR
                       : (intensity >= GachaUIConstants.RARITY_SSR_MIN ? GachaUIConstants.SHAKE_COUNT_SSR
                       : GachaUIConstants.SHAKE_COUNT_DEFAULT);
        float magnitude = intensity >= GachaUIConstants.RARITY_UR_MIN ? GachaUIConstants.SHAKE_MAGNITUDE_UR
                        : (intensity >= GachaUIConstants.RARITY_SSR_MIN ? GachaUIConstants.SHAKE_MAGNITUDE_SSR
                        : GachaUIConstants.SHAKE_MAGNITUDE_DEFAULT);
        int delay = 0;

        for (int i = 0; i < shakeCount; i++)
        {
            int currentDelay = delay;
            float offsetX = (i % 2 == 0 ? 1 : -1) * magnitude * (1f - (float)i / shakeCount);
            float offsetY = Random.Range(-magnitude * GachaUIConstants.SHAKE_Y_MAGNITUDE_RATIO, magnitude * GachaUIConstants.SHAKE_Y_MAGNITUDE_RATIO);

            root.schedule.Execute(() =>
            {
                bagScreen.style.translate = new Translate(offsetX, offsetY, 0);
            }).ExecuteLater(currentDelay);

            delay += GachaUIConstants.SHAKE_INTERVAL_MS;
        }

        // 元に戻す
        root.schedule.Execute(() =>
        {
            bagScreen.style.translate = new Translate(0, 0, 0);
        }).ExecuteLater(delay);
    }

    // ========================================
    // ヘルパー
    // ========================================

    /// <summary>
    /// レア度に応じたHDRカラーを取得
    /// </summary>
    public Color GetHDRColor(int rarity)
    {
        return rarity switch
        {
            >= GachaUIConstants.RARITY_UR_MIN => GachaUIConstants.HDR_COLOR_UR,
            >= GachaUIConstants.RARITY_SSR_MIN => GachaUIConstants.HDR_COLOR_SSR,
            _ => GachaUIConstants.HDR_COLOR_SR
        };
    }
}
