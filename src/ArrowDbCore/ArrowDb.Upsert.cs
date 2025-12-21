using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore;

public partial class ArrowDb {
    /// <summary>
    /// Upsert the specified key with the specified value into the database.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to upsert</typeparam>
    /// <param name="key">The key at which to upsert the value</param>
    /// <param name="value">The value to upsert. This cannot be null.</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <returns>True if the value was upserted, false if the provided value was null.</returns>
    public bool Upsert<TValue>(string key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo) {
        return UpsertCore<string, TValue, StringAccessor>(key, value, jsonTypeInfo, default);
    }

    /// <summary>
    /// Upsert the specified key with the specified value into the database.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to upsert</typeparam>
    /// <param name="key">The key at which to upsert the value</param>
    /// <param name="value">The value to upsert. This cannot be null.</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <returns>True if the value was upserted, false if the provided value was null.</returns>
    /// <remarks>
    /// This method overload which uses ReadOnlySpan{char} will not allocate a new string for the key if it already exists, instead it will directly replace the value.
    /// </remarks>
    public bool Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo) {
        return UpsertCore<ReadOnlySpan<char>, TValue, ReadOnlySpanAccessor>(key, value, jsonTypeInfo, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool UpsertCore<TKey, TValue, TAccessor>(TKey key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, TAccessor accessor)
    where TKey : allows ref struct
    where TAccessor : IDictionaryAccessor<TKey>, allows ref struct {
        if (value is null) {
            return false;
        }
        var observedEpoch = Volatile.Read(ref StateEpoch);
        WaitIfSerializing(); // Block if serializing
        byte[] utf8Value = JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
        accessor.Upsert(this, key, utf8Value);
        OnChangeInternal(ArrowDbChangeEventArgs.Upsert); // Trigger change event
        return Volatile.Read(ref StateEpoch) == observedEpoch;
    }

    /// <summary>
    /// Tries to upsert the specified key with the specified value into the database.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to upsert</typeparam>
    /// <param name="key">The key at which to upsert the value</param>
    /// <param name="value">The value to upsert. This cannot be null.</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <param name="updateCondition">A conditional check that determines whether this update should be performed</param>
    /// <returns>True if the value was upserted, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The operation will be rejected if the provided <paramref name="value"/> is null.
    /// </para>
    /// <para>
    /// <paramref name="updateCondition"/> can be used to resolve write conflicts, the update will be rejected only if both conditions are met:
    /// </para>
    /// <para>1. A value for the specified key exists and successfully deserialized to <typeparamref name="TValue"/></para>
    /// <para>2. <paramref name="updateCondition"/> on the reference value returns false</para>
    /// </remarks>
    public bool Upsert<TValue>(string key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, bool> updateCondition) {
        if (TryGetValue(key, jsonTypeInfo, out TValue existingReference) &&
             !updateCondition(existingReference)) {
            return false;
        }
        return Upsert(key, value, jsonTypeInfo);
    }

    /// <summary>
    /// Tries to upsert the specified key with the specified value into the database.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to upsert</typeparam>
    /// <typeparam name="TArg">The type of the argument for the updateCondition function</typeparam>
    /// <param name="key">The key at which to upsert the value</param>
    /// <param name="value">The value to upsert. This cannot be null.</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <param name="updateCondition">A conditional check that determines whether this update should be performed</param>
    /// <param name="updateConditionArgument">An argument that could be provided to the updateCondition function to avoid a closure</param>
    /// <returns>True if the value was upserted, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The operation will be rejected if the provided <paramref name="value"/> is null.
    /// </para>
    /// <para>
    /// <paramref name="updateCondition"/> can be used to resolve write conflicts, the update will be rejected only if both conditions are met:
    /// </para>
    /// <para>1. A value for the specified key exists and successfully deserialized to <typeparamref name="TValue"/></para>
    /// <para>2. <paramref name="updateCondition"/> on the reference value returns false</para>
    /// </remarks>
    public bool Upsert<TValue, TArg>(string key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, TArg, bool> updateCondition, TArg updateConditionArgument) {
        if (TryGetValue(key, jsonTypeInfo, out TValue existingReference) &&
             !updateCondition(existingReference, updateConditionArgument)) {
            return false;
        }
        return Upsert(key, value, jsonTypeInfo);
    }

    /// <summary>
    /// Tries to upsert the specified key with the specified value into the database.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to upsert</typeparam>
    /// <param name="key">The key at which to upsert the value</param>
    /// <param name="value">The value to upsert. This cannot be null.</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <param name="updateCondition">A conditional check that determines whether this update should be performed</param>
    /// <returns>True if the value was upserted, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The operation will be rejected if the provided <paramref name="value"/> is null.
    /// </para>
    /// <para>
    /// <paramref name="updateCondition"/> can be used to resolve write conflicts, the update will be rejected only if both conditions are met:
    /// </para>
    /// <para>1. A value for the specified key exists and successfully deserialized to <typeparamref name="TValue"/></para>
    /// <para>2. <paramref name="updateCondition"/> on the reference value returns false</para>
    /// <para>
    /// This method overload which uses ReadOnlySpan{char} will not allocate a new string for the key if it already exists, instead it will directly replace the value
    /// </para>
    /// </remarks>
    public bool Upsert<TValue>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, bool> updateCondition) {
        if (TryGetValue(key, jsonTypeInfo, out TValue existingReference) &&
             !updateCondition(existingReference)) {
            return false;
        }
        return Upsert(key, value, jsonTypeInfo);
    }

    /// <summary>
    /// Tries to upsert the specified key with the specified value into the database.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to upsert</typeparam>
    /// <typeparam name="TArg">The type of the argument for the updateCondition function</typeparam>
    /// <param name="key">The key at which to upsert the value</param>
    /// <param name="value">The value to upsert. This cannot be null.</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <param name="updateCondition">A conditional check that determines whether this update should be performed</param>
    /// <param name="updateConditionArgument">An argument that could be provided to the updateCondition function to avoid a closure</param>
    /// <returns>True if the value was upserted, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The operation will be rejected if the provided <paramref name="value"/> is null.
    /// </para>
    /// <para>
    /// <paramref name="updateCondition"/> can be used to resolve write conflicts, the update will be rejected only if both conditions are met:
    /// </para>
    /// <para>1. A value for the specified key exists and successfully deserialized to <typeparamref name="TValue"/></para>
    /// <para>2. <paramref name="updateCondition"/> on the reference value returns false</para>
    /// <para>
    /// This method overload which uses ReadOnlySpan{char} will not allocate a new string for the key if it already exists, instead it will directly replace the value
    /// </para>
    /// </remarks>
    public bool Upsert<TValue, TArg>(ReadOnlySpan<char> key, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, Func<TValue, TArg, bool> updateCondition, TArg updateConditionArgument) {
        if (TryGetValue(key, jsonTypeInfo, out TValue existingReference) &&
             !updateCondition(existingReference, updateConditionArgument)) {
            return false;
        }
        return Upsert(key, value, jsonTypeInfo);
    }
}