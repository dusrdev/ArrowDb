using System.Diagnostics;

using ArrowDbCore.Benchmarks.Common;

using BenchmarkDotNet.Attributes;

using Bogus;

using Person = ArrowDbCore.Benchmarks.Common.Person;

namespace ArrowDbCore.Benchmarks.VersionComparison;

[MemoryDiagnoser(false)]
[RankColumn]
[Config(typeof(VersionComparisonConfig))]
public class SerializationToFileBenchmarks
{
    private ArrowDb _db = default!;
    private string _dbPath = default!;

    [Params(100, 10_000, 1_000_000)]
    public int Size { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var faker = new Faker
        {
            Random = new Randomizer(1337)
        };

        _dbPath = $"test-{Guid.NewGuid():N}.db";
        _db = ArrowDb.CreateFromFile(_dbPath).AsTask().GetAwaiter().GetResult();

        Span<char> buffer = stackalloc char[64];

        foreach (var person in Person.GeneratePeople(Size, faker))
        {
            _ = person.Id.TryFormat(buffer, out var written);
            var id = buffer.Slice(0, written);
            _db.Upsert(id, person, JContext.Default.Person);
        }

        Trace.Assert(_db.Count == Size);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Benchmark]
    public async Task SerializeAsync()
    {
        await _db.SerializeAsync();
    }
}
