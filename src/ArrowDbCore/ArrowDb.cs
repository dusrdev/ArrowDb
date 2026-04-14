using System.Collections.Concurrent;

namespace ArrowDbCore;

/// <summary>
/// ArrowDb
/// </summary>
/// <remarks>Initialize via the factory methods</remarks>
public sealed partial class ArrowDb
{
    /// <summary>
    /// Returns the number of active <see cref="ArrowDb"/> instances
    /// </summary>
    public static long RunningInstances => Interlocked.Read(ref s_runningInstances);

    /// <summary>
    /// Tracks the number of running instances
    /// </summary>
    private static long s_runningInstances;

    /// <summary>
    /// The backing dictionary
    /// </summary>
    internal volatile ConcurrentDictionary<string, byte[]> Source;

    /// <summary>
    /// The alternate lookup
    /// </summary>
    internal ConcurrentDictionary<string, byte[]>.AlternateLookup<ReadOnlySpan<char>> Lookup;

    /// <summary>
    /// The semaphore for maintaining serialization consistency
    /// </summary>
    internal readonly SemaphoreSlim Semaphore;

    /// <summary>
    /// The serializer
    /// </summary>
    internal readonly IDbSerializer Serializer;

    /// <summary>
    /// Indicates whether this instance owns the serializer lifetime.
    /// </summary>
    internal readonly bool DisposeSerializer;

    /// <summary>
    /// An event that is raised when any operation was performed that changes the database state, i.e, adding, updating, or removing a key, or clearing the database
    /// </summary>
    public event EventHandler<ArrowDbChangeEventArgs>? OnChange;

    /// <summary>
    /// Raises the <see cref="OnChange"/> event
    /// </summary>
    private void OnChangeInternal(ArrowDbChangeEventArgs args)
    {
        Interlocked.Increment(ref _pendingChanges);
        OnChange?.Invoke(this, args);
    }

    /// <summary>
    /// Returns the number of pending changes (number of changes that have not been serialized)
    /// </summary>
    public long PendingChanges => Interlocked.Read(ref _pendingChanges);

    /// <summary>
    /// Thread-safe pending changes tracker
    /// </summary>
    private long _pendingChanges;

    /// <summary>
    /// Thread-safe transaction depth tracker
    /// </summary>
    internal long TransactionDepth;

    /// <summary>
    /// A state epoch used to detect concurrent <see cref="RollbackAsync"/> operations in hot write paths.
    /// </summary>
    internal long StateEpoch;

    /// <summary>
    /// Private Ctor
    /// </summary>
    /// <param name="source">A pre-existing or empty dictionary</param>
    /// <param name="serializer">A serializer implementation</param>
    /// <param name="disposeSerializer">Whether this instance owns the serializer lifetime.</param>
    private ArrowDb(ConcurrentDictionary<string, byte[]> source, IDbSerializer serializer, bool disposeSerializer)
    {
        Source = source;
        Lookup = Source.GetAlternateLookup<ReadOnlySpan<char>>();
        Serializer = serializer;
        DisposeSerializer = disposeSerializer;
        Interlocked.Increment(ref s_runningInstances);
        Semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Finalizer (called when the instance is garbage collected)
    /// </summary>
    ~ArrowDb()
    {
        if (DisposeSerializer)
            Serializer.Dispose();

        Semaphore.Dispose();

        Interlocked.Decrement(ref s_runningInstances);
    }

    /// <summary>
    /// Returns a transaction scope that implicitly calls <see cref="SerializeAsync"/> when disposed
    /// </summary>
    /// <remarks>
    /// The <see cref="ArrowDbTransactionScope"/> implements both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>, allowing it to be used in both synchronous and asynchronous contexts.
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token for the outermost implicit serialize operation.</param>
    /// <returns>A new <see cref="ArrowDbTransactionScope"/> instance.</returns>
    public ArrowDbTransactionScope BeginTransaction(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Serializer.IsDisposed, Serializer);
        return new(this, cancellationToken);
    }
}
