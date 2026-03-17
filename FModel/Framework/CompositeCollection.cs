using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FModel.Framework;

/// <summary>
/// Minimal replacement for WPF's CompositeCollection.
/// Merges multiple <see cref="IEnumerable"/> sources into a single flat enumerable
/// and relays <see cref="INotifyCollectionChanged"/> events from each source.
/// </summary>
public class CompositeCollection : IEnumerable, INotifyCollectionChanged, IDisposable
{
    private readonly IEnumerable[] _sources;
    private bool _disposed;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public CompositeCollection(params IEnumerable[] sources)
    {
        _sources = sources;
        foreach (var source in _sources)
        {
            if (source is INotifyCollectionChanged ncc)
                ncc.CollectionChanged += OnSourceCollectionChanged;
        }
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Relay as a reset since index mapping across multiple sources is complex
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public IEnumerator GetEnumerator()
    {
        foreach (var source in _sources)
        {
            foreach (var item in source)
                yield return item;
        }
    }

    public int Count
    {
        get
        {
            var count = 0;
            foreach (var source in _sources)
            {
                if (source is ICollection c)
                    count += c.Count;
                // Skip non-ICollection sources; current callers (DataGridCollectionView)
                // implement ICollection so this avoids O(n) enumeration.
            }
            return count;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        foreach (var source in _sources)
        {
            if (source is INotifyCollectionChanged ncc)
                ncc.CollectionChanged -= OnSourceCollectionChanged;
        }
    }
}
