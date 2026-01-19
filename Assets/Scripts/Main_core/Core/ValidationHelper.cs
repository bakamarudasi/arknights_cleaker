using UnityEngine;

/// <summary>
/// 共通のバリデーション処理を一元化
/// Nullチェック、条件チェックなどを統一的に行う
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// nullチェックしてログ出力
    /// </summary>
    /// <returns>nullの場合はtrue</returns>
    public static bool IsNull<T>(T obj, string logTag, string paramName) where T : class
    {
        if (obj == null)
        {
            Debug.LogWarning($"{logTag} {paramName} is null");
            return true;
        }
        return false;
    }

    /// <summary>
    /// nullチェックしてエラーログ出力
    /// </summary>
    /// <returns>nullの場合はtrue</returns>
    public static bool IsNullError<T>(T obj, string logTag, string paramName) where T : class
    {
        if (obj == null)
        {
            Debug.LogError($"{logTag} {paramName} is null");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 文字列が空かチェック
    /// </summary>
    /// <returns>空の場合はtrue</returns>
    public static bool IsNullOrEmpty(string value, string logTag, string paramName)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogWarning($"{logTag} {paramName} is null or empty");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 複数のマネージャーの存在を一括チェック
    /// </summary>
    public static bool ValidateManagers(string logTag, params (object manager, string name)[] managers)
    {
        bool allValid = true;
        foreach (var (manager, name) in managers)
        {
            if (manager == null)
            {
                Debug.LogError($"{logTag} {name} is null");
                allValid = false;
            }
        }
        return allValid;
    }

    /// <summary>
    /// 値が正の数かチェック
    /// </summary>
    public static bool IsPositive(int value, string logTag, string paramName)
    {
        if (value <= 0)
        {
            Debug.LogWarning($"{logTag} {paramName} must be positive, got {value}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 値が正の数かチェック（double版）
    /// </summary>
    public static bool IsPositive(double value, string logTag, string paramName)
    {
        if (value <= 0)
        {
            Debug.LogWarning($"{logTag} {paramName} must be positive, got {value}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 値が非負かチェック
    /// </summary>
    public static bool IsNonNegative(int value, string logTag, string paramName)
    {
        if (value < 0)
        {
            Debug.LogWarning($"{logTag} {paramName} must be non-negative, got {value}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 値が範囲内かチェック
    /// </summary>
    public static bool IsInRange(int value, int min, int max, string logTag, string paramName)
    {
        if (value < min || value > max)
        {
            Debug.LogWarning($"{logTag} {paramName} must be in range [{min}, {max}], got {value}");
            return false;
        }
        return true;
    }
}
