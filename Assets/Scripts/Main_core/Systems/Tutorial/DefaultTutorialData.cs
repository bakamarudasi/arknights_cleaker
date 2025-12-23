using System.Collections.Generic;

/// <summary>
/// デフォルトチュートリアルデータ
/// ScriptableObjectがない場合のフォールバック
/// </summary>
public static class DefaultTutorialData
{
    public static List<TutorialSequenceRuntime> GetDefaultTutorials()
    {
        return new List<TutorialSequenceRuntime>
        {
            CreateMarketTutorial(),
            CreateHomeTutorial(),
            CreateOperatorTutorial()
        };
    }

    private static TutorialSequenceRuntime CreateMarketTutorial()
    {
        return new TutorialSequenceRuntime
        {
            sequenceId = "market_basic",
            sequenceName = "株式市場入門",
            steps = new List<TutorialStep>
            {
                Step("market_1", "マーケットへようこそ！",
                    "ここでは株式を売買できます。\n安く買って高く売れば利益が出ます。\n逆だと損失です...気をつけて！"),
                Step("market_2", "所持金（LMD）",
                    "左上に所持金が表示されています。\n株を買うにはLMDが必要です。\nまずは少額から始めましょう！",
                    "lmd-value", TutorialPosition.Right),
                Step("market_3", "銘柄リスト",
                    "右側に売買できる銘柄が並んでいます。\nクリックすると詳細が見れます。\n各企業には特徴があります。",
                    "stock-list", TutorialPosition.Left),
                Step("market_4", "チャート",
                    "中央のチャートで価格推移が見れます。\n緑 = 上昇トレンド\n赤 = 下落トレンド",
                    "chart-canvas", TutorialPosition.Bottom),
                Step("market_5", "売買方法",
                    "1. 数量を入力（10, 100, MAXボタンも便利）\n2. BUYで購入 / SELLで売却\n\n保有中は「利確」「損切り」表示に変わります。",
                    "trade-panel", TutorialPosition.Top),
                Step("market_6", "ニュース＆イベント",
                    "株価はニュースやイベントで変動します。\n・ポジティブニュース → 上昇傾向\n・ネガティブニュース → 下落傾向\n\nログをチェックしよう！"),
                Step("market_7", "スキル（イカサマ）",
                    "困ったときの奥の手！\n・物理買い支え: 株価を少し上げる\n・インサイダー: 数秒先が見える\n\nクールダウンがあるので計画的に！",
                    "skill-panel", TutorialPosition.Left),
                Step("market_8", "ロドス株",
                    "左側の「RHODES ISLAND」は特別な株。\nクリック連打で株価が上がり、\n定期的に配当がもらえます！\n\nランクを上げると報酬UP！",
                    "rhodos-panel", TutorialPosition.Right),
                Step("market_9", "まとめ",
                    "・安く買って高く売る\n・ニュースに注目\n・損切りは早めに\n・ロドス株で安定収入\n\nグッドラック！")
            }
        };
    }

    private static TutorialSequenceRuntime CreateHomeTutorial()
    {
        return new TutorialSequenceRuntime
        {
            sequenceId = "home_basic",
            sequenceName = "ホーム画面の使い方",
            steps = new List<TutorialStep>
            {
                Step("home_1", "ようこそ、ドクター！",
                    "ロドス・アイランドへようこそ！\nここがメイン画面です。\n各種機能にアクセスできます。"),
                Step("home_2", "サイドバー",
                    "左のアイコンから：\n・ホーム - この画面\n・オペレーター - キャラ交流\n・マーケット - 株式売買\n・ショップ - アイテム購入",
                    "sidebar", TutorialPosition.Right),
                Step("home_3", "ステータス表示",
                    "画面上部に所持金（LMD）や\n理性（SP）が表示されています。\n\nSPはクリックで回復します！",
                    "status-bar", TutorialPosition.Bottom),
                Step("home_4", "オペレーター表示",
                    "中央にオペレーターが表示されます。\nクリックすると反応してくれます！\n\n好感度を上げると特別なイベントも...",
                    "operator-display"),
                Step("home_5", "設定メニュー",
                    "右上の歯車アイコンから\n各種設定ができます。\n\n・音量調整\n・表示設定\n・データ管理",
                    "settings-btn", TutorialPosition.Left),
                Step("home_6", "次のステップ",
                    "まずはオペレーターと交流して\n好感度を上げてみましょう！\n\nマーケットで資金を増やすのも◎")
            }
        };
    }

    private static TutorialSequenceRuntime CreateOperatorTutorial()
    {
        return new TutorialSequenceRuntime
        {
            sequenceId = "operator_basic",
            sequenceName = "オペレーター画面",
            steps = new List<TutorialStep>
            {
                Step("operator_1", "オペレーターとの出会い",
                    "ここでキャラクターと交流できます。\nオペレーターは様々な反応をしてくれます。\n\nまずはクリックしてみましょう！"),
                Step("operator_2", "好感度システム",
                    "クリックや撫でると好感度UP！\n\n好感度が上がると：\n・新しいセリフ解放\n・新しい衣装解放\n・特別なイベント発生",
                    "affection-gauge", TutorialPosition.Top),
                Step("operator_3", "プレゼント",
                    "アイテムをプレゼントすると\n好感度が大きく上がります！\n\nキャラごとに好みがあるので\n反応を見ながら覚えよう。",
                    "gift-btn", TutorialPosition.Right),
                Step("operator_4", "スキン変更",
                    "衣装ボタンから\nオペレーターの見た目を変更できます。\n\n好感度を上げると\n新しいスキンが解放されます！",
                    "skin-btn", TutorialPosition.Right),
                Step("operator_5", "ボイス再生",
                    "オペレーターのボイスを聴けます。\n\n・タップボイス\n・挨拶ボイス\n・特殊ボイス\n\n好感度で解放されるものも！",
                    "voice-btn", TutorialPosition.Left),
                Step("operator_6", "プロフィール",
                    "オペレーターの詳細情報を確認できます。\n\n・基本情報\n・経歴\n・能力値\n\n推しの情報をチェック！",
                    "profile-btn", TutorialPosition.Left),
                Step("operator_7", "オペレーター切替",
                    "他のオペレーターに切り替えられます。\n\n解放済みのオペレーターから\n好きなキャラを選んで交流しよう！",
                    "operator-select", TutorialPosition.Bottom),
                Step("operator_8", "楽しみ方",
                    "・毎日話しかけてボーナス獲得\n・プレゼントで好感度を効率UP\n・イベントで特別なシーンを解放\n\nお気に入りのオペレーターと\n仲良くなろう！")
            }
        };
    }

    private static TutorialStep Step(string id, string title, string message,
        string highlight = null, TutorialPosition pos = TutorialPosition.Center)
    {
        return new TutorialStep
        {
            id = id,
            title = title,
            message = message,
            highlightElement = highlight,
            position = pos
        };
    }
}

/// <summary>
/// ランタイム用チュートリアルシーケンス
/// </summary>
public class TutorialSequenceRuntime
{
    public string sequenceId;
    public string sequenceName;
    public List<TutorialStep> steps = new();
}
