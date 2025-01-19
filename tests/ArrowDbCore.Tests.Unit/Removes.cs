namespace ArrowDbCore.Tests.Unit;

public class Removes {
    [Fact]
    public async Task TryRemove_When_Not_Found_Returns_False() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        Assert.False(db.TryRemove("1"));
    }

    [Fact]
    public async Task TryRemove_When_Found_Returns_True() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.TryRemove("1"));
    }

    [Fact]
    public async Task TryRemove_When_Found_Removes() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.Equal(1, db.Count);
        Assert.True(db.TryRemove("1"));
        Assert.False(db.ContainsKey("1"));
        Assert.False(db.TryGetValue("1", JContext.Default.Int32, out var value));
        Assert.Equal(0, value);
        Assert.Equal(0, db.Count);
    }

    [Fact]
    public async Task Clear_Removes_All_From_Db() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        db.Upsert("2", 2, JContext.Default.Int32);
        Assert.Equal(2, db.Count);
        db.Clear();
        Assert.False(db.ContainsKey("1"));
        Assert.False(db.ContainsKey("2"));
        Assert.False(db.TryGetValue("1", JContext.Default.Int32, out _));
        Assert.False(db.TryGetValue("2", JContext.Default.Int32, out _));
        Assert.Equal(0, db.Count);
    }
}