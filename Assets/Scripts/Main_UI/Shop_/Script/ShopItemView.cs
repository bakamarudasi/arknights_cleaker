using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ショップリストのアイテム1行分を制御するViewクラス
/// </summary>
public class ShopItemView
{
    // ルート要素
    public VisualElement Root { get; private set; }

    // UI要素のキャッシュ
    private VisualElement iconElement;
    private Label nameLabel;
    private Label levelLabel;
    private VisualElement levelBar;
    private VisualElement levelBarFill;
    private Label costLabel;
    private Label effectPreviewLabel;

    /// <summary>
    /// コンストラクタ：UI要素の生成と参照の取得を行う
    /// </summary>
    public ShopItemView()
    {
        // ルート要素作成
        Root = new VisualElement();
        Root.AddToClassList("upgrade-item");

        // 斜め装飾（右上）
        var deco = new VisualElement();
        deco.AddToClassList("upgrade-item-deco");
        deco.pickingMode = PickingMode.Ignore;
        Root.Add(deco);

        // アイコン
        iconElement = new VisualElement { name = "icon" };
        iconElement.AddToClassList("upgrade-icon");

        // 情報エリア（名前・レベル・レベルバー）
        var info = new VisualElement { name = "info" };
        info.AddToClassList("upgrade-info");

        nameLabel = new Label { name = "name" };
        nameLabel.AddToClassList("upgrade-name");

        // レベル表示コンテナ（レベル + レベルバー）
        var levelContainer = new VisualElement();
        levelContainer.AddToClassList("upgrade-level-container");

        levelLabel = new Label { name = "level" };
        levelLabel.AddToClassList("upgrade-level");

        // レベルバー
        levelBar = new VisualElement();
        levelBar.AddToClassList("upgrade-level-bar");

        levelBarFill = new VisualElement();
        levelBarFill.AddToClassList("upgrade-level-bar-fill");
        levelBar.Add(levelBarFill);

        levelContainer.Add(levelLabel);
        levelContainer.Add(levelBar);

        info.Add(nameLabel);
        info.Add(levelContainer);

        // コストエリア（コスト + 効果プレビュー）
        var costArea = new VisualElement { name = "cost-area" };
        costArea.AddToClassList("upgrade-cost-area");

        costLabel = new Label { name = "cost" };
        costLabel.AddToClassList("upgrade-cost");

        effectPreviewLabel = new Label { name = "effect-preview" };
        effectPreviewLabel.AddToClassList("upgrade-effect-preview");

        costArea.Add(costLabel);
        costArea.Add(effectPreviewLabel);

        // 組み立て
        Root.Add(iconElement);
        Root.Add(info);
        Root.Add(costArea);

        // このクラス自身をuserDataに入れておく（Controllerから取り出せるように）
        Root.userData = this;
    }

    /// <summary>
    /// データを表示に反映する
    /// </summary>
    public void Bind(UpgradeData data)
    {
        if (data == null) return;

        var gc = GameController.Instance;
        int level = gc.GetUpgradeLevel(data.id);
        double cost = data.GetCostAtLevel(level);
        UpgradeState state = gc.GetUpgradeState(data);
        bool isMax = data.IsMaxLevel(level);
        bool isUnlimited = data.maxLevel <= 0;
        int maxLevel = isUnlimited ? 0 : data.maxLevel;

        // テキスト更新
        nameLabel.text = data.displayName;
        if (isMax)
        {
            levelLabel.text = "MAX";
        }
        else if (isUnlimited)
        {
            levelLabel.text = $"Lv.{level} (∞)";
        }
        else
        {
            levelLabel.text = $"Lv.{level}/{maxLevel}";
        }
        costLabel.text = isMax ? "-" : $"{cost:N0}";

        // レベルバーの進捗を更新（無限アップグレードの場合は非表示）
        if (isUnlimited)
        {
            levelBar.style.display = DisplayStyle.None;
        }
        else
        {
            float progress = maxLevel > 0 ? (float)level / maxLevel : 0f;
            levelBarFill.style.width = new Length(progress * 100f, LengthUnit.Percent);
            levelBar.style.display = DisplayStyle.Flex;
        }

        // 効果プレビュー（次レベルの効果増加量）
        if (!isMax)
        {
            double nextEffect = data.effectValue;
            string effectStr = data.isPercentDisplay
                ? $"+{nextEffect * 100:F1}%"
                : $"+{nextEffect:F1}";
            effectPreviewLabel.text = effectStr;
            effectPreviewLabel.style.display = DisplayStyle.Flex;
        }
        else
        {
            effectPreviewLabel.style.display = DisplayStyle.None;
        }

        // アイコン更新
        if (data.icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(data.icon);
        }
        else
        {
            iconElement.style.backgroundImage = null;
        }

        // 状態に応じたスタイル適用
        ApplyStateStyle(state);
    }

    /// <summary>
    /// 状態に応じた見た目の変更
    /// </summary>
    private void ApplyStateStyle(UpgradeState state)
    {
        // 一旦クラスを全削除
        Root.RemoveFromClassList("state-locked");
        Root.RemoveFromClassList("state-affordable");
        Root.RemoveFromClassList("state-not-afford");
        Root.RemoveFromClassList("state-max");

        // 状態ごとのクラス付与
        switch (state)
        {
            case UpgradeState.Locked:
                Root.AddToClassList("state-locked");
                break;
            case UpgradeState.ReadyToUpgrade:
                Root.AddToClassList("state-affordable");
                break;
            case UpgradeState.CanUnlockButNotAfford:
                Root.AddToClassList("state-not-afford");
                break;
            case UpgradeState.MaxLevel:
                Root.AddToClassList("state-max");
                break;
        }
    }
}