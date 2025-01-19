using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Returns the number of entries in the database
	/// </summary>
	public int Count => Source.Count;

	/// <summary>
	/// Checks if the database contains the specified key
	/// </summary>
	/// <param name="key">The key to search for</param>
	/// <returns>True if the key exists, false otherwise</returns>
	public bool ContainsKey(ReadOnlySpan<char> key) => Lookup.ContainsKey(key);

	/// <summary>
	/// Tries to read and parse a value of the database
	/// </summary>
	/// <typeparam name="TValue">The type of the value to read</typeparam>
	/// <param name="key">The key to search for</param>
	/// <param name="jsonTypeInfo">The json type info for the value type</param>
	/// <param name="value">The resulting value</param>
	/// <returns>True if the value exists and was parsed successfully, false otherwise</returns>
	public bool TryGetValue<TValue>(ReadOnlySpan<char> key, JsonTypeInfo<TValue> jsonTypeInfo, out TValue value) {
		if (!Lookup.TryGetValue(key, out byte[]? existingReference)) {
			value = default!;
			return false;
		}
		value = JsonSerializer.Deserialize(existingReference.AsSpan(), jsonTypeInfo)!;
		return !EqualityComparer<TValue>.Default.Equals(value, default);
	}

	/// <summary>
	/// Returns a read-only collection of the database keys
	/// </summary>
	public ICollection<string> Keys => Source.Keys;
}
