namespace ArrowDbCore.Tests.Unit;

public class GetOrAddAsync {
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
// this is required here for testing purposes
    [Fact]
    public async Task GetOrAddAsync_ReturnsSynchronously_WhenExists() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32); // add before
        var task = db.GetOrAddAsync("1", JContext.Default.Int32, async _ => {
            await Task.Delay(1000);
            return 1;
        });
        Assert.True(task.IsCompletedSuccessfully);

        Assert.Equal(1, task.GetAwaiter().GetResult());

    }
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

    [Fact]
    public async Task GetOrAddAsync_ReturnsAsynchronously_WhenNotExists() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        // doesn't exist
        var task = db.GetOrAddAsync("1", JContext.Default.Int32, async _ => {
            await Task.Delay(1000);
            return 1;
        });
        Assert.False(task.IsCompletedSuccessfully);
        Assert.Equal(1, await task);
    }
}