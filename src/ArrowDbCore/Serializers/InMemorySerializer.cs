using System.Collections.Concurrent;


namespace ArrowDbCore.Serializers;

/// <summary>
/// An in-memory serializer (does nothing)
/// </summary>
public sealed class InMemorySerializer : IDbSerializer {
    private bool _disposed;

    /// <inheritdoc />
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Returns an empty dictionary
    /// </summary>
    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
    }

    /// <summary>
    /// Does nothing
    /// </summary>
    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() {
        _disposed = true;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
