# ArrowDb

A fast, lightweight, and type-safe key-value database designed for .NET.

* Super-Lightweight (dll size is <= 20KB - approximately 9X smaller than [UltraLiteDb](https://github.com/rejemy/UltraLiteDB))
* Ultra-Fast (1,000,000 random operations / ~100ms on M2 MacBook Pro)
* Minimal-Allocation (~2KB for serialization of 1,000,000 items)
* Thread-Safe and Concurrent
* ACID compliant on transaction level
* Type-Safe (no reflection - compile-time enforced via source-generated `JsonSerializerContext`)
* Cross-Platform and Fully AOT-compatible
* Super-Easy API near mirroring of `Dictionary<TKey, TValue>`

Information on usage can be found in the [README](https://github.com/dusrdev/ArrowDb/blob/stable/README.md).
