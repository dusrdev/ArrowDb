
namespace ArrowDbCore;

/// <summary>
/// Provides a scope that can be used to defer serialization until the scope is disposed
/// </summary>
internal sealed class ArrowDbTransactionScope : IAsyncDisposable {
	private readonly ArrowDb _database;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArrowDbTransactionScope"/> class.
	/// </summary>
	/// <param name="database">The database instance</param>
	public ArrowDbTransactionScope(ArrowDb database) {
		_database = database;
	}

	/// <summary>
	/// Disposes the scope and calls <see cref="ArrowDb.SerializeAsync"/>
	/// </summary>
    public async ValueTask DisposeAsync() {
        await _database.SerializeAsync();
    }
}
