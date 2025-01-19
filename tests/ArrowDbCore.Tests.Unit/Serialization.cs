namespace ArrowDbCore.Tests.Unit;

public class Serialization {
    [Fact]
    public async Task Serialize_Resets_Changes() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.Count);
        Assert.Equal(1, db.PendingChanges);
        await db.SerializeAsync();
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.Count);
        Assert.Equal(0, db.PendingChanges);
    }

    [Fact]
    public async Task Rollback_Resets_Changes() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.Count);
        Assert.Equal(1, db.PendingChanges);
        await db.RollbackAsync();
        Assert.False(db.ContainsKey("1"));
        Assert.Equal(0, db.Count);
        Assert.Equal(0, db.PendingChanges);
    }

    [Fact]
    public async Task Serialize_Using_Event_Resets_Changes() {
        var db = await ArrowDb.CreateInMemory();
        db.OnChange += async (sender, _) => {
            await ((ArrowDb)sender!).SerializeAsync();
        };
        db.Upsert("1", 1, JContext.Default.Int32);
        // the upsert was already applied
        Assert.True(db.ContainsKey("1")); // key should exist
        Assert.Equal(0, db.PendingChanges); // no pending changes
    }

    [Fact]
    public async Task DeferredSerializationScope_Serialize_After_Dispose() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        await using (_ = db.BeginTransaction()) {
            db.Upsert("1", 1, JContext.Default.Int32);
            Assert.True(db.ContainsKey("1"));
            Assert.Equal(1, db.Count);
            Assert.Equal(1, db.PendingChanges);
        }
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.Count);
        Assert.Equal(0, db.PendingChanges);
    }

    [Fact]
    public async Task SerializedToFile_Serializes_And_Deserializes_As_Expected() {
        var file = Path.GetTempFileName();
        try {
            var db = await ArrowDb.CreateFromFile(file);
            db.Upsert("1", 1, JContext.Default.Int32);
            Assert.True(db.ContainsKey("1"));
            Assert.Equal(1, db.Count);
            Assert.Equal(1, db.PendingChanges);
            await db.SerializeAsync();
            var db2 = await ArrowDb.CreateFromFile(file);
            Assert.Equal(db2.Source, db.Source);
        } finally {
            // cleanup
            if (File.Exists(file)) {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task SerializedToFile_Serializes_And_Rollback_As_Expected() {
        var file = Path.GetTempFileName();
        try {
            var db = await ArrowDb.CreateFromFile(file);
            db.Upsert("1", 1, JContext.Default.Int32);
            Assert.True(db.ContainsKey("1"));
            Assert.Equal(1, db.Count);
            Assert.Equal(1, db.PendingChanges);
            await db.SerializeAsync();
            // clear the db (critical change)
            db.Clear();
            Assert.False(db.ContainsKey("1"));
            Assert.Equal(0, db.Count);
            Assert.Equal(1, db.PendingChanges);
            // rollback
            await db.RollbackAsync();
            // verification
            Assert.Equal(1, db.Count);
            Assert.Equal(0, db.PendingChanges);
            Assert.True(db.ContainsKey("1"));
            Assert.True(db.TryGetValue("1", JContext.Default.Int32, out var value));
            Assert.Equal(1, value);
        } finally {
            // cleanup
            if (File.Exists(file)) {
                File.Delete(file);
            }
        }
    }
}