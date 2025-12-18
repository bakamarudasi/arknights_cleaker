using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// マーケットニュースのデータベース
/// シュールなテキストを管理
/// </summary>
[CreateAssetMenu(fileName = "MarketNewsDatabase", menuName = "ArknightsClicker/Market/News Database")]
public class MarketNewsDatabase : ScriptableObject
{
    [Header("平常時ニュース")]
    [TextArea(1, 2)]
    public List<string> normalNews = new()
    {
        "クロージャ、また値上げ。",
        "ドクター、執務室でカップ麺をすする。",
        "ケルシー、本日も残業中。",
        "アーミヤ、会議で居眠り。",
        "龍門、今日も渋滞。",
        "ペンギン急便、配達遅延のお知らせ。",
        "エクシア、新しいアップルパイのレシピを開発中。",
        "テキサス、ポッキー在庫切れに激怒。",
        "ラップランド、また窓ガラスを割る。",
        "ドーベルマン教官、訓練生を追い回す。",
        "グラベル、ドクターの部屋の前で待機中（8時間目）。"
    };

    [Header("高騰時ニュース")]
    [TextArea(1, 2)]
    public List<string> positiveNews = new()
    {
        "【速報】ムリナールおじさん、ついに本気を出す。",
        "テキサス、ポッキー新味を絶賛。",
        "【朗報】ケルシー、珍しく笑顔を見せる。",
        "ロドス、新規契約を獲得！",
        "シルバーアッシュ、四半期決算で黒字達成。",
        "ペンギン急便、新航路を開拓。",
        "ライン生命、新薬の臨床試験に成功。",
        "カランド貿易、過去最高益を更新。",
        "【祝】アーミヤの誕生日、株価も祝福ムード。",
        "エクシア、りんご市場を独占。"
    };

    [Header("暴落時ニュース")]
    [TextArea(1, 2)]
    public List<string> negativeNews = new()
    {
        "【悲報】源石虫レースで八百長発覚。",
        "CEO、バナナの皮で転倒。",
        "【速報】レユニオン、また何かやらかす。",
        "龍門幣、紙切れ疑惑が浮上。",
        "ペンギン急便、荷物紛失事件。",
        "ライン生命、実験室で爆発音。",
        "【悲報】ドクター、また寝坊。",
        "クロージャのショップ、在庫全滅。",
        "天災が近づいています。",
        "W、どこかで笑っている。"
    };

    [Header("銘柄別ニュース")]
    public List<StockSpecificNews> stockSpecificNews = new();

    /// <summary>
    /// ランダムなニュースを取得
    /// </summary>
    public MarketNews GetRandomNews()
    {
        float roll = Random.value;

        // 70%平常、15%ポジティブ、15%ネガティブ
        if (roll < 0.70f)
        {
            return GetNewsFromList(normalNews, MarketNewsType.Normal);
        }
        else if (roll < 0.85f)
        {
            return GetNewsFromList(positiveNews, MarketNewsType.Positive, 0.05f);
        }
        else
        {
            return GetNewsFromList(negativeNews, MarketNewsType.Negative, -0.05f);
        }
    }

    /// <summary>
    /// 特定銘柄のニュースを取得
    /// </summary>
    public MarketNews GetNewsForStock(string stockId, bool positive)
    {
        var specific = stockSpecificNews.Find(s => s.stockId == stockId);
        if (specific != null)
        {
            var list = positive ? specific.positiveNews : specific.negativeNews;
            if (list.Count > 0)
            {
                string text = list[Random.Range(0, list.Count)];
                float impact = positive ? 0.1f : -0.1f;
                return new MarketNews(text, positive ? MarketNewsType.Positive : MarketNewsType.Negative, stockId, impact);
            }
        }

        // デフォルトニュース
        var defaultList = positive ? positiveNews : negativeNews;
        return GetNewsFromList(defaultList, positive ? MarketNewsType.Positive : MarketNewsType.Negative, positive ? 0.05f : -0.05f);
    }

    private MarketNews GetNewsFromList(List<string> list, MarketNewsType type, float impact = 0f)
    {
        if (list.Count == 0) return new MarketNews("...", MarketNewsType.Normal);

        string text = list[Random.Range(0, list.Count)];
        return new MarketNews(text, type, null, impact);
    }
}

/// <summary>
/// 銘柄固有のニュース
/// </summary>
[System.Serializable]
public class StockSpecificNews
{
    public string stockId;
    public string companyName;

    [TextArea(1, 2)]
    public List<string> positiveNews = new();

    [TextArea(1, 2)]
    public List<string> negativeNews = new();
}
