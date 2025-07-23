using System.Security.Cryptography;

using ArrowDbCore.Tests.Common;

namespace ArrowDbCore.Tests.Unit;

public class Transactions {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NestedTransactionScope_SerializesOnce(bool useAes) {
        // Arrange
        var path = Path.GetTempFileName();
        using var aes = Aes.Create();
        var db = await CreateDb(path, useAes, aes);
        var person = new Person { Name = "John", Age = 42, BirthDate = DateTime.UtcNow, IsMarried = false };

        // Act
        await using (var scope1 = db.BeginTransaction()) {
            db.Upsert("key1", person, JContext.Default.Person);
            Assert.Equal(1, db.PendingChanges);

            await using (var scope2 = db.BeginTransaction()) {
                db.Upsert("key2", person, JContext.Default.Person);
                Assert.Equal(2, db.PendingChanges);

                // Still shouldn't serialize
            }
            var db2 = await CreateDb(path, useAes, aes);
            Assert.Equal(0, db2.Count);

            Assert.Equal(2, db.PendingChanges);
        }

        // Assert
        var db3 = await CreateDb(path, useAes, aes);
        Assert.Equal(2, db3.Count);
        Assert.Equal(0, db3.PendingChanges);
        File.Delete(path);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RollbackAsync_RevertsChanges(bool useAes) {
        // Arrange
        var path = Path.GetTempFileName();
        using var aes = Aes.Create();
        var db = await CreateDb(path, useAes, aes);
        var person = new Person { Name = "John", Age = 42, BirthDate = DateTime.UtcNow, IsMarried = false };

        db.Upsert("key1", person, JContext.Default.Person);
        await db.SerializeAsync();
        Assert.Equal(1, db.Count);
        Assert.Equal(0, db.PendingChanges);

        // Act
        db.Upsert("key2", person, JContext.Default.Person);
        Assert.Equal(2, db.Count);
        Assert.Equal(1, db.PendingChanges);

        await db.RollbackAsync();

        // Assert
        Assert.Equal(1, db.Count);
        Assert.Equal(0, db.PendingChanges);
        Assert.True(db.ContainsKey("key1"));
        Assert.False(db.ContainsKey("key2"));
        File.Delete(path);
    }

    private async Task<ArrowDb> CreateDb(string path, bool useAes, Aes? aes = null) {
        if (useAes) {
            return await ArrowDb.CreateFromFileWithAes(path, aes!);
        }

        return await ArrowDb.CreateFromFile(path);
    }
}
