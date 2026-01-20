using UnityEngine;

/// <summary>
/// MonoBehaviourベースのシングルトン基底クラス
/// 全Managerクラスで共通のシングルトン処理を一元化
/// </summary>
/// <typeparam name="T">派生クラスの型</typeparam>
public abstract class BaseSingleton<T> : MonoBehaviour where T : BaseSingleton<T>
{
    public static T Instance { get; private set; }

    /// <summary>
    /// DontDestroyOnLoadを適用するか（デフォルト: true）
    /// </summary>
    protected virtual bool Persistent => true;

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)this;

        if (Persistent)
        {
            DontDestroyOnLoad(gameObject);
        }

        OnAwake();
    }

    /// <summary>
    /// Awake処理の拡張ポイント（派生クラスでオーバーライド）
    /// </summary>
    protected virtual void OnAwake() { }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
