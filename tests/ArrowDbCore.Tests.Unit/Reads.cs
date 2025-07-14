using System.Text.Json;

using ArrowDbCore.Tests.Common;

namespace ArrowDbCore.Tests.Unit;

public class Reads {
    [Fact]
    public async Task TryGetValue_CurrentType() {
        var db = await ArrowDb.CreateInMemory();
        Assert.Equal(0, db.Count);
        db.Upsert("1", 1, JContext.Default.Int32);
        Assert.True(db.TryGetValue("1", JContext.Default.Int32, out var src));
        Assert.Equal(1, src);
        Assert.Equal(1, db.Count);
    }

    [Fact]
    public async Task TryGetValue_WrongType_ThrowsJsonException() {
        var db = await ArrowDb.CreateInMemory();
        Person ron = new() {
            Name = "Ron",
            Age = 50,
            BirthDate = TimeProvider.System.GetUtcNow().AddYears(-50).DateTime,
            IsMarried = false
        };
        Assert.Equal(0, db.Count);
        db.Upsert("ron", ron, JContext.Default.Person);
        // db should contain the key as a value was upserted
        Assert.True(db.ContainsKey("ron"));
        // TryGetValue should throw JsonException as deserialization into incorrect type should fail
        Assert.Throws<JsonException>(() => db.TryGetValue("ron", JContext.Default.Int32, out _));
    }
}