using System.Collections.Concurrent;

namespace ArrowDbCore;

/// <summary>
/// The interface that defines a serializer for ArrowDb
/// </summary>
public interface IDbSerializer {
	/// <summary>
	/// Deserializes the database from the underlying storage
	/// </summary>
	ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync();

	/// <summary>
	/// Serializes the database to the underlying storage
	/// </summary>
	/// <param name="data">The data to serialize</param>
	ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data);
}
