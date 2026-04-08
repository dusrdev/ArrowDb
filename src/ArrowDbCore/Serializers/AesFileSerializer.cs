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
    protected override async ValueTask SerializeDataAsync(Stream stream, ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken) {
        using var encryptor = _aes.CreateEncryptor();
        await using var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write, leaveOpen: true);
        await JsonSerializer.SerializeAsync(cryptoStream, data, _jsonTypeInfo, cancellationToken);
    }

    /// <inheritdoc />
    protected override async ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeDataAsync(Stream stream, CancellationToken cancellationToken) {
        using var decryptor = _aes.CreateDecryptor();
        await using var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read, leaveOpen: true);
        ConcurrentDictionary<string, byte[]>? result = await JsonSerializer.DeserializeAsync(cryptoStream, _jsonTypeInfo, cancellationToken);
        return result ?? new ConcurrentDictionary<string, byte[]>();
    }
}
