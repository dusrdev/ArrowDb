
namespace ArrowDbCore;

/// <summary>
/// Provides a scope that can be used to defer serialization until the scope is disposed
/// </summary>
public sealed class ArrowDbTransactionScope : IAsyncDisposable, IDisposable {
    private readonly ArrowDb _database;
    private readonly CancellationToken _cancellationToken;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrowDbTransactionScope"/> class.
    /// </summary>
    /// <param name="database">The database instance</param>
    /// <param name="cancellationToken">A cancellation token for the outermost implicit serialize operation.</param>
    internal ArrowDbTransactionScope(ArrowDb database, CancellationToken cancellationToken) {
        _database = database;
        _cancellationToken = cancellationToken;
        Interlocked.Increment(ref _database.TransactionDepth);
    }

    /// <summary>
    /// Disposes the scope and calls <see cref="ArrowDb.SerializeAsync"/>
    /// </summary>
    public async ValueTask DisposeAsync() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        if (Interlocked.Decrement(ref _database.TransactionDepth) == 0) {
            await _database.SerializeAsync(_cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes the scope and calls <see cref="ArrowDb.SerializeAsync"/> in a blocking operation
    /// </summary>
    public void Dispose() {
#pragma warning disable CA2012
        var task = DisposeAsync();
#pragma warning restore CA2012
        if (task.IsCompletedSuccessfully) {
            return;
        }
        task.GetAwaiter().GetResult();
    }
}
