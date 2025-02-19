# Changelog (Sorted by Date in Descending Order)

## 1.3.0.0

* Added overloads of `Upsert` that accept a `string` key, while the `ReadOnlySpan<char>` overloads are amazing in specific cases where its use can prevent a string allocation for the lookup, in other places where the input was originally a `string` that was implicitly converted to a `ReadOnlySpan<char>` for the parameter, this would've caused a copy to be allocated for the key when the key did not exist. The same scenario will now use the `string` overload and use it for the key directly, avoiding the intermediate copy.
* Added a `ValueTask` based `GetOrAddAsync` method, commonly used in caching scenarios.

## 1.2.0.0

* An overload to `Upsert` without `updateCondition` was added and would now act as default path in case `updateCondition` wasn't specified, this should further optimize such cases by removing condition checks and another reference from the stack during runtime.
* Internal methods which are rather small and frequently invoked will now be prioritized for inlining by JIT, this should slightly improve perf, especially in NativeAot.
* Added a new factory initializer `CreateFromFileWithAes` that received an `Aes` instance as parameter. It will then use it to encrypt and decrypt the output and input during serialization and deserialization respectively.

## 1.1.0.0

* Fixed issue with `FileSerializer` where serialization would write over existing file data which could create invalid tokens, causing deserialization to fail.
* Added static `ArrowDb.GenerateTypedKey<T>` method that accepts the type of the value, specific key (identifier) and a buffer, it returns a `ReadOnlySpan<char>` key that prefixes the type to the specific key.

## 1.0.0.0

* Initial Release
