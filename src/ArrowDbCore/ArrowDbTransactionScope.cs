
namespace ArrowDbCore;

/// <summary>
/// Provides a scope that can be used to defer serialization until the scope is disposed
/// </summary>
public sealed class ArrowDbTransactionScope : IAsyncDisposable, IDisposable {
	private readonly ArrowDb _database;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrowDbTransactionScope"/> class.
	/// </summary>
	/// <param name="database">The database instance</param>
	internal ArrowDbTransactionScope(ArrowDb database) {
		_database = database;
		Interlocked.Increment(ref _database.TransactionDepth);
	}

	/// <summary>
	/// Disposes the scope and calls <see cref="ArrowDb.SerializeAsync"/>
	/// </summary>
	public async ValueTask DisposeAsync() {
		if (_disposed) {
			return;
		}
		if (Interlocked.Decrement(ref _database.TransactionDepth) == 0) {
			await _database.SerializeAsync().ConfigureAwait(false);
		}
		_disposed = true;
	}

	/// <summary>
	/// Disposes the scope and calls <see cref="ArrowDb.SerializeAsync"/> in a blocking operation
	/// </summary>
	public void Dispose() {
		var task = DisposeAsync();
		if (task.IsCompleted) {
			return;
		}
		task.GetAwaiter().GetResult();
	}
}
