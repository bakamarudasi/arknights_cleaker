using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// チュートリアルUIのスタイル設定とビルドを担当
/// </summary>
public static class TutorialUIBuilder
{
    // ========================================
    // カラー定義
    // ========================================
    private static readonly Color AccentColor = new(1f, 0.31f, 0f, 1f);
    private static readonly Color BackgroundColor = new(0.05f, 0.05f, 0.08f, 0.98f);
    private static readonly Color OverlayColor = new(0, 0, 0, 0.7f);
    private static readonly Color SecondaryTextColor = new(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color SkipButtonBorderColor = new(0.4f, 0.4f, 0.5f, 1f);
    private static readonly Color SkipButtonTextColor = new(0.7f, 0.7f, 0.75f, 1f);
    private static readonly Color SkipButtonBgColor = new(0.15f, 0.15f, 0.2f, 0.9f);

    // ========================================
    // オーバーレイ作成
    // ========================================

    public static VisualElement CreateOverlay()
    {
        var overlay = new VisualElement
        {
            name = "tutorial-overlay"
        };

        overlay.style.position = Position.Absolute;
        overlay.style.top = 0;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = OverlayColor;
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;

        return overlay;
    }

    // ========================================
    // パネル作成
    // ========================================

    public static VisualElement CreatePanel()
    {
        var panel = new VisualElement();

        panel.style.backgroundColor = BackgroundColor;
        SetBorder(panel, 2, AccentColor);
        SetPadding(panel, 20, 20, 30, 30);
        panel.style.minWidth = 400;
        panel.style.maxWidth = 500;

        return panel;
    }

    // ========================================
    // ラベル作成
    // ========================================

    public static Label CreateTitleLabel()
    {
        var label = new Label();
        label.style.fontSize = 24;
        label.style.color = AccentColor;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 15;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        return label;
    }

    public static Label CreateMessageLabel()
    {
        var label = new Label();
        label.style.fontSize = 16;
        label.style.color = Color.white;
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.marginBottom = 20;
        return label;
    }

    public static Label CreateStepIndicator()
    {
        var label = new Label();
        label.style.fontSize = 12;
        label.style.color = SecondaryTextColor;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.marginBottom = 15;
        return label;
    }

    // ========================================
    // ボタン作成
    // ========================================

    public static Button CreateSkipButton(Action onClick)
    {
        var button = new Button(onClick)
        {
            text = ">> スキップ"
        };

        button.style.backgroundColor = SkipButtonBgColor;
        SetBorder(button, 1, SkipButtonBorderColor);
        SetBorderRadius(button, 4);
        button.style.color = SkipButtonTextColor;
        SetPadding(button, 10, 10, 20, 20);
        button.style.fontSize = 14;

        return button;
    }

    public static Button CreateNextButton(Action onClick)
    {
        var button = new Button(onClick)
        {
            text = "次へ →"
        };

        button.style.backgroundColor = new Color(1f, 0.31f, 0f, 0.2f);
        SetBorder(button, 2, AccentColor);
        button.style.color = AccentColor;
        SetPadding(button, 8, 8, 25, 25);
        button.style.unityFontStyleAndWeight = FontStyle.Bold;

        return button;
    }

    public static VisualElement CreateButtonContainer()
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.justifyContent = Justify.SpaceBetween;
        return container;
    }

    // ========================================
    // ヘルパー
    // ========================================

    private static void SetBorder(VisualElement element, float width, Color color)
    {
        element.style.borderTopWidth = width;
        element.style.borderBottomWidth = width;
        element.style.borderLeftWidth = width;
        element.style.borderRightWidth = width;
        element.style.borderTopColor = color;
        element.style.borderBottomColor = color;
        element.style.borderLeftColor = color;
        element.style.borderRightColor = color;
    }

    private static void SetBorderRadius(VisualElement element, float radius)
    {
        element.style.borderTopLeftRadius = radius;
        element.style.borderTopRightRadius = radius;
        element.style.borderBottomLeftRadius = radius;
        element.style.borderBottomRightRadius = radius;
    }

    private static void SetPadding(VisualElement element, float top, float bottom, float left, float right)
    {
        element.style.paddingTop = top;
        element.style.paddingBottom = bottom;
        element.style.paddingLeft = left;
        element.style.paddingRight = right;
    }
}
