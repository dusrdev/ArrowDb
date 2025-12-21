# ArrowDb

A fast, lightweight, and type-safe key-value database designed for .NET.

* Super-Lightweight (dll size is ~19KB - approximately 9X smaller than [UltraLiteDb](https://github.com/rejemy/UltraLiteDB))
* Ultra-Fast (1,000,000 random operations / ~98ms on M2 MacBook Pro)
* Minimal-Allocation (constant ~520 bytes for serialization any db size)
* Thread-Safe and Concurrent
* ACID compliant on transaction level
* Type-Safe (no reflection - compile-time enforced via source-generated `JsonSerializerContext`)
* Cross-Platform and Fully AOT-compatible
* Super-Easy API near mirroring of `Dictionary<TKey, TValue>`

## A Note on `null` Values

`ArrowDb` enforces a "no `null`s" policy by design. Attempting to `Upsert` a `null` value will be rejected and return `false`. This simplifies the developer experience by guaranteeing that if a key exists, its value is never `null`. This eliminates the need for null-checking after retrieval, leading to cleaner and more predictable application code.

This policy does not affect value types (`structs`); their `default` values (e.g., `0` for an `int`) are considered valid.

Information on usage can be found in the [README](https://github.com/dusrdev/ArrowDb/blob/stable/README.md).

## Concurrency note: `GetOrAddAsync`

`GetOrAddAsync` is intentionally **not atomic**. Under concurrency, the factory may be invoked multiple times for the same key, and the final stored value is last-writer-wins (because the value is persisted via `Upsert`). If you need single-invocation semantics for the factory (e.g. side-effects/expensive work), guard the call site with a keyed lock.
