using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore.Serializers;

/// <summary>
/// A file/disk backed serializer using JSON.
/// </summary>
public class FileSerializer : BaseFileSerializer {
    private readonly JsonTypeInfo<ConcurrentDictionary<string, byte[]>> _jsonTypeInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSerializer"/> class.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="jsonTypeInfo">The json type info for the dictionary.</param>
    public FileSerializer(string path, JsonTypeInfo<ConcurrentDictionary<string, byte[]>> jsonTypeInfo)
        : base(path) {
        _jsonTypeInfo = jsonTypeInfo;
    }

    /// <inheritdoc />
    protected override void SerializeData(Stream stream, ConcurrentDictionary<string, byte[]> data) {
        JsonSerializer.Serialize(stream, data, _jsonTypeInfo);
    }

    /// <inheritdoc />
    protected override ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeData(Stream stream) {
        var result = JsonSerializer.Deserialize(stream, _jsonTypeInfo) ?? new();
        return ValueTask.FromResult(result);
    }
}