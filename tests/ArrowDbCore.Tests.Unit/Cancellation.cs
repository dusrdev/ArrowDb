using System.Collections.Concurrent;

using ArrowDbCore.Tests.Common;

namespace ArrowDbCore.Tests.Unit;

public class Cancellation
{
    [Fact]
    public async Task CreateInMemory_WhenCanceled_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => ArrowDb.CreateInMemory(cancellationTokenSource.Token).AsTask());
    }

    [Fact]
    public async Task CreateCustom_WhenCanceled_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => ArrowDb.CreateCustom(new CancellationSerializer(), cancellationTokenSource.Token).AsTask());
    }

    [Fact]
    public async Task CreateFromFile_WhenCanceled_ThrowsOperationCanceledException()
    {
        string path = Path.GetTempFileName();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() => ArrowDb.CreateFromFile(path, cancellationTokenSource.Token).AsTask());
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task CreateFromFileWithAes_WhenCanceled_ThrowsOperationCanceledException()
    {
        string path = Path.GetTempFileName();
        using var aes = System.Security.Cryptography.Aes.Create();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        try
        {
            await Assert.ThrowsAsync<OperationCanceledException>(() => ArrowDb.CreateFromFileWithAes(path, aes, cancellationTokenSource.Token).AsTask());
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task SerializeAsync_WhenCanceledWhileWaitingForSemaphore_ThrowsAndDoesNotStartSecondSerialize()
    {
        var serializer = new CancellationSerializer();
        var db = await ArrowDb.CreateCustom(serializer);
        Assert.True(db.Upsert("seed", 1, JContext.Default.Int32));

        Task firstSerializeTask = db.SerializeAsync();
        await serializer.SerializeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        using var cancellationTokenSource = new CancellationTokenSource();
        Task secondSerializeTask = db.SerializeAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => secondSerializeTask);
        Assert.Equal(1, Volatile.Read(ref serializer.SerializeCalls));
        Assert.Equal(1, db.PendingChanges);

        serializer.AllowSerializeToFinish.TrySetResult();
        await firstSerializeTask;
    }

    [Fact]
    public async Task RollbackAsync_WhenCanceledWhileWaitingForSemaphore_ThrowsAndLeavesStateUnchanged()
    {
        var serializer = new CancellationSerializer();
        var db = await ArrowDb.CreateCustom(serializer);
        int deserializeCallsBeforeRollback = Volatile.Read(ref serializer.DeserializeCalls);
        Assert.True(db.Upsert("seed", 1, JContext.Default.Int32));

        Task serializeTask = db.SerializeAsync();
        await serializer.SerializeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        using var cancellationTokenSource = new CancellationTokenSource();
        Task rollbackTask = db.RollbackAsync(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => rollbackTask);
        Assert.True(db.ContainsKey("seed"));
        Assert.Equal(1, db.PendingChanges);
        Assert.Equal(deserializeCallsBeforeRollback, Volatile.Read(ref serializer.DeserializeCalls));

        serializer.AllowSerializeToFinish.TrySetResult();
        await serializeTask;
    }

    [Fact]
    public async Task TransactionScope_WhenOuterTokenCanceled_ThrowsAndLeavesPendingChanges()
    {
        var db = await ArrowDb.CreateInMemory();
        using var cancellationTokenSource = new CancellationTokenSource();

        var scope = db.BeginTransaction(cancellationTokenSource.Token);
        db.Upsert("1", 1, JContext.Default.Int32);
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.DisposeAsync().AsTask());
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.PendingChanges);

        await db.SerializeAsync();
        Assert.Equal(0, db.PendingChanges);
    }
}

internal sealed class CancellationSerializer : IDbSerializer
{
    private bool _disposed;

    public readonly TaskCompletionSource SerializeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public readonly TaskCompletionSource AllowSerializeToFinish = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int DeserializeCalls;
    public int SerializeCalls;

    public bool IsDisposed => _disposed;

    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Interlocked.Increment(ref DeserializeCalls);
        return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
    }

    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Interlocked.Increment(ref SerializeCalls);
        SerializeStarted.TrySetResult();
        return new ValueTask(AllowSerializeToFinish.Task);
    }

    public void Dispose() => _disposed = true;

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
