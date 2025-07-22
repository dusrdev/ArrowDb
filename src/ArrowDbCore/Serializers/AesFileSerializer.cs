using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore.Serializers;

/// <summary>
/// An <see cref="Aes"/> managed file/disk backed serializer.
/// </summary>
public sealed class AesFileSerializer : BaseFileSerializer {
    private readonly Aes _aes;
    private readonly JsonTypeInfo<ConcurrentDictionary<string, byte[]>> _jsonTypeInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesFileSerializer"/> class.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="aes">The <see cref="Aes"/> instance to use.</param>
    /// <param name="jsonTypeInfo">The json type info for the dictionary.</param>
    public AesFileSerializer(string path, Aes aes, JsonTypeInfo<ConcurrentDictionary<string, byte[]>> jsonTypeInfo)
        : base(path) {
        _aes = aes;
        _jsonTypeInfo = jsonTypeInfo;
    }

    /// <inheritdoc />
    protected override void SerializeData(Stream stream, ConcurrentDictionary<string, byte[]> data) {
        using var encryptor = _aes.CreateEncryptor();
        using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write);
        JsonSerializer.Serialize(cryptoStream, data, _jsonTypeInfo);
    }

    /// <inheritdoc />
    protected override ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeData(Stream stream) {
        using var decryptor = _aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
        var res = JsonSerializer.Deserialize(cryptoStream, _jsonTypeInfo);
        return ValueTask.FromResult(res ?? new ConcurrentDictionary<string, byte[]>());
    }
}
