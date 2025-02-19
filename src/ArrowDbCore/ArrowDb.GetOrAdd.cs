using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Tries to retrieve a value stored in the database under <paramref name="key"/>, if doesn't exist, it uses the factory to create and add it, then returns it.
	/// </summary>
	/// <typeparam name="TValue">The type of the value to get or add</typeparam>
	/// <param name="key">The key at which to find or add the value</param>
	/// <param name="jsonTypeInfo">The json type info for the value type</param>
	/// <param name="factory">The function used to generate a value for the key</param>
	/// <returns>The value after finding or adding it</returns>
	/// <remarks>
	/// </remarks>
	public async ValueTask<TValue> GetOrAddAsync<TValue>(string key, JsonTypeInfo<TValue> jsonTypeInfo, Func<string, ValueTask<TValue>> factory) {
		if (Lookup.TryGetValue(key, out var source)) {
			return JsonSerializer.Deserialize(new ReadOnlySpan<byte>(source), jsonTypeInfo)!;
		}
		var val = await factory(key);
		Upsert(key, val, jsonTypeInfo);
		return val;
	}
}
