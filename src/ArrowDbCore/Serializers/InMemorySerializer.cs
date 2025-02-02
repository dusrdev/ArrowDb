using System.Collections.Concurrent;


namespace ArrowDbCore.Serializers;

/// <summary>
/// An in-memory serializer (does nothing)
/// </summary>
public sealed class InMemorySerializer : IDbSerializer {
    /// <summary>
    /// Returns an empty dictionary
    /// </summary>
    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync() => ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());

    /// <summary>
    /// Does nothing
    /// </summary>
    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data) => ValueTask.CompletedTask;
}
