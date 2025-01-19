using System.Collections.Concurrent;

namespace ArrowDbCore;

/// <summary>
/// ArrowDb
/// </summary>
/// <remarks>Initialize via the factory methods</remarks>
public sealed partial class ArrowDb {
	/// <summary>
	/// Returns the number of active <see cref="ArrowDb"/> instances
	/// </summary>
	public static int RunningInstances => s_runningInstances;

	/// <summary>
	/// Tracks the number of running instances
	/// </summary>
	private static volatile int s_runningInstances;

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
	/// An event that is raised when any operation was performed that changes the database state, i.e, adding, updating, or removing a key, or clearing the database
	/// </summary>
	public event EventHandler<ArrowDbChangeEventArgs>? OnChange;

    /// <summary>
    /// Raises the <see cref="OnChange"/> event
    /// </summary>
    private void OnChangeInternal(ArrowDbChangeEventArgs args) {
		Interlocked.Increment(ref _pendingChanges);
        OnChange?.Invoke(this, args);
    }

    /// <summary>
    /// Returns the number of pending changes (number of changes that have not been serialized)
    /// </summary>
    public int PendingChanges => _pendingChanges;

	/// <summary>
	/// Thread-safe pending changes tracker
	/// </summary>
	private volatile int _pendingChanges;

	/// <summary>
	/// Private Ctor
	/// </summary>
	/// <param name="source">A pre-existing or empty dictionary</param>
	/// <param name="serializer">A serializer implementation</param>
	private ArrowDb(ConcurrentDictionary<string, byte[]> source, IDbSerializer serializer) {
		Source = source;
		Lookup = Source.GetAlternateLookup<ReadOnlySpan<char>>();
		Serializer = serializer;
		Interlocked.Increment(ref s_runningInstances);
		Semaphore = new SemaphoreSlim(1, 1);
	}

	/// <summary>
	/// Finalizer (called when the instance is garbage collected)
	/// </summary>
	~ArrowDb() {
		Interlocked.Decrement(ref s_runningInstances);
		Semaphore?.Dispose();
	}

	/// <summary>
	/// Returns a transaction scope that implicitly calls <see cref="SerializeAsync"/> when disposed
	/// </summary>
	/// <returns>IAsyncDisposable</returns>
	public IAsyncDisposable BeginTransaction() => new ArrowDbTransactionScope(this);
}
