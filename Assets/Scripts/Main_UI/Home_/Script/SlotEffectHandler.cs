using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// スロット（ジャックポット）演出を担当するハンドラー
/// スロットオーバーレイ、コイン、ルーレット、昇格演出を管理
/// </summary>
public class SlotEffectHandler : IDisposable
{
    // ========================================
    // スロットランク定義
    // ========================================

    private enum SlotRank { Normal, Rare, Super, Mega, Legendary }

    // ========================================
    // UI要素参照
    // ========================================

    private readonly VisualElement _root;

    // スロットオーバーレイ
    private VisualElement _slotOverlay;
    private VisualElement _slotRainbow;
    private VisualElement _slotFlash;
    private VisualElement _slotCoinLayer;
    private Label _slotBigText;
    private VisualElement _slotRoulette;
    private Label _slotMultiplier;
    private Label _slotRankUp;
    private Label _slotBonusText;
    private Label _slotStockChance;

    // ========================================
    // スロットコインプール
    // ========================================

    private readonly List<VisualElement> _slotCoins = new();
    private const int SlotCoinCount = 20;

    // ========================================
    // コールバック参照
    // ========================================

    private UnityEngine.Events.UnityAction<int> _onSlotTriggeredCallback;

    // ========================================
    // コンストラクタ
    // ========================================

    public SlotEffectHandler(VisualElement root)
    {
        _root = root;
    }

    // ========================================
    // 初期化
    // ========================================

    public void Initialize()
    {
        QueryElements();
        BindEvents();

        LogUIController.LogSystem("SlotEffectHandler Initialized.");
    }

    private void QueryElements()
    {
        _slotOverlay = _root.Q<VisualElement>("slot-overlay");
        _slotRainbow = _root.Q<VisualElement>("slot-rainbow");
        _slotFlash = _root.Q<VisualElement>("slot-flash");
        _slotCoinLayer = _root.Q<VisualElement>("slot-coin-layer");
        _slotBigText = _root.Q<Label>("slot-big-text");
        _slotRoulette = _root.Q<VisualElement>("slot-roulette");
        _slotMultiplier = _root.Q<Label>("slot-multiplier");
        _slotRankUp = _root.Q<Label>("slot-rank-up");
        _slotBonusText = _root.Q<Label>("slot-bonus-text");
        _slotStockChance = _root.Q<Label>("slot-stock-chance");
    }

    private void BindEvents()
    {
        var gc = GameController.Instance;
        if (gc?.OnSlotTriggered == null) return;

        _onSlotTriggeredCallback = OnSlotTriggered;
        gc.OnSlotTriggered.AddListener(_onSlotTriggeredCallback);
    }

    // ========================================
    // スロット発動処理
    // ========================================

    private void OnSlotTriggered(int finalMultiplier)
    {
        // オーバーレイ表示
        _slotOverlay?.AddToClassList("active");

        // 虹色背景
        _slotRainbow?.AddToClassList("active");

        // フラッシュエフェクト
        PlaySlotFlash();

        // コイン生成
        SpawnSlotCoins();

        // ビッグテキストアニメーション
        _slotBigText?.AddToClassList("show");

        // ルーレット表示
        _slotRoulette?.AddToClassList("show");
        _slotMultiplier?.AddToClassList("spinning");

        // ルーレットアニメーション開始
        StartRouletteAnimation(finalMultiplier);

        LogUIController.Msg("<color=#FFD700><b>★★★ JACKPOT!! ★★★</b></color>");
    }

    // ========================================
    // フラッシュエフェクト
    // ========================================

    private void PlaySlotFlash()
    {
        if (_slotFlash == null) return;

        // 3連続フラッシュ
        for (int i = 0; i < 3; i++)
        {
            int delay = i * 150;
            _root.schedule.Execute(() =>
            {
                _slotFlash.AddToClassList("pulse");
                _root.schedule.Execute(() => _slotFlash.RemoveFromClassList("pulse")).ExecuteLater(80);
            }).ExecuteLater(delay);
        }
    }

    // ========================================
    // コイン生成
    // ========================================

    private void SpawnSlotCoins()
    {
        if (_slotCoinLayer == null) return;

        // 既存のコインをクリア
        foreach (var coin in _slotCoins)
        {
            _slotCoinLayer.Remove(coin);
        }
        _slotCoins.Clear();

        // 新しいコインを生成
        for (int i = 0; i < SlotCoinCount; i++)
        {
            var coin = new VisualElement();
            coin.AddToClassList("slot-coin");

            var shine = new VisualElement();
            shine.AddToClassList("slot-coin-shine");
            coin.Add(shine);

            // ランダムな開始位置（画面端から）
            bool fromLeft = UnityEngine.Random.value > 0.5f;
            float startX = fromLeft ? -50 : _slotCoinLayer.resolvedStyle.width + 50;
            float startY = UnityEngine.Random.Range(100f, _slotCoinLayer.resolvedStyle.height - 100);

            coin.style.left = startX;
            coin.style.top = startY;

            _slotCoinLayer.Add(coin);
            _slotCoins.Add(coin);

            // 中央に向かって飛ぶアニメーション
            int coinDelay = i * 50;
            float targetX = _slotCoinLayer.resolvedStyle.width / 2 + UnityEngine.Random.Range(-100f, 100f);
            float targetY = _slotCoinLayer.resolvedStyle.height / 2 + UnityEngine.Random.Range(-100f, 100f);

            _root.schedule.Execute(() =>
            {
                coin.style.translate = new Translate(targetX - startX, targetY - startY);
                coin.style.rotate = new Rotate(UnityEngine.Random.Range(180f, 720f));
                coin.AddToClassList("animate");
            }).ExecuteLater(coinDelay + 50);
        }

        // コインを削除
        _root.schedule.Execute(() =>
        {
            foreach (var coin in _slotCoins)
            {
                _slotCoinLayer.Remove(coin);
            }
            _slotCoins.Clear();
        }).ExecuteLater(2000);
    }

    // ========================================
    // ルーレットアニメーション
    // ========================================

    private void StartRouletteAnimation(int finalMultiplier)
    {
        if (_slotMultiplier == null) return;

        // 最終ランクを決定
        SlotRank finalRank = GetRankFromMultiplier(finalMultiplier);

        // 昇格演出のスケジュール
        SlotRank[] ranks = { SlotRank.Normal, SlotRank.Rare, SlotRank.Super, SlotRank.Mega, SlotRank.Legendary };
        int finalRankIndex = (int)finalRank;

        // ルーレット回転開始（各ランク内の数字でスピン）
        SlotRank displayRank = SlotRank.Normal;

        var rouletteSchedule = _root.schedule.Execute(() =>
        {
            int randomMult = GetRandomMultiplierForRank(displayRank);
            _slotMultiplier.text = $"x{randomMult}";
        }).Every(50);

        // 昇格演出をスケジュール
        int delay = 300;
        for (int i = 1; i <= finalRankIndex; i++)
        {
            int rankIndex = i;
            _root.schedule.Execute(() =>
            {
                displayRank = ranks[rankIndex];
                ShowRankUpEffect(ranks[rankIndex]);
                UpdateMultiplierStyle(ranks[rankIndex]);
            }).ExecuteLater(delay);
            delay += 350; // 各昇格間隔
        }

        // 回転停止と最終結果
        int stopDelay = delay + 200;
        _root.schedule.Execute(() =>
        {
            rouletteSchedule.Pause();

            // 最終結果を表示
            _slotMultiplier.text = $"x{finalMultiplier}";
            _slotMultiplier.RemoveFromClassList("spinning");
            UpdateMultiplierStyle(finalRank);

            // ボーナステキスト表示
            if (_slotBonusText != null)
            {
                _slotBonusText.text = $"+{finalMultiplier}x BONUS!";
                _slotBonusText.AddToClassList("show");
            }

            // 追加フラッシュ
            PlaySlotFlash();

        }).ExecuteLater(stopDelay);

        // 株チャンス表示（ボーナス表示後に出現）
        _root.schedule.Execute(() =>
        {
            if (_slotStockChance != null)
            {
                _slotStockChance.AddToClassList("show");
                // パルスアニメーション
                _root.schedule.Execute(() => _slotStockChance.AddToClassList("pulse")).ExecuteLater(400);
                _root.schedule.Execute(() => _slotStockChance.RemoveFromClassList("pulse")).ExecuteLater(600);
                _root.schedule.Execute(() => _slotStockChance.AddToClassList("pulse")).ExecuteLater(800);
                _root.schedule.Execute(() => _slotStockChance.RemoveFromClassList("pulse")).ExecuteLater(1000);
            }
        }).ExecuteLater(stopDelay + 800);

        // 非表示処理
        _root.schedule.Execute(() =>
        {
            _slotBigText?.RemoveFromClassList("show");
            _slotBonusText?.RemoveFromClassList("show");
            _slotRoulette?.RemoveFromClassList("show");
            _slotRankUp?.RemoveFromClassList("show");
            ClearMultiplierStyles();
            ClearRankUpStyles();
        }).ExecuteLater(stopDelay + 2500);

        // 株チャンスを少し長めに表示
        _root.schedule.Execute(() =>
        {
            _slotStockChance?.RemoveFromClassList("show");
            _slotStockChance?.RemoveFromClassList("pulse");
        }).ExecuteLater(stopDelay + 3500);

        _root.schedule.Execute(() =>
        {
            _slotOverlay?.RemoveFromClassList("active");
            _slotRainbow?.RemoveFromClassList("active");
        }).ExecuteLater(stopDelay + 4000);
    }

    // ========================================
    // ランク判定
    // ========================================

    private SlotRank GetRankFromMultiplier(int multiplier)
    {
        if (multiplier >= 1000) return SlotRank.Legendary;
        if (multiplier >= 100) return SlotRank.Mega;
        if (multiplier >= 51) return SlotRank.Super;
        if (multiplier >= 11) return SlotRank.Rare;
        return SlotRank.Normal;
    }

    private int GetRandomMultiplierForRank(SlotRank rank)
    {
        return rank switch
        {
            SlotRank.Normal => UnityEngine.Random.Range(2, 11),
            SlotRank.Rare => UnityEngine.Random.Range(11, 51),
            SlotRank.Super => UnityEngine.Random.Range(51, 101),
            SlotRank.Mega => UnityEngine.Random.Range(101, 1000),
            SlotRank.Legendary => UnityEngine.Random.Range(1000, 10000),
            _ => UnityEngine.Random.Range(2, 11)
        };
    }

    // ========================================
    // 昇格演出
    // ========================================

    private void ShowRankUpEffect(SlotRank rank)
    {
        if (_slotRankUp == null) return;

        // 前のスタイルをクリア
        ClearRankUpStyles();

        // 昇格テキストとスタイル設定
        string text = rank switch
        {
            SlotRank.Rare => "昇格！",
            SlotRank.Super => "SUPER昇格!",
            SlotRank.Mega => "MEGA昇格!!",
            SlotRank.Legendary => "LEGENDARY!!!",
            _ => ""
        };

        if (string.IsNullOrEmpty(text)) return;

        _slotRankUp.text = text;
        _slotRankUp.AddToClassList(rank.ToString().ToLower());
        _slotRankUp.AddToClassList("show");

        // フラッシュ
        PlaySlotFlash();

        // 昇格テキストを消す
        _root.schedule.Execute(() =>
        {
            _slotRankUp.RemoveFromClassList("show");
            _slotRankUp.AddToClassList("hide");
        }).ExecuteLater(300);

        _root.schedule.Execute(() =>
        {
            _slotRankUp.RemoveFromClassList("hide");
        }).ExecuteLater(600);
    }

    // ========================================
    // スタイル更新
    // ========================================

    private void UpdateMultiplierStyle(SlotRank rank)
    {
        if (_slotMultiplier == null) return;

        ClearMultiplierStyles();
        _slotMultiplier.RemoveFromClassList("spinning");

        string styleClass = rank switch
        {
            SlotRank.Rare => "rare",
            SlotRank.Super => "super",
            SlotRank.Mega => "mega",
            SlotRank.Legendary => "legendary",
            _ => ""
        };

        if (!string.IsNullOrEmpty(styleClass))
        {
            _slotMultiplier.AddToClassList(styleClass);
        }
    }

    private void ClearMultiplierStyles()
    {
        _slotMultiplier?.RemoveFromClassList("rare");
        _slotMultiplier?.RemoveFromClassList("super");
        _slotMultiplier?.RemoveFromClassList("mega");
        _slotMultiplier?.RemoveFromClassList("legendary");
    }

    private void ClearRankUpStyles()
    {
        _slotRankUp?.RemoveFromClassList("rare");
        _slotRankUp?.RemoveFromClassList("super");
        _slotRankUp?.RemoveFromClassList("mega");
        _slotRankUp?.RemoveFromClassList("legendary");
        _slotRankUp?.RemoveFromClassList("show");
        _slotRankUp?.RemoveFromClassList("hide");
    }

    // ========================================
    // クリーンアップ
    // ========================================

    public void Dispose()
    {
        var gc = GameController.Instance;
        if (gc?.OnSlotTriggered != null && _onSlotTriggeredCallback != null)
        {
            gc.OnSlotTriggered.RemoveListener(_onSlotTriggeredCallback);
        }

        // スロットコインクリア
        foreach (var coin in _slotCoins)
        {
            _slotCoinLayer?.Remove(coin);
        }
        _slotCoins.Clear();

        _onSlotTriggeredCallback = null;

        LogUIController.LogSystem("SlotEffectHandler Disposed.");
    }
}
