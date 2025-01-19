using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Bogus;

namespace ArrowDbCore.Benchmarks;

[MemoryDiagnoser(false)]
[RankColumn]
[MediumRunJob]
public class SerializationToFileBenchmarks {
    private ArrowDb _db = default!;

    [Params(100, 10_000, 1_000_000)]
    public int Size { get; set; }

    [IterationSetup]
    public void Setup() {
        var faker = new Faker {
            Random = new Randomizer(1337)
        };

        _db = ArrowDb.CreateFromFile("test.db").GetAwaiter().GetResult();

        Span<char> buffer = stackalloc char[64];

		foreach (var person in Person.GeneratePeople(Size, faker)) {
            _ = person.Id.TryFormat(buffer, out var written);
            var id = buffer.Slice(0, written);
            _db.Upsert(id, person, JContext.Default.Person);
        }

        Trace.Assert(_db.Count == Size);
    }

    [IterationCleanup]
    public void Cleanup() {
        if (File.Exists("test.db")) {
            File.Delete("test.db");
        }
    }

    [Benchmark]
    public async Task SerializeAsync() {
        await _db.SerializeAsync();
    }
}