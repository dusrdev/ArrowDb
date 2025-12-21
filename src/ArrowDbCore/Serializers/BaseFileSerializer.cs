using System.Collections.Concurrent;

namespace ArrowDbCore.Serializers;

/// <summary>
/// Provides a base implementation for file-based serializers that ensures atomic and multi-process safe writes.
/// </summary>
public abstract class BaseFileSerializer : IDbSerializer {
    private readonly string _dbFilePath;
    private readonly string _tempFilePath;
    private readonly Mutex _mutex;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFileSerializer"/> class.
    /// </summary>
    /// <param name="path">The path to the database file.</param>
    protected BaseFileSerializer(string path) {
        _dbFilePath = Path.GetFullPath(path);
        _tempFilePath = $"{_dbFilePath}.tmp";
        string mutexName = $"Global\\ArrowDb-{Extensions.ToSHA256Hash(_dbFilePath)}";
        _mutex = new Mutex(false, mutexName);
    }

    /// <summary>
    /// Finalizer to ensure the system-wide mutex is released when the serializer is garbage collected.
    /// </summary>
    ~BaseFileSerializer() {
        _mutex.Dispose();
    }

    /// <inheritdoc />
    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync() {
        if (!File.Exists(_dbFilePath) || new FileInfo(_dbFilePath).Length == 0) {
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
        }

        _mutex.WaitOne();
        try {
            using var fileStream = File.OpenRead(_dbFilePath);
            return DeserializeData(fileStream);
        } finally {
            _mutex.ReleaseMutex();
        }
    }

    /// <inheritdoc />
    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data) {
        _mutex.WaitOne();
        try {
            using (var fileStream = File.Create(_tempFilePath)) {
                SerializeData(fileStream, data);
            }
            File.Move(_tempFilePath, _dbFilePath, true);
        } finally {
            _mutex.ReleaseMutex();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// When overridden in a derived class, serializes the data to the provided stream.
    /// </summary>
    /// <param name="stream">The stream to write the data to.</param>
    /// <param name="data">The data to serialize.</param>
    protected abstract void SerializeData(Stream stream, ConcurrentDictionary<string, byte[]> data);

    /// <summary>
    /// When overridden in a derived class, deserializes the data from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the data from.</param>
    /// <returns>The deserialized dictionary.</returns>
    protected abstract ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeData(Stream stream);
}