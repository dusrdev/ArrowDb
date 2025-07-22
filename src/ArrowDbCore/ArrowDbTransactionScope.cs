
namespace ArrowDbCore;

/// <summary>
/// Provides a scope that can be used to defer serialization until the scope is disposed
/// </summary>
internal sealed class ArrowDbTransactionScope : IAsyncDisposable {
	private readonly ArrowDb _database;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrowDbTransactionScope"/> class.
	/// </summary>
	/// <param name="database">The database instance</param>
	public ArrowDbTransactionScope(ArrowDb database) {
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
}
