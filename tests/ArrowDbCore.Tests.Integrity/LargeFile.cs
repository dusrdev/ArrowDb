using System.Security.Cryptography;

using Bogus;

namespace ArrowDbCore.Tests.Integrity;

public class LargeFile {
    private static async Task LargeFile_Passes_OneReadWriteCycle(string path, Func<ValueTask<ArrowDb>> factory) {
        const int itemCount = 500_000;

        var faker = new Faker<Person>();
        faker.UseSeed(1337);
        faker.RuleFor(p => p.Name, (f, _) => f.Name.FullName());
        faker.RuleFor(p => p.Age, (f, _) => f.Random.Int(1, 100));
        faker.RuleFor(p => p.BirthDate, (f, _) => f.Date.Past(1, DateTime.Now.AddYears(-100)));
        faker.RuleFor(p => p.IsMarried, (f, _) => f.Random.Bool());

        var buffer = new char[256];
        try {
            // load the db
            var db = await factory();
            // clear
            db.Clear();
            // add items
            for (var j = 0; j < itemCount; j++) {
                var person = faker.Generate();
                var key = ArrowDb.GenerateTypedKey<Person>(person.Name, buffer);
                db.Upsert(key, person, JContext.Default.Person);
            }
            // save
            await db.SerializeAsync();
            var actualCount = db.Count;
            // try to load again
            var db2 = await factory();
            Assert.Equal(actualCount, db2.Count);
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        // this test fails if an exception is thrown
    }

    [Fact]
    public async Task LargeFile_Passes_OneReadWriteCycle_FileSerializer() {
        var path = Sharpify.Utils.Env.PathInBaseDirectory("long-test-file-serializer.db");
        await LargeFile_Passes_OneReadWriteCycle(path, () => ArrowDb.CreateFromFile(path));
    }

    [Fact]
    public async Task LargeFile_Passes_OneReadWriteCycle_AesFileSerializer() {
        var path = Sharpify.Utils.Env.PathInBaseDirectory("long-test-aes-file-serializer.db");
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        await LargeFile_Passes_OneReadWriteCycle(path, () => ArrowDb.CreateFromFileWithAes(path, aes));
    }
}