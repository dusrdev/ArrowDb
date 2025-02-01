using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Upsert the specified key with the specified value into the database
	/// </summary>
	/// <typeparam name="TValue">The type of the value to upsert</typeparam>
	/// <param name="key">The key at which to upsert the value</param>
	/// <param name="value">The value to upsert</param>
	/// <param name="jsonTypeInfo">The json type info for the value type</param>
	/// <returns>True</returns>
	public bool Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo) {
		WaitIfSerializing(); // block if the database is currently serializing
		byte[] utf8value = JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
		Lookup[key] = utf8value;
		OnChangeInternal(ArrowDbChangeEventArgs.Upsert); // trigger change event
		return true;
	}

	/// <summary>
	/// Tries to upsert the specified key with the specified value into the database
	/// </summary>
	/// <typeparam name="TValue">The type of the value to upsert</typeparam>
	/// <param name="key">The key at which to upsert the value</param>
	/// <param name="value">The value to upsert</param>
	/// <param name="jsonTypeInfo">The json type info for the value type</param>
	/// <param name="updateCondition">A conditional check that determines whether this update should be performed</param>
	/// <returns>True if the value was upserted, false otherwise</returns>
	/// <remarks>
	/// <para>
	/// <paramref name="updateCondition"/> can be used to resolve write conflicts, the update will be rejected only if all of the following conditions are met:
	/// </para>
	/// <para>1. <paramref name="updateCondition"/> is not null</para>
	/// <para>2. A value for the specified key exists and successfully deserialized to <typeparamref name="TValue"/></para>
	/// <para>3. <paramref name="updateCondition"/> on the reference value returns false</para>
	/// </remarks>
	public bool Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, bool> updateCondition) {
		if (TryGetValue(key, jsonTypeInfo, out TValue existingReference) &&
			 !updateCondition(existingReference)) {
			return false;
		}
		return Upsert(key, value, jsonTypeInfo);
	}
}
