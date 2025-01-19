using Bogus;

namespace ArrowDbCore.Benchmarks;

public sealed class Person {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public int Age { get; set; }

	public static IEnumerable<Person> GeneratePeople(int count, Faker faker) {
		for (var i = 0; i < count; i++) {
			yield return new Person {
				Id = i,
				Name = faker.Name.FirstName(),
				Surname = faker.Name.LastName(),
				Age = faker.Random.Int(0, 100)
			};
		}
	}
}