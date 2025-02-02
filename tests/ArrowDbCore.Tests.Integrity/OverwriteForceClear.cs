using System.Security.Cryptography;

using Bogus;

namespace ArrowDbCore.Tests.Integrity;

public class OverwriteForceClear {
    private static async Task SerializeOverwritesExistingFile(string path, Func<ValueTask<ArrowDb>> factory) {
        const int itemCount = 1_000;

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
            // now we have sample data to verify overwrite
            var fileSize = new FileInfo(path).Length;
            // now we overwrite
            db.Clear();
            await db.SerializeAsync();
            // clear data and overwritten (file should next to empty - aside from headers)
            var newFileSize = new FileInfo(path).Length;
            // check if new is smaller
            Assert.True(newFileSize < fileSize);
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        // this test fails if an exception is thrown or the file is not overwritten
    }

    [Fact]
    public async Task SerializeOverwritesExistingFile_FileSerializer() {
        var path = Sharpify.Utils.Env.PathInBaseDirectory("overwrite-test-file-serializer.db");
        await SerializeOverwritesExistingFile(path, () => ArrowDb.CreateFromFile(path));
    }

    [Fact]
    public async Task SerializeOverwritesExistingFile_AesFileSerializer() {
        var path = Sharpify.Utils.Env.PathInBaseDirectory("overwrite-test-aes-file-serializer.db");
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        await SerializeOverwritesExistingFile(path, () => ArrowDb.CreateFromFileWithAes(path, aes));
    }
}