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
}
