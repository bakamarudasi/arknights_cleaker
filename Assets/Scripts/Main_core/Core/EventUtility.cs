using System;
using UnityEngine;

/// <summary>
/// イベント発火の共通ユーティリティ
/// 例外をキャッチしてログ出力し、イベントハンドラのエラーがシステム全体に影響しないようにする
/// </summary>
public static class EventUtility
{
    /// <summary>
    /// 引数なしのActionを安全に発火
    /// </summary>
    public static void SafeInvoke(Action action, string logTag, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"{logTag} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 引数1つのActionを安全に発火
    /// </summary>
    public static void SafeInvoke<T>(Action<T> action, T arg, string logTag, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke(arg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{logTag} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 引数2つのActionを安全に発火
    /// </summary>
    public static void SafeInvoke<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, string logTag, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke(arg1, arg2);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{logTag} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 引数3つのActionを安全に発火
    /// </summary>
    public static void SafeInvoke<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, string logTag, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke(arg1, arg2, arg3);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{logTag} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 引数4つのActionを安全に発火
    /// </summary>
    public static void SafeInvoke<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, string logTag, string eventName)
    {
        if (action == null) return;
        try
        {
            action.Invoke(arg1, arg2, arg3, arg4);
        }
        catch (Exception ex)
        {
            Debug.LogError($"{logTag} Event '{eventName}' handler threw exception: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
