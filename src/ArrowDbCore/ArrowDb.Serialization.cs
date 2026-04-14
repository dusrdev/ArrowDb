using System.Runtime.CompilerServices;

namespace ArrowDbCore;

public partial class ArrowDb
{
    /// <summary>
    /// Serializes the database
    /// </summary>
    /// <remarks>
    /// If there are no pending updates, this method does nothing, otherwise it serializes the database and resets the pending updates counter
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SerializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Serializer.IsDisposed, Serializer);

        if (Interlocked.Read(ref _pendingChanges) == 0)
        {
            return;
        }

        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            var observedPendingChanges = Interlocked.Read(ref _pendingChanges);
            await Serializer.SerializeAsync(Source, cancellationToken);
            Interlocked.CompareExchange(ref _pendingChanges, 0, observedPendingChanges); // reset pending changes only if unchanged
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Waits for the semaphore if the database is currently serializing
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WaitIfSerializing()
    {
        if (Semaphore.CurrentCount == 0)
        {
            Semaphore.Wait();
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Rolls the entire database to the last persisted state
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Serializer.IsDisposed, Serializer);

        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            Interlocked.Increment(ref StateEpoch);
            var prevState = await Serializer.DeserializeAsync(cancellationToken);
            Source.Clear();
            Interlocked.Exchange(ref Source, prevState);
            Lookup = Source.GetAlternateLookup<ReadOnlySpan<char>>();
            Interlocked.Exchange(ref _pendingChanges, 0);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
