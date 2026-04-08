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
        ArrowDb? db = null;
        ArrowDb? db3 = null;
        try {
            db = await CreateDb(path, useAes, aes);
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
                Assert.Equal(2, db.PendingChanges);
                Assert.Equal(0, new FileInfo(path).Length);
            }

            // Assert
            FileBackedTestHelpers.ReleaseOwnership(db);
            db3 = await CreateDb(path, useAes, aes);
            Assert.Equal(2, db3.Count);
            Assert.Equal(0, db3.PendingChanges);
        } finally {
            if (db3 is not null) {
                FileBackedTestHelpers.ReleaseOwnership(db3);
            }

            if (db is not null) {
                FileBackedTestHelpers.ReleaseOwnership(db);
            }

            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RollbackAsync_RevertsChanges(bool useAes) {
        // Arrange
        var path = Path.GetTempFileName();
        using var aes = Aes.Create();
        ArrowDb? db = null;
        try {
            db = await CreateDb(path, useAes, aes);
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
        } finally {
            if (db is not null) {
                FileBackedTestHelpers.ReleaseOwnership(db);
            }

            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    private async Task<ArrowDb> CreateDb(string path, bool useAes, Aes? aes = null) {
        if (useAes) {
            return await ArrowDb.CreateFromFileWithAes(path, aes!);
        }

        return await ArrowDb.CreateFromFile(path);
    }
}
