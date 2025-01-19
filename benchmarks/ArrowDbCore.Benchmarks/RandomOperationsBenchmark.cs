using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Bogus;

namespace ArrowDbCore.Benchmarks;

[MemoryDiagnoser(false)]
[RankColumn]
[MediumRunJob]
public class RandomOperationsBenchmarks {
    private Person[] _items = [];
    private ArrowDb _db = default!;

    [Params(100, 10_000, 1_000_000)]
    public int Count { get; set; }

    [IterationSetup]
    public void Setup() {
        var faker = new Faker {
            Random = new Randomizer(1337)
        };

		_items = Person.GeneratePeople(Count, faker).ToArray();

        Trace.Assert(_items.Length == Count);

        _db = ArrowDb.CreateInMemory().GetAwaiter().GetResult();
    }

    [Benchmark]
    public void RandomOperations() {
        Parallel.For(0, Count, i => {
            // Pick a random operation: 0 = add/update, 1 = remove
            int operationType = Random.Shared.Next(0, 2);

            var item = _items[i];

            var key = item.Name;
            var jsonTypeInfo = JContext.Default.Person;

            switch (operationType) {
                case 0: // Add/Update
                    _db.Upsert(key, item, jsonTypeInfo);
                    break;
                case 1: // Remove
                    _db.TryRemove(key);
                    break;
            }
        });
    }
}