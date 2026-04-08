using ArrowDbCore.Tests.Common;

namespace ArrowDbCore.Tests.Unit;

public class GetOrAddAsync {
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
    // this is required here for testing purposes
    [Fact]
    public async Task GetOrAddAsync_ReturnsSynchronously_WhenExists() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32); // add before
        var task = db.GetOrAddAsync("1", JContext.Default.Int32, async (_, _) => {
            await Task.Delay(1000);
            return 1;
        });
        Assert.True(task.IsCompletedSuccessfully);

        Assert.Equal(1, task.GetAwaiter().GetResult());

    }

    [Fact]
    public async Task GetOrAddAsync_WithTArg_ReturnsSynchronously_WhenExists() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32); // add before
        // using a static delegate ensures that closure cannot be allocated
        var task = db.GetOrAddAsync("1", JContext.Default.Int32, static async (_, value, _) => {
            await Task.Delay(1000);
            return value;
        }, 1);
        Assert.True(task.IsCompletedSuccessfully);

        Assert.Equal(1, task.GetAwaiter().GetResult());

    }
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

    [Fact]
    public async Task GetOrAddAsync_ReturnsAsynchronously_WhenNotExists() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        // doesn't exist
        var task = db.GetOrAddAsync("1", JContext.Default.Int32, async (_, _) => {
            await Task.Delay(1000);
            return 1;
        });
        Assert.False(task.IsCompletedSuccessfully);
        Assert.Equal(1, await task);
    }

    [Fact]
    public async Task GetOrAddAsync_WithTArg_ReturnsAsynchronously_WhenNotExists() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        // doesn't exist
        // using a static delegate ensures that closure cannot be allocated
        var task = db.GetOrAddAsync("1", JContext.Default.Int32, static async (_, value, _) => {
            await Task.Delay(1000);
            return value;
        }, 1);
        Assert.False(task.IsCompletedSuccessfully);
        Assert.Equal(1, await task);
    }

    [Fact]
    public async Task GetOrAddAsync_FailingFactory_DoesNotAddItem() {
        // Arrange
        var db = await ArrowDb.CreateInMemory();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.GetOrAddAsync("key", JContext.Default.Int32, (_, _) =>
                ValueTask.FromException<int>(new InvalidOperationException("Factory failed"))
            ).AsTask()
        );

        Assert.Equal(0, db.Count);
        Assert.False(db.ContainsKey("key"));
    }

    [Fact]
    public async Task GetOrAddAsync_ReturnsSynchronously_WhenExists_EvenIfCanceled() {
        var db = await ArrowDb.CreateInMemory();
        db.Upsert("1", 1, JContext.Default.Int32);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        bool factoryCalled = false;

        var task = db.GetOrAddAsync("1", JContext.Default.Int32, (_, _) => {
            factoryCalled = true;
            return ValueTask.FromResult(2);
        }, cancellationTokenSource.Token);

        Assert.True(task.IsCompletedSuccessfully);
        Assert.False(factoryCalled);
        Assert.Equal(1, await task);
    }

    [Fact]
    public async Task GetOrAddAsync_WhenCanceledAfterFactory_ReturnsCanceledAndDoesNotAddItem() {
        var db = await ArrowDb.CreateInMemory();
        using var cancellationTokenSource = new CancellationTokenSource();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            db.GetOrAddAsync("key", JContext.Default.Int32, (_, cancellationToken) => {
                cancellationTokenSource.Cancel();
                return ValueTask.FromResult(1);
            }, cancellationTokenSource.Token).AsTask()
        );

        Assert.Equal(0, db.Count);
        Assert.False(db.ContainsKey("key"));
    }
}
