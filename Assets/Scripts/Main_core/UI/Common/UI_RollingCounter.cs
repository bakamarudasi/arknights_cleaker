using UnityEngine;
using TMPro;

/// <summary>
/// 数字を指定された値までパラパラとカウントアップさせる演出スクリプト。
/// GameControllerではなく、表示するテキスト(Text_LMDなど)にアタッチして使います。
/// </summary>
public class UI_RollingCounter : MonoBehaviour
{
    [Header("設定")]
    public TextMeshProUGUI targetText; // 動かしたいテキスト
    public float animationDuration = 0.5f; // 何秒かけて数値を変化させるか

    private double _currentDisplayValue = 0;
    private double _targetValue = 0;
    private double _startValue = 0;
    private float _timer = 0;
    private bool _isAnimating = false;

    void Awake()
    {
        // アタッチし忘れても、同じオブジェクトにあれば自動で取得
        if (targetText == null) targetText = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// 目標値を設定してアニメーション開始
    /// </summary>
    public void SetValue(double newValue, bool instant = false)
    {
        if (instant)
        {
            _currentDisplayValue = newValue;
            _targetValue = newValue;
            _isAnimating = false;
            UpdateText();
            return;
        }

        // すでにアニメーション中なら、今の表示位置からスタート
        _startValue = _currentDisplayValue;
        _targetValue = newValue;
        _timer = 0;
        _isAnimating = true;
    }

    void Update()
    {
        if (!_isAnimating) return;

        _timer += Time.deltaTime;
        float progress = _timer / animationDuration;

        if (progress >= 1.0f)
        {
            // 完了
            _currentDisplayValue = _targetValue;
            _isAnimating = false;
        }
        else
        {
            // 補間 (Lerp)
            // 指数関数的な動き（最初早くて最後ゆっくり）にすると気持ちいい
            float ease = 1 - Mathf.Pow(1 - progress, 3);
            _currentDisplayValue = _startValue + (_targetValue - _startValue) * ease;
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (targetText != null)
        {
            // N0 は3桁区切り
            targetText.text = _currentDisplayValue.ToString("N0");
        }
    }
}