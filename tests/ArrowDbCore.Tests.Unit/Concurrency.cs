using System.Security.Cryptography;

using ArrowDbCore.Tests.Common;

namespace ArrowDbCore.Tests.Unit;

public class Concurrency
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Concurrent_Writes_ShouldBe_ThreadSafe(bool useAes)
    {
        // Arrange
        var path = Path.GetTempFileName();
        using var aes = Aes.Create();
        ArrowDb? db = null;
        ArrowDb? db2 = null;
        try
        {
            db = await CreateDb(path, useAes, aes);
            var person = new Person { Name = "John", Age = 42, BirthDate = DateTime.UtcNow, IsMarried = false };
            var taskCount = 100;
            var tasks = new Task[taskCount];

            // Act
            for (var i = 0; i < taskCount; i++)
            {
                var key = $"key{i}";
                tasks[i] = Task.Run(() => db.Upsert(key, person, JContext.Default.Person));
            }

            await Task.WhenAll(tasks);
            await db.SerializeAsync();
            FileBackedTestHelpers.ReleaseOwnership(db);

            // Assert
            db2 = await CreateDb(path, useAes, aes);
            Assert.Equal(taskCount, db2.Count);
        }
        finally
        {
            if (db2 is not null)
            {
                FileBackedTestHelpers.ReleaseOwnership(db2);
            }

            if (db is not null)
            {
                FileBackedTestHelpers.ReleaseOwnership(db);
            }

            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    private async Task<ArrowDb> CreateDb(string path, bool useAes, Aes? aes = null)
    {
        if (useAes)
        {
            return await ArrowDb.CreateFromFileWithAes(path, aes!);
        }

        return await ArrowDb.CreateFromFile(path);
    }
}
