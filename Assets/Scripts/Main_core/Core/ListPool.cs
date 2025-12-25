using System.Collections.Generic;

/// <summary>
/// 汎用Listプールクラス
/// GC圧力を軽減するためにListインスタンスを再利用する
/// </summary>
/// <typeparam name="T">リストの要素型</typeparam>
public static class ListPool<T>
{
    private static readonly Stack<List<T>> _pool = new Stack<List<T>>();
    private static readonly object _lock = new object();

    /// <summary>デフォルトの初期容量</summary>
    private const int DefaultCapacity = 16;

    /// <summary>プールの最大サイズ（メモリリーク防止）</summary>
    private const int MaxPoolSize = 32;

    /// <summary>
    /// プールからListを取得（なければ新規作成）
    /// </summary>
    public static List<T> Get()
    {
        lock (_lock)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
        }
        return new List<T>(DefaultCapacity);
    }

    /// <summary>
    /// 指定容量を確保したListを取得
    /// </summary>
    public static List<T> Get(int capacity)
    {
        var list = Get();
        if (list.Capacity < capacity)
        {
            list.Capacity = capacity;
        }
        return list;
    }

    /// <summary>
    /// Listをプールに返却
    /// </summary>
    public static void Release(List<T> list)
    {
        if (list == null) return;

        list.Clear();

        lock (_lock)
        {
            // プールサイズ制限
            if (_pool.Count < MaxPoolSize)
            {
                _pool.Push(list);
            }
        }
    }

    /// <summary>
    /// プールをクリア（メモリ解放時に使用）
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _pool.Clear();
        }
    }
}

/// <summary>
/// using文で使用できるListプールラッパー
/// </summary>
/// <typeparam name="T">リストの要素型</typeparam>
public struct PooledList<T> : System.IDisposable
{
    public List<T> List { get; private set; }
    private bool _disposed;

    public PooledList(int capacity = 0)
    {
        List = capacity > 0 ? ListPool<T>.Get(capacity) : ListPool<T>.Get();
        _disposed = false;
    }

    public void Dispose()
    {
        if (!_disposed && List != null)
        {
            ListPool<T>.Release(List);
            List = null;
            _disposed = true;
        }
    }

    /// <summary>暗黙的変換でList<T>として使用可能</summary>
    public static implicit operator List<T>(PooledList<T> pooled) => pooled.List;
}
