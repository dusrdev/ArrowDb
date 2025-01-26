# Changelog (Sorted by Date in Descending Order)

## 1.1.0.0

* Fixed issue with `FileSerializer` where serialization would write over existing file data which could create invalid tokens, causing deserialization to fail.
* Added static `ArrowDb.GenerateTypedKey<T>` method that accepts the type of the value, specific key (identifier) and a buffer, it returns a `ReadOnlySpan<char>` key that prefixes the type to the specific key.

## 1.0.0.0

* Initial Release
