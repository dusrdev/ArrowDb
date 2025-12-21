# AGENTS.md

This file contains repo-specific instructions for AI coding agents working in this repository.

## Critical points before answering any question or performing any task

- Never assume based on read data from earlier points in the conversation or logical guesses - Always read the latest version of each file that is references in the tasks or conversation, unless a different version is explicitly asked for.
- Never change the source project/code to make something in the unit tests easier when it costs perf or otherwise. The source code is much more important than testing convenience. If you have suggestion on how to refactor the source to allow easier testing - always prompt the user asking if he would like them implemented, never assume that without asking.
- When answering about the capabilities, source code and otherwise of 3rd party libraries ensure to always give the most correct and up-to-date answer. If you are not sure, search the web to find what happens in the exact version used.
- When discussing APIs, make sure all of your logic aligns with whether the APIs are publicly used / public but only used internally / or internal.
- If you require source that you don't have access to - ask the user if he may provide them.
- Always start answering questions about the codebase, or tasks by reading AGENTS.md, and make sure to keep it up-to-date.
- If public APIs or usage semantics change, prompt the user and asks if he would like to update the README.md / Changelog files (if they exist).

## Repo overview (ArrowDbCore)

- Product: ArrowDb (NuGet package id: `ArrowDb`) - fast, lightweight, type-safe key-value database for .NET.
- Runtime target: `net9.0` (repo relies on .NET 9 APIs such as `ConcurrentDictionary<,>.AlternateLookup<ReadOnlySpan<char>>`).
- Goals: tiny footprint, minimal allocations, thread-safe concurrency, AOT + trimming compatibility, System.Text.Json source-gen (no reflection).

## How ArrowDb works (implementation notes)

- Data model: an in-memory `ConcurrentDictionary<string, byte[]>` (`ArrowDb.Source`).
  - Keys are `string` in storage; many APIs accept `ReadOnlySpan<char>` to avoid allocations when looking up/removing keys derived from slices.
  - Values are UTF-8 JSON bytes produced by `System.Text.Json` using caller-provided `JsonTypeInfo<T>` (source-generated metadata).
- Type safety: `TryGetValue<T>`/`Upsert<T>` require a `JsonTypeInfo<T>`; passing the wrong `JsonTypeInfo` for stored bytes can throw `JsonException`.
- Change tracking:
  - Any successful mutation calls `OnChangeInternal(...)`, which increments `_pendingChanges` and then invokes the `OnChange` event.
  - `PendingChanges` is a `long` counter; `SerializeAsync()` resets it to `0` only if no new changes happened during the serialization window (conditional reset to avoid losing the “needs another serialize” signal).
- Concurrency model:
  - Normal reads/writes are lock-free at the dictionary level (`ConcurrentDictionary`).
  - A per-instance `SemaphoreSlim` guards `SerializeAsync()`/`RollbackAsync()`. Writers do not take the semaphore, but they do call `WaitIfSerializing()` to avoid mutating while a serialize is actively in progress.
  - `RollbackAsync()` increments a monotonic in-memory epoch (`StateEpoch`). Mutating operations (`Upsert`, `TryRemove`, `TryClear`) detect an epoch change during the operation and return `false` to signal the mutation was not reliable relative to the rollback.
  - Multi-process safety for file serializers is implemented via a system-wide named `Mutex` in `BaseFileSerializer` (per DB path).
- Transactions:
  - `BeginTransaction()` returns `ArrowDbTransactionScope`.
  - `ArrowDbTransactionScope` increments `ArrowDb.TransactionDepth` on creation and decrements on dispose.
  - Only when the outermost scope is disposed (`TransactionDepth` reaches `0`) does it call `SerializeAsync()`; nested scopes do not serialize.

## Source code map (what lives where)

- `src/ArrowDbCore/ArrowDb.cs`: core state (`Source`, `Lookup`, `Serializer`, `Semaphore`), counters (`RunningInstances`, `PendingChanges`), `OnChange` event, and `BeginTransaction()`.
- `src/ArrowDbCore/ArrowDb.Factory.cs`: factory initializers (`CreateFromFile`, `CreateFromFileWithAes`, `CreateInMemory`, `CreateCustom`) + `GenerateTypedKey<T>(...)`.
- `src/ArrowDbCore/ArrowDbJsonContext.cs`: internal `JsonSerializerContext` used by file serializers to (de)serialize `ConcurrentDictionary<string, byte[]>` without reflection.
- `src/ArrowDbCore/ArrowDb.Read.cs`: read-only API (`Count`, `Keys`, `ContainsKey`, `TryGetValue<T>`).
  - Note: `TryGetValue<T>` returns `true` for value types even when the value is `default(T)`. For reference/nullable types it returns `false` when the deserialized value is `null`, preserving the “no null-check after `TryGetValue == true`” guarantee.
- `src/ArrowDbCore/ArrowDb.Upsert.cs`: `Upsert` overloads + optimistic concurrency via `updateCondition`.
  - Span-vs-string keys: `Upsert(ReadOnlySpan<char> ...)` uses `Lookup[...]`; this avoids allocating a new string when updating an existing key, but inserting a non-existing key may still allocate a new string key internally. Prefer the `string` overload when the key is already a `string`.
  - Null policy: `UpsertCore` returns `false` for `null` reference values (no-`null` design).
- `src/ArrowDbCore/ArrowDb.GetOrAdd.cs`: `GetOrAddAsync` helpers (string keys only); note the check-then-upsert is not atomic across threads (duplicate factory calls are possible under races).
- `src/ArrowDbCore/ArrowDb.Remove.cs`: `TryRemove(ReadOnlySpan<char>)`, `TryClear()`, and `Clear()` (obsolete; use `TryClear()`).
- `src/ArrowDbCore/ArrowDb.Serialization.cs`: `SerializeAsync()` and `RollbackAsync()` + the `WaitIfSerializing()` gate, conditional `PendingChanges` reset, and rollback epoch bump (`StateEpoch`).
- `src/ArrowDbCore/ArrowDbTransactionScope.cs`: transaction scope that defers serialization until disposed (supports both `IDisposable` and `IAsyncDisposable`).
- `src/ArrowDbCore/ArrowDb.IDictionaryAccessor.cs`: internal indirection used by `UpsertCore` to write via either `Source` (string keys) or `Lookup` (span keys).
- `src/ArrowDbCore/IDbSerializer.cs`: public serializer abstraction for persisting/loading the dictionary.
- `src/ArrowDbCore/Serializers/BaseFileSerializer.cs`: shared file serializer base (atomic write via `*.tmp` + `File.Move`, cross-process lock via named mutex).
- `src/ArrowDbCore/Serializers/FileSerializer.cs`: JSON file serializer (writes plain JSON).
- `src/ArrowDbCore/Serializers/AesFileSerializer.cs`: AES-encrypted JSON file serializer (wraps stream with `CryptoStream`).
- `src/ArrowDbCore/Serializers/InMemorySerializer.cs`: no-op serializer for purely in-memory databases.
- `src/ArrowDbCore/ChangeEventArgs.cs`: `ArrowDbChangeEventArgs` + `ArrowDbChangeType` used by `OnChange`.
- `src/ArrowDbCore/Extensions.cs`: internal helpers (currently used for SHA-256 hashing to derive mutex names).

## Repository layout

- `src/ArrowDbCore/`: main library (public API lives here).
- `tests/`:
  - `ArrowDbCore.Tests.Unit/`: unit tests (Microsoft Testing Platform + xUnit v3).
  - `ArrowDbCore.Tests.Unit.Isolated/`: unit tests intended to be runnable in isolation (Microsoft Testing Platform + xUnit v3).
  - `ArrowDbCore.Tests.Integrity/`: integrity tests (Microsoft Testing Platform + xUnit v3; may do heavier scenarios).
  - `ArrowDbCore.Tests.Analyzers/`: builds the library with trimming/AOT settings to catch issues early (not a test runner).
  - `ArrowDbCore.Tests.Common/`: shared test utilities.
- `benchmarks/`:
  - `ArrowDbCore.Benchmarks/`: main benchmarks (BenchmarkDotNet).
  - `ArrowDbCore.Benchmarks.VersionComparison/`: compares current code vs a referenced released package.

## Common commands (local + CI parity)

- Build: `dotnet build ArrowDbCore.slnx -c Release`
- Unit tests (CI matrix): `dotnet test tests/ArrowDbCore.Tests.Unit/ArrowDbCore.Tests.Unit.csproj -c Release` and `dotnet test tests/ArrowDbCore.Tests.Unit.Isolated/ArrowDbCore.Tests.Unit.Isolated.csproj -c Release`
- Integrity tests (CI): `dotnet test tests/ArrowDbCore.Tests.Integrity/ArrowDbCore.Tests.Integrity.csproj -c Release`
- AOT/trimming sanity build (CI): `dotnet build tests/ArrowDbCore.Tests.Analyzers/ArrowDbCore.Tests.Analyzers.csproj -c Release`
- Benchmarks: `dotnet run -c Release --project benchmarks/ArrowDbCore.Benchmarks/ArrowDbCore.Benchmarks.csproj`

## Code conventions and constraints

- Follow `.editorconfig` (notably: file-scoped namespaces; explicit types over `var`; and the repo prefers CRLF line endings).
- Avoid adding new NuGet dependencies to `src/ArrowDbCore/ArrowDbCore.csproj` unless explicitly requested (the library is intentionally dependency-free).
- Performance-first: avoid avoidable allocations; prefer `ReadOnlySpan<char>` APIs and the `Lookup` alternate lookup path; avoid introducing LINQ or other allocation-heavy patterns into hot paths.
- Preserve documented semantics (README):
  - Reference-type `null` values are rejected on `Upsert` (no-`null` policy).
  - Type safety is enforced via `JsonTypeInfo<T>`/`JsonSerializerContext`; do not introduce reflection-based serialization.
- If a change affects public API/behavior/versioning, confirm intent and then update `README.md`, `CHANGELOG.md`, and `src/ArrowDbCore/Readme.Nuget.md` as appropriate.
