using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ZipperManipulator : PointerManipulator
{
    public event Action<float> OnProgressChanged;
    public event Action OnUnzipCompleted;

    private bool _isDragging;
    private Vector2 _startPointerPos;
    private float _startElementX; // Vector3ではなくX座標のfloatだけ保持
    private VisualElement _rail;
    private float _maxMovement;

    public ZipperManipulator(VisualElement target, VisualElement rail)
    {
        this.target = target;
        _rail = rail;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCaptureOutEvent>(evt => OnPointerUp(null));
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_rail == null) return;
        _isDragging = true;
        target.CapturePointer(evt.pointerId);
        _startPointerPos = evt.position;

        var currentTranslate = target.style.translate.value;
        _startElementX = currentTranslate.x.value;

        // 修正: Paddingなどを考慮して少し余裕を持たせる
        // resolvedStyle.width が NaN の場合のガードを入れる
        float railWidth = float.IsNaN(_rail.resolvedStyle.width) ? 500f : _rail.resolvedStyle.width;
        float targetWidth = float.IsNaN(target.resolvedStyle.width) ? 60f : target.resolvedStyle.width;

        _maxMovement = railWidth - targetWidth;
        if (_maxMovement <= 0) _maxMovement = 1f;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging) return;
        float deltaX = evt.position.x - _startPointerPos.x;
        float targetX = _startElementX + deltaX;
        float clampedX = Mathf.Clamp(targetX, 0, _maxMovement);

        target.style.translate = new Translate(clampedX, 0, 0);

        float progress = Mathf.Clamp01(clampedX / _maxMovement);
        OnProgressChanged?.Invoke(progress);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDragging) return;
        _isDragging = false;
        target.ReleasePointer(evt.pointerId);

        float currentX = target.style.translate.value.x.value;
        float progress = currentX / _maxMovement;

        if (progress >= 0.95f)
        {
            OnUnzipCompleted?.Invoke();
        }
        else
        {
            // 修正点2: experimental.animation が無い環境のため、単純に戻す
            // 本来はDOTweenやUnityのTransitionを使いたいが、
            // 依存を減らすため即時リセットしつつ進行度0を通知する
            target.style.translate = new Translate(0, 0, 0);
            OnProgressChanged?.Invoke(0f);
        }
    }

    public void Reset()
    {
        target.style.translate = new Translate(0, 0, 0);
        OnProgressChanged?.Invoke(0f);
    }

    /// <summary>
    /// スキップ用：即座に開いた状態にする
    /// </summary>
    public void ForceComplete()
    {
        _isDragging = false;

        // 最大位置にセット
        if (_maxMovement <= 0)
        {
            float railWidth = _rail != null && !float.IsNaN(_rail.resolvedStyle.width)
                ? _rail.resolvedStyle.width : 500f;
            float targetWidth = target != null && !float.IsNaN(target.resolvedStyle.width)
                ? target.resolvedStyle.width : 60f;
            _maxMovement = railWidth - targetWidth;
        }

        target.style.translate = new Translate(_maxMovement, 0, 0);
        OnProgressChanged?.Invoke(1f);
    }
}