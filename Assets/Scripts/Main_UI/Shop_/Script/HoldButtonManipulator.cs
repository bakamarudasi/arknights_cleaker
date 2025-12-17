using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// ボタンを長押ししたときにアクションを連続実行するマニピュレータ
/// </summary>
public class HoldButtonManipulator : Manipulator
{
    private readonly Action onAction;
    private readonly int delayMs;    // 長押し判定までの時間
    private readonly int intervalMs; // 連打の間隔

    private IVisualElementScheduledItem scheduledItem;
    private bool isHeld = false;

    public HoldButtonManipulator(Action onAction, int delayMs = 500, int intervalMs = 100)
    {
        this.onAction = onAction;
        this.delayMs = delayMs;
        this.intervalMs = intervalMs;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        // マウス/タッチが押されたとき
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        // マウス/タッチが離されたとき
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        // マウスが要素の外に出たとき（キャンセル扱い）
        target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (target.enabledInHierarchy)
        {
            isHeld = true;
            // 最初に1回実行するかはお好みで（今回は長押し成立後に連打開始）
            // UI Toolkitのスケジューラー機能を使って遅延実行を設定
            scheduledItem = target.schedule.Execute(OnTimer).StartingIn(delayMs);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        StopHold();
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        StopHold();
    }

    private void StopHold()
    {
        isHeld = false;
        if (scheduledItem != null)
        {
            scheduledItem.Pause(); // タイマー停止
            scheduledItem = null;
        }
    }

    private void OnTimer()
    {
        if (!isHeld) return;

        // アクション実行
        onAction?.Invoke();

        // 次の実行をスケジュール（連打間隔）
        scheduledItem = target.schedule.Execute(OnTimer).StartingIn(intervalMs);
    }
}