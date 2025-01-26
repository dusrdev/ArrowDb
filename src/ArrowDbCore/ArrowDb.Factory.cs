namespace ArrowDbCore;

public partial class ArrowDb {
	/// <summary>
	/// Initializes a file/disk backed database at the specified path
	/// </summary>
	/// <param name="path">The path that the file that backs the database</param>
	/// <returns>A database instance</returns>
	public static async ValueTask<ArrowDb> CreateFromFile(string path) {
		var serializer = new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);
		var data = await serializer.DeserializeAsync();
		return new ArrowDb(data, serializer);
	}

	/// <summary>
	/// Initializes an in-memory database
	/// </summary>
	/// <returns>A database instance</returns>
	public static async ValueTask<ArrowDb> CreateInMemory() {
		var serializer = new InMemorySerializer();
		var data = await serializer.DeserializeAsync();
		return new ArrowDb(data, serializer);
	}

	/// <summary>
	/// Initializes a database with a custom <see cref="IDbSerializer"/> implementation
	/// </summary>
	/// <param name="serializer">A custom <see cref="IDbSerializer"/> implementation</param>
	/// <returns>A database instance</returns>
	public static async ValueTask<ArrowDb> CreateCustom(IDbSerializer serializer) {
		var data = await serializer.DeserializeAsync();
		return new ArrowDb(data, serializer);
	}

	/// <summary>
	/// Generates a typed key for the specified specific key in a very efficient manner
	/// </summary>
	/// <typeparam name="T">The type of the value</typeparam>
	/// <param name="specificKey">The key that is specific to the value</param>
	/// <param name="buffer">The buffer to use for the generation</param>
	/// <returns>
	/// A key that is formatted as "<typeparamref name="T"/>:<paramref name="specificKey"/>"
	/// </returns>
	public static ReadOnlySpan<char> GenerateTypedKey<T>(ReadOnlySpan<char> specificKey, Span<char> buffer) {
		var typeName = TypeNameCache<T>.TypeName;
		var length = typeName.Length + 1 + specificKey.Length; // type:specificKey
		ArgumentOutOfRangeException.ThrowIfGreaterThan(length, buffer.Length);
        typeName.CopyTo(buffer);
        buffer[typeName.Length] = ':';
        specificKey.CopyTo(buffer.Slice(typeName.Length + 1));
        return buffer.Slice(0, length);
	}

	// A static class that caches type names during runtime
	private static class TypeNameCache<T> {
		/// <summary>
		/// The name of the type of T
		/// </summary>
		public static readonly string TypeName = typeof(T).Name;
	}
}
