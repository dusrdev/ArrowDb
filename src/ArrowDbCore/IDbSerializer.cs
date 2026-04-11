using System.Collections.Concurrent;

namespace ArrowDbCore;

/// <summary>
/// The interface that defines a serializer for ArrowDb
/// </summary>
public interface IDbSerializer : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Indicates whether the serializer was disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Deserializes the database from the underlying storage
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the database to the underlying storage
    /// </summary>
    /// <param name="data">The data to serialize</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default);
}
