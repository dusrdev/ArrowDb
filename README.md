<div align="center">

  ![ArrowDb-logo](https://github.com/user-attachments/assets/b90af6b1-8ed0-44dd-a1f5-50a238d6dc14)

</div>
<div align="center">

  [![NuGet Downloads](https://img.shields.io/nuget/dt/ArrowDb?style=flat&label=Nuget%20-%20ArrowDb)](https://www.nuget.org/packages/ArrowDb)
  [![Unit Tests](https://github.com/dusrdev/ArrowDb/actions/workflows/unit-tests.yaml/badge.svg)](https://github.com/dusrdev/ArrowDb/actions/workflows/unit-tests.yaml)
  [![Integrity Tests](https://github.com/dusrdev/ArrowDb/actions/workflows/integrity-tests.yaml/badge.svg)](https://github.com/dusrdev/ArrowDb/actions/workflows/integrity-tests.yaml)

</div>

ArrowDb is a fast, lightweight, and type-safe key-value database designed for .NET.

* Super-Lightweight (dll size is ~19KB - approximately 9X smaller than [UltraLiteDb](https://github.com/rejemy/UltraLiteDB))
* Ultra-Fast (1,000,000 random operations / ~100ms on M2 MacBook Pro)
* Minimal-Allocation (~2KB for serialization of 1,000,000 items)
* Thread-Safe and Concurrent
* ACID compliant on transaction level
* Type-Safe (no reflection - compile-time enforced via source-generated `JsonSerializerContext`)
* Cross-Platform and Fully AOT-compatible
* Super-Easy API near mirroring of `Dictionary<TKey, TValue>`

## Getting Started

Installation is done via NuGet: `dotnet add package ArrowDbCore`

Initializing the db is done via the factory methods, they return the instance as `ValueTask` and may or may not be asynchronous depending on the selected serializer implementation. The default serializer is `FileSerializer`, which serializes the db to a file on disk. The following example demonstrates its usage, and more details on serializers will be discussed later.

```csharp
// manual instance creation
var db = await ArrowDb.CreateFromFile("path.db");
// or with dependency injection
builder.Services.AddSingleton(_ => ArrowDb.CreateFromFile("path.db").GetAwaiter().GetResult());
// the default DI container doesn't support async, so we hack it with GetAwaiter().GetResult()
// in the case of ArrowDb FileSerializer, this ValueTask is actually synchronous so this is fine
// in cases of different serializers, you can use Lazy<T> or other workarounds
```

This will either create a new ArrowDb instance, or load an existing one from the specified path, if exists.

`ArrowDb` uses `string` as keys, and `byte[]` for values, it leverages the `JsonSerializerContext` to support serializing every type efficiently to `byte[]` as long as it has a `JsonTypeInfo` implementation. Let's see a `Person` example:

```csharp
public class Person {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public int Age { get; set; }
}

[JsonSerializable(typeof(Person))]
public partial class MyJsonContext : JsonSerializerContext {}
```

Now we can upsert (insert or update) a `Person` into the db:

```csharp
var john = new Person { Id = 1, Name = "John", Surname = "Doe", Age = 42 };
db.Upsert(john.Name, john, MyJsonContext.Default.Person);
```

john will be serialized to `byte[]` using the `JsonSerializerContext` and stored in the db.

And we can read it back:

```csharp
if (db.TryGetValue(john.Name, MyJsonContext.Default.Person, out var johnFromDb)) {
    Console.WriteLine($"Found {johnFromDb.Name} {johnFromDb.Surname}");
}
```

Up until now, the data was stored in-memory, to finalize and persist the changes, we need to call:

```csharp
await db.SerializeAsync();
```

## APIs

For tracking some ArrowDb internals the following properties are exposed:

```csharp
int ArrowDb.RunningInstances;  // Number of active ArrowDb instances (static)
int db.InstanceId;              // The id of this ArrowDb instance
int db.Count;                   // The number of entities in the ArrowDb
int db.PendingChanges;          // The number of pending changes (number of changes that have not been serialized)
```

For reading the data we have the following methods:

```csharp
bool db.ContainsKey(ReadOnlySpan<char> key);  // checks if the ArrowDb instance contains the specified key
bool db.TryGetValue<TValue>(ReadOnlySpan<char> key, JsonTypeInfo<TValue> jsonTypeInfo, out TValue value);  // tries to read and parse a value from the ArrowDb instance
```

Notice that all APIs accept keys as `ReadOnlySpan<char>` to avoid unnecessary allocations. This means that if you check for a key by some slice of a string, there is no need to allocate a string just for the lookup.

Upserting (adding or updating) is done via a 2 overloads:

```csharp
bool db.Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo);
bool db.Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, bool> updateCondition = null);  // upserts a value into the ArrowDb instance
```

And removal:

```csharp
bool db.TryRemove(ReadOnlySpan<char> key);  // removes the entry with the specified key
void Clear();                              // removes all entries from the ArrowDb instance
```

## Optimistic Concurrency Control

`ArrowDb` uses optimistic concurrency control as a way to resolve write conflicts, similar to MongoDb. This is done via the overload of `Upsert` with the `updateCondition` parameter.

The `updateCondition` is a predicate that is invoked on the reference value that is currently stored in the db under the same key.

For ArrowDb to reject the update (and return `false`), ALL of the following 2 conditions must be met:

1. An entry with the same key must exist in the db and be successfully parsed into the specified type.
2. The `updateCondition` predicate returns `false` when invoked on the reference value.

This means that all other cases would allow addition/update:

* If `updateCondition` is used and the key does not exist, the update is allowed - and regarded as an addition. If a strict "update or nothing" behavior is desired, combine `ContainsKey` into the workflow before calling `Upsert`.

To illustrate this, Let’s look at an example of a timestamped `Note` entity:

```csharp
public class Note {
    public DateTimeOffset LastUpdatedUTC { get; set; }
    public string Content { get; set; } = string.Empty;
}
bool noteUpdated = false; // track if conflict was resolved
do {
    if (!db.TryGetValue("shopping list", MyJsonContext.Default.Note, out Note? note)) {
        // note does not exist, I am skipping this condition as it is not part of the example
    }
    // we are here, so previous note was found
    var referenceDate = note.LastUpdatedUTC; // locally store the reference
    note!.Content += "Pizza"; // modify the note
    note.LastUpdatedUTC = TimeProvider.System.UtcNow; // update note timestamp
    // update on condition that the stored reference is still the same, by checking the timestamp
    if (db.Upsert("shopping list", note, MyJsonContext.Default.Note, reference => reference.LastUpdatedUTC == referenceDate)) {
        noteUpdated = true; // note was updated - this will break out of the loop
    }
} while (!noteUpdated);
```

As the example shows retries is the usual way to resolve these conflicts, but custom logic can also be used, you can simply reject the operation, and also use other loops or even `goto` statements if you are brave enough.

## `ReadOnlySpan<char>` Key Generation

`ArrowDb` APIs use `ReadOnlySpan<char>` for keys to minimize unnecessary string allocations. Usually using the API with `Upsert` doesn't require specific logic as string can also be interpreted as `ReadOnlySpan<char>`, however when checking if a key exists or removing keys, usually you don't have pre-existing reference to the key, which means you have to use rather low level APIs to efficiently generate a `ReadOnlySpan<char>` key.

To make this process much easier, and help with type safety, `ArrowDb` exposes a static `GenerateTypedKey<T>` method that accepts the type of the value, specific key (identifier) and a buffer, it returns a `ReadOnlySpan<char>` key that prefixes the type to the specific key.

For example, if you have a `Person` class (from examples above):

```csharp
// we need a buffer (we can rent one from a pool, or allocate it ourselves)
// in this example we will rent memory
using var memoryOwner = MemoryPool<char>.Shared.Rent(128);
// in this example 128 chars will be sufficient, use the smallest size that fits your needs
ReadOnlySpan<char> key = ArrowDb.GenerateTypedKey<Person>("john", buffer.Memory.Span);
// key is now ReadOnlySpan<char> that contains "Person:john"
// we can use it for Upsert, ContainsKey, TryGetValue, Remove, etc...
_ = db.ContainsKey(key);
_ = db.TryGetValue(key, MyJsonContext.Default.Person, out var person);
// etc...
```

This can also be used to filter out keys for mass lookups:

```csharp
// get all keys
var keys = db.Keys;
// get the type name
var prefix = typeof(Person).Name;
// get all keys where the value type is Person
var people = keys.Where(k => k.StartsWith(prefix));
```

## Use `ArrowDb` for Runtime Caching

`ArrowDb` is a great fit for runtime caching, as it is extremely lightweight, fast, type-safe and thread-safe. To support this use case, `ArrowDb` provides a ‘NoOp’ serializer that does not persist the data and keeps it in volatile memory. This is used via the factory method:

```csharp
var db = await ArrowDb.CreateInMemory();
// or with dependency injection
builder.Services.AddSingleton(() => ArrowDb.CreateInMemory().GetAwaiter().GetResult());
// Since this isn’t persisted, you may also use it as a Transient or Scoped service (whatever fits your needs).
```

## Encryption

As seen earlier, the default recommended serializer is `FileSerializer`, which serializes the db to a file on disk. In addition to it, `ArrowDb` also features a similar serializer that encrypts the db to a file on disk (the `AesFileSerializer`), for that the serializer requires an `Aes` instance to be passed along with the path to the file.

```csharp
string path = "store.db";
using var aes = Aes.Create();
var db = await ArrowDb.CreateFromFileWithAes(path, aes);
// or with dependency injection
builder.Services.AddSingleton(_ => Aes.Create());
builder.Services.AddSingleton(services => ArrowDb.CreateFromFileWithAes(path, services.GetRequiredService<Aes>()).GetAwaiter().GetResult());
```

## Serialization

To enhance the use cases of `ArrowDb` it was designed to allow for custom serialization (the methods of persisting the db).

There is factory method that accepts a custom `IDbSerializer` implementation:

```csharp
var db = await ArrowDb.CreateCustom(IDbSerializer);
```

The `IDbSerializer` is exposed and can be used to implement custom serializers:

```csharp
public interface IDbSerializer {
    ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync();
    ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data);
}
```

The `DeserializeAsync` method is invoked to load the db, and the `SerializeAsync` method is invoked to persist the db.

Being that they return a `ValueTask`, the implementations can be async. This means that you can even implement serializers to persist the db to a remote server, or cloud, or whatever else you want.

### Reducing Redundant Work

Each call to `SerializeAsync` will also check the `PendingChanges` counter, if it is 0, the method will return immediately, this means that you can also create a background service that periodically calls `SerializeAsync` to persist the db, without fear of wasting resources.

## Transactions

A transaction in the context of `ArrowDb`, is any sequence of operations with no count or time limit, that explicitly ends with a call to `SerializeAsync` to persist the changes.

Until `SerializeAsync` is called, no changes are persisted, and they are only stored in memory.

In case you want to rollback the changes, you can call the following method:

```csharp
await db.RollbackAsync();
```

`RollbackAsync` will block all writing threads, until the following is complete:

1. The persisted version of the db is deserialized using the `DeserializeAsync` method of the current serializer.
2. The db is cleared.
3. The db source reference is atomically replaced with the persisted version.
4. Pending changes counter is reset to 0.

### Transaction Scope

While the above definition explains how users can manually control the transaction by explicitly calling `SerializeAsync`, `ArrowDb` also provides a transaction scope that can defer an implicit the call to `SerializeAsync` when the scope is disposed. This was inspired by the way that [ZigLang](https://ziglang.org/) uses `defer` immediately after allocating memory to [ensure the memory is deallocated at the end of the scope](https://ziglang.org/documentation/master/#Choosing-an-Allocator), this helps prevent issues caused by forgetting to deallocate memory (in Zig) or in this case - forgetting to call `SerializeAsync`.

```csharp
var db = await ArrowDb.CreateFromFile("path.db");
// this uses a "using" statement.
await using (var scope = db.BeginTransaction()) {
    db.Upsert(john.Name, john, MyJsonContext.Default.Person);
}
// the scope was disposed, and SerializeAsync was called implicitly
// The same also works with a "using" declaration, that will bind to the containing scope
void SomeMethod() {
    await using var scope = db.BeginTransaction();
    db.Upsert(john.Name, john, MyJsonContext.Default.Person);
} // the function scope ends here, and implicitly closes the scope of the transaction
```

Using a transaction scope ensures that `SerializeAsync` is always called, even if an `Exception` is thrown.

## Subscribing to Changes

`ArrowDb` exposes an `OnChange` event that is raised whenever an operation that changes the database state, i.e, adding, updating, or removing a key, or clearing the database, is performed. The event is raised with a `ArrowDbChangeEventArgs` argument that contains the type of change that occurred.

```csharp
db.OnChange += (_, args) => {
    Console.WriteLine($"Change: {args.ChangeType}");
};
```

`args.ChangeType` is an enum and can be one of the following: `Upsert`, `Remove`, or `Clear`. All three of these types of changes are cached as `static readonly` instances of `ArrowDbChangeEventArgs` for minimal performance overhead.

The event can also act as way to trigger serialization after every change

```csharp
db.OnChange += async (sender, _) => {
    await ((ArrowDb)sender!).SerializeAsync();
};
```

Using the sender also prevents `closure capture`. But be careful as `SerializeAsync` blocks writing threads, using this event in combination with transactions that contain concurrent writes, can cause to significant performance degradation due to thread blocking.

## Saving External Objects

`ArrowDb` doesn't have built-in support for saving external objects like files, however, since any file can be represented as a `byte[]`, it is possible to create persist them anyway. Here's an example:

```csharp
// first we have to add an implementation to the JsonSerializerContext
[JsonSerializable(typeof(byte[]))] // we add this
[JsonSerializable(typeof(Person))]
public partial class MyJsonContext : JsonSerializerContext {}

var path = "/path/to/video.mp4"; // an example path
byte[] video = File.ReadAllBytes(path); // read the file as bytes
db.Upsert(path, video, MyJsonContext.Default.ByteArray); // upsert the bytes under the path as key
// Since the path contains the extension, writing the bytes to this path will yield the same result.
// Alternatively, you can create a nested class that contains the extension or whatever else you need
```

## Performance and Characteristics

The performance characteristics of ArrowDb, listed above, are based on benchmarks conducted on an M2 MacBook Pro using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet):

* [Random Operations, Upserts/Removals of `Person` [Count = 100, 10,000, 1,000,000]](benchmarks/ArrowDbCore.Benchmarks/BenchmarkDotNet.Artifacts/ArrowDbCore.Benchmarks.RandomOperationsBenchmarks-report-github.md)
* [SerializeAsync, Containing `Person` entries [Size = 100, 10,000, 1,000,000]](benchmarks/ArrowDbCore.Benchmarks/BenchmarkDotNet.Artifacts/ArrowDbCore.Benchmarks.SerializationToFileBenchmarks.md)

## Contributing

Contributions are welcome as suggestions, bug reports, and pull requests.

For inquiries or support, contact me at [dusrdev@gmail.com](mailto:dusrdev@gmail.com).

> This project was proudly made in 🇮🇱
