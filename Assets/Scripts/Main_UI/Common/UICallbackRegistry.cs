using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// UI Toolkitのコールバックを自動管理するレジストリ
/// 登録したコールバックをDispose()で一括解除
/// </summary>
public class UICallbackRegistry : IDisposable
{
    private readonly List<Action> _unregisterActions = new List<Action>();

    /// <summary>
    /// ClickEventコールバックを登録
    /// </summary>
    public void RegisterClick(VisualElement element, Action handler)
    {
        if (element == null || handler == null) return;

        EventCallback<ClickEvent> callback = evt => handler();
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// ClickEventコールバックを登録（イベント引数付き）
    /// </summary>
    public void RegisterClick(VisualElement element, Action<ClickEvent> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<ClickEvent> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// MouseMoveEventコールバックを登録
    /// </summary>
    public void RegisterMouseMove(VisualElement element, Action<MouseMoveEvent> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<MouseMoveEvent> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// MouseDownEventコールバックを登録
    /// </summary>
    public void RegisterMouseDown(VisualElement element, Action<MouseDownEvent> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<MouseDownEvent> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// MouseUpEventコールバックを登録
    /// </summary>
    public void RegisterMouseUp(VisualElement element, Action<MouseUpEvent> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<MouseUpEvent> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// PointerDownEventコールバックを登録
    /// </summary>
    public void RegisterPointerDown(VisualElement element, Action<PointerDownEvent> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<PointerDownEvent> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// PointerUpEventコールバックを登録
    /// </summary>
    public void RegisterPointerUp(VisualElement element, Action<PointerUpEvent> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<PointerUpEvent> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// ChangeEventコールバックを登録
    /// </summary>
    public void RegisterChange<T>(VisualElement element, Action<ChangeEvent<T>> handler)
    {
        if (element == null || handler == null) return;

        EventCallback<ChangeEvent<T>> callback = evt => handler(evt);
        element.RegisterCallback(callback);
        _unregisterActions.Add(() => element.UnregisterCallback(callback));
    }

    /// <summary>
    /// 任意のイベントコールバックを登録
    /// </summary>
    public void Register<TEvent>(VisualElement element, EventCallback<TEvent> handler)
        where TEvent : EventBase<TEvent>, new()
    {
        if (element == null || handler == null) return;

        element.RegisterCallback(handler);
        _unregisterActions.Add(() => element.UnregisterCallback(handler));
    }

    /// <summary>
    /// 登録数を取得
    /// </summary>
    public int Count => _unregisterActions.Count;

    /// <summary>
    /// 全コールバックを解除
    /// </summary>
    public void Dispose()
    {
        foreach (var unregister in _unregisterActions)
        {
            try
            {
                unregister?.Invoke();
            }
            catch
            {
                // 要素が既に破棄されている場合は無視
            }
        }
        _unregisterActions.Clear();
    }
}
