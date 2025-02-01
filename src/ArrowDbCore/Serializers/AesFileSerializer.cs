using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore.Serializers;

/// <summary>
/// An <see cref="Aes"/> managed file/disk backed serializer
/// </summary>
public sealed class AesFileSerializer : IDbSerializer {
	/// <summary>
	/// The path to the file
	/// </summary>
	private readonly string _path;

	/// <summary>
	/// The Aes instance
	/// </summary>
    private readonly Aes _aes;

	/// <summary>
    /// The json type info for the dictionary
    /// </summary>
    private readonly JsonTypeInfo<ConcurrentDictionary<string, byte[]>> _jsonTypeInfo;

	/// <summary>
    /// Initializes a new instance of the <see cref="AesFileSerializer"/> class.
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <param name="aes">The <see cref="Aes"/> instance to use</param>
	/// <param name="jsonTypeInfo">The json type info for the dictionary</param>
    public AesFileSerializer(string path, Aes aes, JsonTypeInfo<ConcurrentDictionary<string, byte[]>> jsonTypeInfo) {
        _path = path;
		_aes = aes;
		_jsonTypeInfo = jsonTypeInfo;
    }

	/// <inheritdoc />
    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync() {
        if (!File.Exists(_path) || new FileInfo(_path).Length == 0) {
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
        }
		using var fileStream = File.OpenRead(_path);
		using var decryptor = _aes.CreateDecryptor();
		using var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
		var res = JsonSerializer.Deserialize(cryptoStream, _jsonTypeInfo);
		return ValueTask.FromResult(res ?? new ConcurrentDictionary<string, byte[]>());
    }

	/// <inheritdoc />
    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data) {
        using var fileStream = File.Create(_path);
		using var encryptor = _aes.CreateEncryptor();
		using var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write);
		JsonSerializer.Serialize(cryptoStream, data, _jsonTypeInfo);
		return ValueTask.CompletedTask;
    }
}