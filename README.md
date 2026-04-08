<div align="center">

  ![ArrowDb-logo](https://github.com/user-attachments/assets/b90af6b1-8ed0-44dd-a1f5-50a238d6dc14)

</div>
<div align="center">

  [![NuGet Downloads](https://img.shields.io/nuget/dt/ArrowDb?style=flat&label=Nuget%20-%20ArrowDb)](https://www.nuget.org/packages/ArrowDb)
  [![Tests](https://github.com/dusrdev/ArrowDb/actions/workflows/unit-tests-matrix.yaml/badge.svg)](https://github.com/dusrdev/ArrowDb/actions/workflows/unit-tests-matrix.yaml)

</div>

ArrowDb is a fast, lightweight, and type-safe key-value database designed for .NET.

* Super-Lightweight (dll size is ~19KB - approximately 9X smaller than [UltraLiteDb](https://github.com/rejemy/UltraLiteDB))
* Ultra-Fast (1,000,000 random operations / ~98ms on M2 MacBook Pro)
* Minimal-Allocation (constant ~520 bytes for serialization of any db size)
* Thread-Safe and Concurrent
* ACID compliant on transaction level
* Type-Safe (no reflection - compile-time enforced via source-generated `JsonSerializerContext`)
* Cross-Platform and Fully AOT-compatible
* Super-Easy API near mirroring of `Dictionary<TKey, TValue>`

### A Note on `null` Values

ArrowDb enforces a "no nulls" policy by design. Attempting to `Upsert` a `null` value will be rejected and return `false`. This simplifies the developer experience by guaranteeing that if a key exists, its value is never `null`. This eliminates the need for null-checking after retrieval, leading to cleaner and more predictable application code.

This policy does not affect value types (`structs`); their `default` values (e.g., `0` for an `int`) are considered valid.

## Getting Started

Installation is done via NuGet: `dotnet add package ArrowDbCore`

Initializing the db is done via the factory methods, they return the instance as `ValueTask` and may or may not be asynchronous depending on the selected serializer implementation. The default serializer is `FileSerializer`, which serializes the db to a file on disk. These async APIs accept an optional `CancellationToken`. The following example demonstrates the basic usage, and more details on serializers will be discussed later.

```csharp
// manual instance creation
var db = await ArrowDb.CreateFromFile("path.db");
// or with dependency injection
builder.Services.AddSingleton(_ => ArrowDb.CreateFromFile("path.db").GetAwaiter().GetResult());
// the default DI container doesn't support async, so we hack it with GetAwaiter().GetResult()
// this will block during startup while the serializer performs file I/O
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

Now we can upsert (insert or update, similar to "put") a `Person` into the db:

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

* Using `TryGetValue` with an incorrect `JsonTypeInfo<T>` (wrong type) will cause a `JsonException` to be thrown. If your call site cannot guarantee a type, be sure to handle the possibility of an exception.

Up until now, the data was stored in-memory, to finalize and persist the changes, we need to call:

```csharp
await db.SerializeAsync();
// or
await db.SerializeAsync(cancellationToken);
```

## APIs

For tracking some ArrowDb internals the following properties are exposed:

```csharp
long ArrowDb.RunningInstances;  // Number of active ArrowDb instances (static)
long db.PendingChanges;          // The number of pending changes (number of changes that have not been serialized)
int db.Count;                   // The number of entities in the ArrowDb
```

For reading the data we have the following methods:

```csharp
bool db.ContainsKey(ReadOnlySpan<char> key);  // checks if the ArrowDb instance contains the specified key
bool db.TryGetValue<TValue>(ReadOnlySpan<char> key, JsonTypeInfo<TValue> jsonTypeInfo, out TValue value);  // tries to read and parse a value from the ArrowDb instance
```

Notice that all APIs accept keys as `ReadOnlySpan<char>` to avoid unnecessary allocations. This means that if you check for a key by some slice of a string, there is no need to allocate a string just for the lookup.

Upserting (adding or updating, similar to "put") is done via 6 overloads:

```csharp
bool db.Upsert<TValue>(string key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo);
bool db.Upsert<TValue>(string, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, bool> updateCondition);
bool Upsert<TValue, TArg>(string key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, TArg, bool> updateCondition, TArg updateConditionArgument);
bool db.Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo);
bool db.Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, bool> updateCondition);
bool Upsert<TValue, TArg>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, TArg, bool> updateCondition, TArg updateConditionArgument);
```

### Upsert Overloads Best Practices

As noted above, There are 2 main upsert methods, but each has 2 options, a `ReadOnlySpan<char>` key, or a `string` key.

The `ReadOnlySpan<char>` methods are best used for scenarios where the key is generated using a slice of a string (whether from regular string, stackallocated buffers, interop and so and forth), and cases where the value for the same key is being frequently updated, in which case this method will replace the value without allocating the key.

The `string` methods are best for addition and cases where the parameter in the caller method is already of type `string`, in these case the direct use of `string` will prevent a `string` allocation by the lookup.

And removal:

```csharp
bool db.TryRemove(ReadOnlySpan<char> key);  // removes the entry with the specified key
bool db.TryClear();                        // clears all entries; returns false if a concurrent RollbackAsync occurred
void db.Clear();                           // obsolete: use TryClear()
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

In this example, `referenceDate` is a local value, and when used inside the lambda of the `updateCondition` it allocates a [Closure](https://www.youtube.com/watch?v=h3MsnBRqzcY), which depending on whether is a performance critical code section, could be sub-optimal. To address this, a secondary overload is available:

```csharp
bool Upsert<TValue, TArg>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, TArg, bool> updateCondition, TArg updateConditionArgument)

// adapt example to use this
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
    if (db.Upsert("shopping list", note, MyJsonContext.Default.Note, (reference, date) => reference.LastUpdatedUTC == date), referenceDate) {
        noteUpdated = true; // note was updated - this will break out of the loop
    }
} while (!noteUpdated);
```

Using the overload we created a different lambda, in which there are no 2 input arguments, one of which is the date to check against, and the scope of the lambda only uses its parameters, which in turn means that no class has to be allocated for the closure, and instead the compiler will generate the lambda as a static method, the argument would then be forwarded from `Upsert` into the lambda during runtime. Avoiding the performance penalty of allocating a closure class for each call.

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

A common code pattern for caching usually consists of some `GetOrAdd` method, that will check if a value exists by the key, and return it, otherwise it will accept a method used to generate the value, which will be used to add the value to the cache, then return it.

`ArrowDb` supports this via the `async ValueTask` method:

```csharp
async ValueTask<TValue> GetOrAddAsync<TValue>(string key, JsonTypeInfo<TValue> jsonTypeInfo, Func<string, CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default);
async ValueTask<TValue> GetOrAddAsync<TValue, TArg>(string key, JsonTypeInfo<TValue> jsonTypeInfo, Func<string, TArg, CancellationToken, ValueTask<TValue>> valueFactory, TArg factoryArgument, CancellationToken cancellationToken = default);
```

If the value exists, the asynchronous factory method is not called, and the value is returned synchronously. Otherwise the factory will receive the key and the supplied `CancellationToken`, produce the value, `Upsert` it, then return it.

### Concurrency Note

`GetOrAddAsync` is intentionally **not atomic**. Under concurrency, `valueFactory` may be invoked multiple times for the same key, and the final stored value is last-writer-wins (because the value is persisted via `Upsert`). If you need single-invocation semantics for the factory (e.g. side-effects/expensive work), guard the call site with a keyed lock.

Since `ArrowDb` was not made specifically to cache, it doesn't store time metadata for values, because of this, there will not be a method that accepts "cache expiration" or similar options in the foreseen future. Such scenarios will need to implemented client-side, best done with a pattern that splits read and write, by called `TryGetValue` which will also check the inner time reference, if false and out of date, will generate the value and use `Upsert`.

Similarly to `Upsert` - `GetOrAddAsync` also has an overload that accepts `TArg` and and enables closure free execution for optimal performance.

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
    ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default);
    ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default);
}
```

The `DeserializeAsync` method is invoked to load the db, and the `SerializeAsync` method is invoked to persist the db. For custom file-based serializers, it is recommended to inherit from `BaseFileSerializer` to get atomic writes, single-owner writable file semantics, and async file I/O out of the box.

Being that they return a `ValueTask`, the implementations can be async. This means that you can even implement serializers to persist the db to a remote server, or cloud, or whatever else you want.

### Reducing Redundant Work

Each call to `SerializeAsync` will also check the `PendingChanges` counter, if it is 0, the method will return immediately, this means that you can also create a background service that periodically calls `SerializeAsync` to persist the db, without fear of wasting resources.

## Transactions

A transaction in the context of `ArrowDb`, is any sequence of operations with no count or time limit, that explicitly ends with a call to `SerializeAsync` to persist the changes.

Until `SerializeAsync` is called, no changes are persisted, and they are only stored in memory.

In case you want to rollback the changes, you can call the following method:

```csharp
await db.RollbackAsync();
// or
await db.RollbackAsync(cancellationToken);
```

`RollbackAsync` restores the last persisted state (as returned by your current serializer) by:

1. The persisted version of the db is deserialized using the `DeserializeAsync` method of the current serializer.
2. The db is cleared.
3. The db source reference is atomically replaced with the persisted version.
4. Pending changes counter is reset to 0.

## File-backed ownership

The built-in file-backed serializers (`FileSerializer` and `AesFileSerializer`) are single-owner writable. The first process that opens a database file owns it for the lifetime of that serializer instance. A second writable open against the same path fails fast with `ArrowDbOwnershipException`.

This is intentional: ArrowDb keeps the live state in-process and persists snapshots to disk. The persisted file is not a shared live database between processes.

### Concurrency note: `RollbackAsync` and writers

`RollbackAsync` is intended to be a rare operation. For best results, avoid running it concurrently with writers.

To keep the write path fast, ArrowDb does not take a global lock on every write. Instead, `Upsert` detects a concurrent rollback and will return `false` if a rollback happened during the operation, indicating the update was not reliable relative to the rollback.

If `Upsert` returns `false` due to a concurrent rollback, the in-memory state may or may not contain the attempted update (depending on timing). If you need the update to be applied reliably, retry the upsert after rollback completes.

The same “not reliable relative to rollback” behavior applies to other mutating operations:

- `TryRemove` returns `false` if a rollback occurred concurrently.
- `TryClear` returns `false` if a rollback occurred concurrently.

### Transaction Scope

While the above definition explains how users can manually control the transaction by explicitly calling `SerializeAsync`, `ArrowDb` also provides a transaction scope that can defer an implicit the call to `SerializeAsync` when the scope is disposed. This was inspired by the way that [ZigLang](https://ziglang.org/) uses `defer` immediately after allocating memory to [ensure the memory is deallocated at the end of the scope](https://ziglang.org/documentation/master/#Choosing-an-Allocator), this helps prevent issues caused by forgetting to deallocate memory (in Zig) or in this case - forgetting to call `SerializeAsync`.

```csharp
var db = await ArrowDb.CreateFromFile("path.db");
// this uses a "using" statement.
await using (var scope = db.BeginTransaction(cancellationToken)) {
    db.Upsert(john.Name, john, MyJsonContext.Default.Person);
}
// the scope was disposed, and SerializeAsync was called implicitly
// The same also works with a "using" declaration, that will bind to the containing scope
void SomeMethod() {
    await using var scope = db.BeginTransaction(cancellationToken);
    db.Upsert(john.Name, john, MyJsonContext.Default.Person);
} // the function scope ends here, and implicitly closes the scope of the transaction
```

Using a transaction scope ensures that `SerializeAsync` is always called, even if an `Exception` is thrown. These scopes can be nested, and serialization will only occur when the outermost scope is disposed. If the `CancellationToken` passed to the outermost scope is canceled before disposal commits, the implicit serialize throws `OperationCanceledException` and the pending changes remain in memory until you retry `SerializeAsync` or call `RollbackAsync`.

`ArrowDbTransactionScope` also implements the regular `IDisposable` interface, meaning it can be used in a non-`async` method. However it internally calls the `DisposeAsync` method in a blocking manner. This works with the built-in file-based serializers, but it will block on file I/O during commit. In asynchronous code, prefer the `Async Disposable` pattern accordingly.

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
