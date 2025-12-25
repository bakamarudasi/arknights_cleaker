using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// オペレーター画面のタブ管理
/// タブの開閉、アクティブ状態の管理を担当
/// </summary>
public class OperatorTabController
{
    private readonly Dictionary<string, TabInfo> _tabs = new Dictionary<string, TabInfo>();
    private readonly UICallbackRegistry _callbacks = new UICallbackRegistry();

    private class TabInfo
    {
        public VisualElement Panel;
        public Button Icon;
        public Button CloseButton;
        public bool IsOpen;
    }

    /// <summary>
    /// タブを登録
    /// </summary>
    public void RegisterTab(string name, VisualElement panel, Button icon, Button closeButton = null)
    {
        if (string.IsNullOrEmpty(name) || panel == null) return;

        var tab = new TabInfo
        {
            Panel = panel,
            Icon = icon,
            CloseButton = closeButton,
            IsOpen = !panel.ClassListContains("hidden")
        };

        _tabs[name] = tab;

        // コールバック登録
        if (icon != null)
        {
            _callbacks.RegisterClick(icon, () => Toggle(name));
        }

        if (closeButton != null)
        {
            _callbacks.RegisterClick(closeButton, () => Close(name));
        }
    }

    /// <summary>
    /// ルート要素からタブを自動検出して登録
    /// 命名規則: tab-{name}, tab-icon-{name}, btn-close-{name}
    /// </summary>
    public void AutoRegister(VisualElement root, params string[] tabNames)
    {
        foreach (var name in tabNames)
        {
            var panel = root.Q<VisualElement>($"tab-{name}");
            var icon = root.Q<Button>($"tab-icon-{name}");
            var close = root.Q<Button>($"btn-close-{name}");

            if (panel != null)
            {
                RegisterTab(name, panel, icon, close);
            }
        }
    }

    /// <summary>
    /// タブを開閉トグル
    /// </summary>
    public void Toggle(string name)
    {
        if (!_tabs.TryGetValue(name, out var tab)) return;

        if (tab.IsOpen)
        {
            Close(name);
        }
        else
        {
            Open(name);
        }
    }

    /// <summary>
    /// タブを開く
    /// </summary>
    public void Open(string name)
    {
        if (!_tabs.TryGetValue(name, out var tab)) return;

        tab.Panel.RemoveFromClassList("hidden");
        tab.Icon?.AddToClassList("active");
        tab.IsOpen = true;
    }

    /// <summary>
    /// タブを閉じる
    /// </summary>
    public void Close(string name)
    {
        if (!_tabs.TryGetValue(name, out var tab)) return;

        tab.Panel.AddToClassList("hidden");
        tab.Icon?.RemoveFromClassList("active");
        tab.IsOpen = false;
    }

    /// <summary>
    /// 全タブを閉じる
    /// </summary>
    public void CloseAll()
    {
        foreach (var name in _tabs.Keys)
        {
            Close(name);
        }
    }

    /// <summary>
    /// 指定タブが開いているか
    /// </summary>
    public bool IsOpen(string name)
    {
        return _tabs.TryGetValue(name, out var tab) && tab.IsOpen;
    }

    /// <summary>
    /// クリーンアップ
    /// </summary>
    public void Dispose()
    {
        _callbacks.Dispose();
        _tabs.Clear();
    }
}
