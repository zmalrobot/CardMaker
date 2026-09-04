namespace CardMaker.Infrastructure.Rendering;

/// <summary>
/// Cache LRU thread-safe a capacita' fissa. Alla scadenza della capacita' l'elemento meno usato
/// di recente viene rimosso e, se <typeparamref name="TValue"/> e' <see cref="IDisposable"/>
/// e <c>disposeOnEviction</c> e' vero, smaltito (F2: "cache LRU degli asset decodificati").
/// </summary>
public sealed class LruCache<TKey, TValue>(int capacity, bool disposeOnEviction = true) where TKey : notnull
{
    private readonly int _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
    private readonly bool _disposeOnEviction = disposeOnEviction;
    private readonly Lock _gate = new();
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map = [];
    private readonly LinkedList<(TKey Key, TValue Value)> _order = new();

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _map.Count;
            }
        }
    }

    public TValue? TryGet(TKey key)
    {
        lock (_gate)
        {
            if (!_map.TryGetValue(key, out var node))
            {
                return default;
            }

            _order.Remove(node);
            _order.AddFirst(node);
            return node.Value.Value;
        }
    }

    /// <summary>Inserisce o aggiorna una voce. Se scade la capacita', rimuove la meno recente.</summary>
    public void Set(TKey key, TValue value)
    {
        lock (_gate)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                _order.Remove(existing);
                if (_disposeOnEviction)
                {
                    DisposeIfPossible(existing.Value.Value);
                }
            }

            var node = new LinkedListNode<(TKey, TValue)>((key, value));
            _order.AddFirst(node);
            _map[key] = node;

            while (_map.Count > _capacity)
            {
                var last = _order.Last!;
                _order.RemoveLast();
                _map.Remove(last.Value.Key);
                if (_disposeOnEviction)
                {
                    DisposeIfPossible(last.Value.Value);
                }
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            foreach (var node in _map.Values)
            {
                DisposeIfPossible(node.Value.Value);
            }

            _map.Clear();
            _order.Clear();
        }
    }

    private static void DisposeIfPossible(TValue? value)
    {
        if (value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
