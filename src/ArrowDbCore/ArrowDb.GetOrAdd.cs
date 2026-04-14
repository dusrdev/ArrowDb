using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore;

public partial class ArrowDb
{
    /// <summary>
    /// Tries to retrieve a value stored in the database under <paramref name="key"/>, if it doesn't exist, it uses the factory to create and add it, then returns it.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to get or add</typeparam>
    /// <param name="key">The key at which to find or add the value</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <param name="valueFactory">The function used to generate a value for the key</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The value after finding or adding it</returns>
    /// <remarks>
    /// <para>
    /// This method is intentionally not atomic: under concurrent callers, <paramref name="valueFactory"/> may be invoked multiple times for the same <paramref name="key"/>.
    /// The returned value is always the value produced by this invocation of <paramref name="valueFactory"/>.
    /// The final stored value is last-writer-wins, since it is persisted via <see cref="Upsert{TValue}(string,TValue,JsonTypeInfo{TValue})"/>.
    /// </para>
    /// <para>
    /// If you need single-invocation semantics for <paramref name="valueFactory"/> (e.g. the factory has side-effects or is expensive), guard the call site with a keyed lock.
    /// </para>
    /// </remarks>
    public async ValueTask<TValue> GetOrAddAsync<TValue>(string key, JsonTypeInfo<TValue> jsonTypeInfo, Func<string, CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default)
    {
        if (Lookup.TryGetValue(key, out var source))
        {
            return JsonSerializer.Deserialize(new ReadOnlySpan<byte>(source), jsonTypeInfo)!;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var val = await valueFactory(key, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Upsert(key, val, jsonTypeInfo);
        return val;
    }

    /// <summary>
    /// Tries to retrieve a value stored in the database under <paramref name="key"/>, if it doesn't exist, it uses the factory to create and add it, then returns it.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to get or add</typeparam>
    /// <typeparam name="TArg">The type of the argument for the updateCondition function</typeparam>
    /// <param name="key">The key at which to find or add the value</param>
    /// <param name="jsonTypeInfo">The json type info for the value type</param>
    /// <param name="valueFactory">The function used to generate a value for the key</param>
    /// <param name="factoryArgument">An argument that could be provided to the valueFactory function to avoid a closure</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The value after finding or adding it</returns>
    /// <remarks>
    /// <para>
    /// This method is intentionally not atomic: under concurrent callers, <paramref name="valueFactory"/> may be invoked multiple times for the same <paramref name="key"/>.
    /// The returned value is always the value produced by this invocation of <paramref name="valueFactory"/>.
    /// The final stored value is last-writer-wins, since it is persisted via <see cref="Upsert{TValue}(string,TValue,JsonTypeInfo{TValue})"/>.
    /// </para>
    /// <para>
    /// If you need single-invocation semantics for <paramref name="valueFactory"/> (e.g. the factory has side-effects or is expensive), guard the call site with a keyed lock.
    /// </para>
    /// </remarks>
    public async ValueTask<TValue> GetOrAddAsync<TValue, TArg>(string key, JsonTypeInfo<TValue> jsonTypeInfo, Func<string, TArg, CancellationToken, ValueTask<TValue>> valueFactory, TArg factoryArgument, CancellationToken cancellationToken = default)
    {
        if (Lookup.TryGetValue(key, out var source))
        {
            return JsonSerializer.Deserialize(new ReadOnlySpan<byte>(source), jsonTypeInfo)!;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var val = await valueFactory(key, factoryArgument, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Upsert(key, val, jsonTypeInfo);
        return val;
    }
}
