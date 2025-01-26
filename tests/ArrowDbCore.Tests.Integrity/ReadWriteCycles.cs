using Bogus;

namespace ArrowDbCore.Tests.Integrity;

public class ReadWriteCycles
{
    [Fact]
    public async Task FileIO_Passes_ReadWriteCycles()
    {
        const int iterations = 200;
        const int itemCount = 100;

        var faker = new Faker<Person>();
        faker.UseSeed(1337);
        faker.RuleFor(p => p.Name, (f, _) => f.Name.FullName());
        faker.RuleFor(p => p.Age, (f, _) => f.Random.Int(1, 100));
        faker.RuleFor(p => p.BirthDate, (f, _) => f.Date.Past(1, DateTime.Now.AddYears(-100)));
        faker.RuleFor(p => p.IsMarried, (f, _) => f.Random.Bool());

        var buffer = new char[256];

        var path = Sharpify.Utils.Env.PathInBaseDirectory("rdc-test.db");
        try {
            for (var i = 0; i < iterations; i++) {
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
            }
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        // this test fails if an exception is thrown
    }
}