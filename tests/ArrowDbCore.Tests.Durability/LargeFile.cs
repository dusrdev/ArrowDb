using Bogus;

namespace ArrowDbCore.Tests.Durability;

public class LargeFile {
    [Fact]
    public async Task LargeFile_Passes_OneReadWriteCycle() {
        const int itemCount = 500_000;

        var faker = new Faker<Person>();
        faker.UseSeed(1337);
        faker.RuleFor(p => p.Name, (f, _) => f.Name.FullName());
        faker.RuleFor(p => p.Age, (f, _) => f.Random.Int(1, 100));
        faker.RuleFor(p => p.BirthDate, (f, _) => f.Date.Past(1, DateTime.Now.AddYears(-100)));
        faker.RuleFor(p => p.IsMarried, (f, _) => f.Random.Bool());

        var buffer = new char[256];

        var path = Sharpify.Utils.Env.PathInBaseDirectory("long-test.db");
        try {
            // load the db
            var db = await ArrowDb.CreateFromFile(path);
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
            var db2 = await ArrowDb.CreateFromFile(path);
            Assert.Equal(actualCount, db2.Count);
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        // this test fails if an exception is thrown
    }
}