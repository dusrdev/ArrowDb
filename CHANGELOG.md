# Changelog (Sorted by Date in Descending Order)

## 2.0.0.0

- Added optional `CancellationToken` parameters to ArrowDb async APIs, including factory initialization, `SerializeAsync`, `RollbackAsync`, `GetOrAddAsync`, and `BeginTransaction`.
- Updated `GetOrAddAsync` factory delegates to receive the active `CancellationToken`.
- Updated the public `IDbSerializer` contract to receive an optional `CancellationToken` for serialization and deserialization.
- Transaction scopes can now carry a cancellation token into the outermost implicit serialize during disposal.
- This is a breaking release for callers implementing `IDbSerializer` or calling `GetOrAddAsync` with the old delegate shapes.

## 1.6.0.0

- Improve correctness of internal change counting to ensure that changes that happened during serialization are still tracked.
- `TryGetValue` will now return true for `value types` that have a default value since it is a valid value for them.
- `Upsert` can return `false` if a `RollbackAsync` occurred concurrently, indicating the write was not reliable relative to the rollback (retry after rollback completes if needed).
- `TryRemove` and `TryClear` can return `false` if a `RollbackAsync` occurred concurrently, indicating the operation was not reliable relative to the rollback.
- `Clear` is now obsolete; use `TryClear` to detect rollback races.

## 1.5.0.0

- File based serializers `FileSerializer` and `AesFileSerializer` now use a new base class implementation and have gained the ability to `journal` (maintain durability through crashes and other `IOException`, and ensure successful atomic write or complete rejection of changes), and cross-process isolation, preventing race condition that could be caused when multiple processes try to access the same `ArrowDb` file.
  - If you had a class implementing `FileSerializer` this change may or may not break functionality and you should run tests to ensure everything still works as expected (With that said, my tests were not broken and did not require any adjusting).
- Thread-safe counters types were changed from `int` to `long`, this includes `PendingChanges` and `RunningInstances`.
- `ArrowDbTransactionScope` was updated to allow nested transactions, and prevent corruption that can be caused by multiple transactions running concurrently on the same `ArrowDb` instance. In addition, it was made `public` and also implements the regular `IDisposable` interface to allow usage in synchronous context. However, it is still your responsibility to ensure the contexts match.
- `Upsert` and all its overloads will now reject (return `false`) whenever the value to be upserted is a `null` reference type. This is to enforce a no `null` policy that will simplify development by eliminating `null` checks on retrieved values.

### Perf Improvements

- Random queued (Non serialized) operations are up to 20% faster due to improvement in cross-threaded state management.
- Serialization allocates up to 250% less memory 🔥 across all benchmarks.

## 1.4.0.0

- `GetOrAddAsync` and `Upsert` (which has the `updateCondition` argument) now both have overloads that accept a `TArg` parameter as well as a modified factory function that can use it, in order to avoid closures.

## 1.3.0.0

- Added overloads of `Upsert` that accept a `string` key, while the `ReadOnlySpan<char>` overloads are amazing in specific cases where its use can prevent a string allocation for the lookup, in other places where the input was originally a `string` that was implicitly converted to a `ReadOnlySpan<char>` for the parameter, this would've caused a copy to be allocated for the key when the key did not exist. The same scenario will now use the `string` overload and use it for the key directly, avoiding the intermediate copy.
- Added a `ValueTask` based `GetOrAddAsync` method, commonly used in caching scenarios.

## 1.2.0.0

- An overload to `Upsert` without `updateCondition` was added and would now act as default path in case `updateCondition` wasn't specified, this should further optimize such cases by removing condition checks and another reference from the stack during runtime.
- Internal methods which are rather small and frequently invoked will now be prioritized for inlining by JIT, this should slightly improve perf, especially in NativeAot.
- Added a new factory initializer `CreateFromFileWithAes` that received an `Aes` instance as parameter. It will then use it to encrypt and decrypt the output and input during serialization and deserialization respectively.

## 1.1.0.0

- Fixed issue with `FileSerializer` where serialization would write over existing file data which could create invalid tokens, causing deserialization to fail.
- Added static `ArrowDb.GenerateTypedKey<T>` method that accepts the type of the value, specific key (identifier) and a buffer, it returns a `ReadOnlySpan<char>` key that prefixes the type to the specific key.

## 1.0.0.0

- Initial Release
