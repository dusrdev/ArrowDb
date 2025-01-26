using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArrowDbCore;

/// <summary>
/// A file/disk backed serializer
/// </summary>
public class FileSerializer : IDbSerializer {
    /// <summary>
    /// The path to the file
    /// </summary>
    private readonly string _path;

    /// <summary>
    /// The json type info for the dictionary
    /// </summary>
    private readonly JsonTypeInfo<ConcurrentDictionary<string, byte[]>> _jsonTypeInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSerializer"/> class.
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <param name="jsonTypeInfo">The json type info for the dictionary</param>
    public FileSerializer(string path, JsonTypeInfo<ConcurrentDictionary<string, byte[]>> jsonTypeInfo) {
        _path = path;
        _jsonTypeInfo = jsonTypeInfo;
    }

    /// <inheritdoc />
    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync() {
        if (!File.Exists(_path) || new FileInfo(_path).Length == 0) {
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
        }
        using var file = File.OpenRead(_path);
        var result = JsonSerializer.Deserialize(file, _jsonTypeInfo) ?? new();
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data) {
        using var file = File.Create(_path);
        JsonSerializer.Serialize(file, data, _jsonTypeInfo);
        return ValueTask.CompletedTask;
    }
}
