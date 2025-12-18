using UnityEngine;
using System;

/// <summary>
/// 株価シミュレーションエンジン
/// 幾何ブラウン運動 + Mertonジャンプ拡散モデル
/// 純粋な計算ロジック（状態を持たない）
/// </summary>
public static class StockPriceEngine
{
    // Box-Muller法用のキャッシュ
    private static bool hasSpare = false;
    private static double spare;

    /// <summary>
    /// 次の株価を計算（1ティック分）
    /// </summary>
    /// <param name="currentPrice">現在の株価</param>
    /// <param name="drift">ドリフト（年率、正で右肩上がり）</param>
    /// <param name="volatility">ボラティリティ（年率）</param>
    /// <param name="deltaTime">時間間隔（秒）</param>
    /// <param name="jumpProb">ジャンプ発生確率（1ティックあたり）</param>
    /// <param name="jumpIntensity">ジャンプの強度</param>
    /// <param name="minPrice">最低株価</param>
    /// <param name="maxPrice">最高株価（0 = 無制限）</param>
    /// <returns>新しい株価</returns>
    public static double CalculateNextPrice(
        double currentPrice,
        float drift,
        float volatility,
        float deltaTime,
        float jumpProb = 0f,
        float jumpIntensity = 0f,
        double minPrice = 1,
        double maxPrice = 0)
    {
        // 時間スケールを年換算から秒換算に変換
        // 1年 = 365日 * 24時間 * 60分 * 60秒 = 31,536,000秒
        // ただしゲームでは1秒を1日程度とみなす
        float timeScale = deltaTime / 86400f; // 1日 = 86400秒として計算

        // 幾何ブラウン運動（GBM）
        // dS = μSdt + σSdW
        // 離散化: S(t+dt) = S(t) * exp((μ - σ²/2)dt + σ√dt * Z)
        double z = GenerateStandardNormal();

        double driftTerm = (drift - 0.5 * volatility * volatility) * timeScale;
        double diffusionTerm = volatility * Math.Sqrt(timeScale) * z;

        double newPrice = currentPrice * Math.Exp(driftTerm + diffusionTerm);

        // Mertonジャンプ拡散
        // ランダムにジャンプが発生
        if (jumpProb > 0 && UnityEngine.Random.value < jumpProb)
        {
            // ジャンプの方向（上下半々、ただし上方向に若干バイアス）
            double jumpDirection = UnityEngine.Random.value < 0.45 ? -1 : 1;

            // ジャンプの大きさ（指数分布）
            // Random.valueが1.0の場合のLog(0)防止
            double randomVal = Math.Min(UnityEngine.Random.value, 0.9999);
            double jumpMagnitude = -Math.Log(1 - randomVal) * jumpIntensity;

            newPrice *= (1 + jumpDirection * jumpMagnitude);
        }

        // 価格の下限・上限を適用
        newPrice = Math.Max(newPrice, minPrice);
        if (maxPrice > 0)
        {
            newPrice = Math.Min(newPrice, maxPrice);
        }

        return newPrice;
    }

    /// <summary>
    /// 複数ティック分を一括計算（履歴生成用）
    /// </summary>
    public static double[] GeneratePriceHistory(
        double initialPrice,
        float drift,
        float volatility,
        float deltaTime,
        int tickCount,
        float jumpProb = 0f,
        float jumpIntensity = 0f,
        double minPrice = 1,
        double maxPrice = 0)
    {
        double[] history = new double[tickCount];
        double price = initialPrice;

        for (int i = 0; i < tickCount; i++)
        {
            price = CalculateNextPrice(price, drift, volatility, deltaTime,
                jumpProb, jumpIntensity, minPrice, maxPrice);
            history[i] = price;
        }

        return history;
    }

    /// <summary>
    /// 株価の変動率を計算
    /// </summary>
    public static double CalculateChangeRate(double currentPrice, double previousPrice)
    {
        if (previousPrice <= 0) return 0;
        return (currentPrice - previousPrice) / previousPrice;
    }

    /// <summary>
    /// 株価の変動率をパーセント文字列で取得
    /// </summary>
    public static string FormatChangeRate(double changeRate)
    {
        string sign = changeRate >= 0 ? "+" : "";
        return $"{sign}{changeRate * 100:F2}%";
    }

    /// <summary>
    /// 変動率に基づいて色を取得
    /// </summary>
    public static Color GetChangeColor(double changeRate)
    {
        if (changeRate > 0.05) return new Color(0.2f, 1f, 0.2f); // 明るい緑
        if (changeRate > 0) return new Color(0.4f, 0.9f, 0.4f);  // 緑
        if (changeRate > -0.05) return new Color(0.9f, 0.4f, 0.4f); // 赤
        return new Color(1f, 0.2f, 0.2f); // 明るい赤
    }

    /// <summary>
    /// 株価をフォーマット
    /// </summary>
    public static string FormatPrice(double price)
    {
        if (price >= 1_000_000_000) return $"{price / 1_000_000_000:F2}B";
        if (price >= 1_000_000) return $"{price / 1_000_000:F2}M";
        if (price >= 1_000) return $"{price / 1_000:F2}K";
        return $"{price:F2}";
    }

    /// <summary>
    /// 外部イベントによる株価変動を計算
    /// </summary>
    public static double ApplyEventImpact(
        double currentPrice,
        float impactStrength,
        bool isPositive,
        double minPrice = 1,
        double maxPrice = 0)
    {
        double multiplier = isPositive ? (1 + impactStrength) : (1 - impactStrength);
        double newPrice = currentPrice * multiplier;

        newPrice = Math.Max(newPrice, minPrice);
        if (maxPrice > 0)
        {
            newPrice = Math.Min(newPrice, maxPrice);
        }

        return newPrice;
    }

    /// <summary>
    /// 物理買い支え効果を計算（クリック連打）
    /// </summary>
    public static double ApplyBuySupport(double currentPrice, int clickCount, float supportStrength = 0.001f)
    {
        // クリック数に応じて株価を微増（対数的に減衰）
        double boost = Math.Log(1 + clickCount) * supportStrength;
        return currentPrice * (1 + boost);
    }

    /// <summary>
    /// 標準正規分布に従う乱数を生成（Box-Muller法）
    /// </summary>
    private static double GenerateStandardNormal()
    {
        if (hasSpare)
        {
            hasSpare = false;
            return spare;
        }

        double u, v, s;
        do
        {
            u = UnityEngine.Random.value * 2.0 - 1.0;
            v = UnityEngine.Random.value * 2.0 - 1.0;
            s = u * u + v * v;
        } while (s >= 1.0 || s == 0.0);

        s = Math.Sqrt(-2.0 * Math.Log(s) / s);
        spare = v * s;
        hasSpare = true;

        return u * s;
    }

    /// <summary>
    /// 期待リターンを計算（情報表示用）
    /// </summary>
    public static double CalculateExpectedReturn(float drift, float volatility, float timeHorizon)
    {
        // E[S(t)] = S(0) * exp(μt)
        return Math.Exp(drift * timeHorizon) - 1;
    }

    /// <summary>
    /// リスク（標準偏差）を計算（情報表示用）
    /// </summary>
    public static double CalculateRisk(float volatility, float timeHorizon)
    {
        return volatility * Math.Sqrt(timeHorizon);
    }
}

/// <summary>
/// 株価のスナップショット
/// 特定時点での株価情報を保持
/// </summary>
[Serializable]
public struct StockPriceSnapshot
{
    public string stockId;
    public double price;
    public double previousPrice;
    public double changeRate;
    public long timestamp; // Unix timestamp

    public StockPriceSnapshot(string id, double price, double prevPrice)
    {
        stockId = id;
        this.price = price;
        previousPrice = prevPrice;
        changeRate = StockPriceEngine.CalculateChangeRate(price, prevPrice);
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
