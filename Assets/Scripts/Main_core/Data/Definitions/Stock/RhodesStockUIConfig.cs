using System;
using UnityEngine;

/// <summary>
/// ロドス株のUI演出設定
/// </summary>
[Serializable]
public class RhodesStockUIConfig
{
    [Header("キャラクター表情連動")]
    [Tooltip("株価低下時にアーミヤの表情が曇る")]
    public bool enableAmiyaMoodSync = true;

    [Tooltip("表情が変わる株価閾値（低い順）")]
    public float[] moodThresholds = { 30f, 50f, 70f, 90f };

    [Header("ニュースティッカー")]
    [Tooltip("暴落時のニュースメッセージ")]
    [TextArea]
    public string[] crashNewsMessages = {
        "【噂】ドクター、実はただの無職か？ 投資家が疑念",
        "【速報】ロドス製薬、資金繰り悪化の噂",
        "【独自】関係者「ドクターは最近寝てばかり」"
    };

    [Tooltip("急騰時のニュースメッセージ")]
    [TextArea]
    public string[] boomNewsMessages = {
        "【速報】ロドス製薬、驚異の業績！ドクターの手腕に注目",
        "【市況】ロドス株がストップ高、投資家殺到",
        "【独自】ドクター「まだまだこれから」と自信"
    };

    [Header("乗っ取り演出")]
    [Tooltip("経営権喪失時のUI変更を有効化")]
    public bool enableTakeoverUI = true;

    [Tooltip("乗っ取り時のメッセージ")]
    public string takeoverMessage = "新経営陣より：ドクターの権限は一時的に制限されています。";

    [Header("危機演出")]
    [Tooltip("株価急落時の警報サイレン演出")]
    public bool enableCrisisAlarm = true;

    [Tooltip("危機時にオペレーターが励ましてくれる")]
    public bool enableOperatorEncouragement = true;

    [Tooltip("励ましメッセージ（ランダム選択）")]
    [TextArea]
    public string[] encouragementMessages = {
        "ドクター、私たちがついています。（アーミヤ）",
        "経営状況は芳しくありませんが、諦めるのはまだ早い。（ケルシー）",
        "ボス、金がなくてもペンギン急便は止まらないぜ！（テキサス）",
        "ドクター殿、逆境こそが真の力を試す時でござる！（シラユキ）"
    };

    [Header("株価ストリーク演出")]
    [Tooltip("連続上昇時の特殊演出を有効化")]
    public bool enableStreakEffects = true;

    [Tooltip("ストリーク達成時のメッセージ")]
    public string[] streakMessages = {
        "3連騰！調子いいですね、ドクター！",
        "5連騰！投資家の信頼が高まっています！",
        "7連騰！伝説のドクターと呼ばれ始めました！",
        "10連騰！！ロドスの黄金時代到来か！？"
    };

    // ========================================
    // ヘルパーメソッド（重複パターン統一）
    // ========================================

    private static string GetRandomMessage(string[] messages, string defaultMessage)
    {
        if (messages == null || messages.Length == 0)
            return defaultMessage;
        return messages[UnityEngine.Random.Range(0, messages.Length)];
    }

    /// <summary>
    /// 株価に対応する表情レベルを取得（0=最悪, 配列長=最高）
    /// </summary>
    public int GetMoodLevel(float stockPrice)
    {
        if (moodThresholds == null || moodThresholds.Length == 0)
            return 2; // デフォルト：普通

        int level = 0;
        foreach (var threshold in moodThresholds)
        {
            if (stockPrice >= threshold) level++;
            else break;
        }
        return level;
    }

    /// <summary>
    /// 暴落時のランダムニュースを取得
    /// </summary>
    public string GetRandomCrashNews()
        => GetRandomMessage(crashNewsMessages, "【速報】市場が不安定な状況です");

    /// <summary>
    /// 急騰時のランダムニュースを取得
    /// </summary>
    public string GetRandomBoomNews()
        => GetRandomMessage(boomNewsMessages, "【速報】市場が好調です");

    /// <summary>
    /// 励ましメッセージをランダム取得
    /// </summary>
    public string GetRandomEncouragement()
        => GetRandomMessage(encouragementMessages, "頑張りましょう、ドクター。");

    /// <summary>
    /// 連騰数に対応するストリークメッセージを取得
    /// </summary>
    public string GetStreakMessage(int streakCount)
    {
        if (streakMessages == null || streakMessages.Length == 0)
            return $"{streakCount}連騰達成！";

        // 3, 5, 7, 10連騰に対応
        int index = streakCount switch
        {
            >= 10 => 3,
            >= 7 => 2,
            >= 5 => 1,
            >= 3 => 0,
            _ => -1
        };

        if (index < 0 || index >= streakMessages.Length)
            return $"{streakCount}連騰達成！";

        return streakMessages[index];
    }

    /// <summary>
    /// 乗っ取りメッセージを取得（null安全）
    /// </summary>
    public string GetTakeoverMessage()
    {
        return string.IsNullOrEmpty(takeoverMessage)
            ? "経営権が移譲されました。"
            : takeoverMessage;
    }
}
