namespace ArrowDbCore.Tests.Unit;

public class TrackingVariables {
    [Fact]
    public async Task Changes_Match_Additions() {
        const int count = 10;
        var db = await ArrowDb.CreateInMemory();
        for (var i = 0; i < count; i++) {
            db.Upsert(i.ToString(), i, JContext.Default.Int32);
        }
        Assert.Equal(count, db.PendingChanges);
    }

    [Fact]
    public async Task Changes_Match_Removals() {
        const int count = 10;
        var db = await ArrowDb.CreateInMemory();
        for (var i = 0; i < count; i++) {
            db.Upsert(i.ToString(), i, JContext.Default.Int32);
        }
        await db.SerializeAsync(); // resets pending changes
        Assert.Equal(0, db.PendingChanges); // verify reset
        for (var i = 0; i < count; i++) {
            db.TryRemove(i.ToString());
        }
        Assert.Equal(count, db.PendingChanges);
    }

    [Fact]
    public async Task Changes_Match_Updates() {
        const int count = 10;
        var db = await ArrowDb.CreateInMemory();
        for (var i = 0; i < count; i++) {
            db.Upsert(i.ToString(), i, JContext.Default.Int32);
        }
        await db.SerializeAsync(); // resets pending changes
        Assert.Equal(0, db.PendingChanges); // verify reset
        for (var i = 0; i < count; i++) {
            db.Upsert(i.ToString(), i + 1, JContext.Default.Int32);
        }
        Assert.Equal(count, db.PendingChanges);
    }
}