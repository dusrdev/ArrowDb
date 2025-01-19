namespace ArrowDbCore.Tests.Unit;

public class Upserts {
    [Fact]
    public async Task Upsert_When_Not_Found_Inserts() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.Count);
    }

    [Fact]
    public async Task Upsert_When_Found_Overwrites() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.ContainsKey("1"));
        Assert.Equal(1, db.Count);
        db.Upsert("1", 2, JContext.Default.Int32);
        Assert.True(db.TryGetValue("1", JContext.Default.Int32, out var value));
        Assert.Equal(2, value);
    }

    [Fact]
    public async Task Conditional_Update_When_Not_Found_Inserts() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        Assert.True(db.Upsert("1", 1, JContext.Default.Int32, reference => reference == 3));
    }

    [Fact]
    public async Task Conditional_Update_When_Found_And_Valid_Updates() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.Upsert("1", 3, JContext.Default.Int32, reference => reference == 1));
        Assert.True(db.TryGetValue("1", JContext.Default.Int32, out var value));
        Assert.Equal(3, value);
    }

    [Fact]
    public async Task Conditional_Update_When_Found_And_Invalid_Returns_False() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.False(db.Upsert("1", 3, JContext.Default.Int32, reference => reference == 3));
        Assert.True(db.TryGetValue("1", JContext.Default.Int32, out var value));
        Assert.Equal(1, value);
    }
}