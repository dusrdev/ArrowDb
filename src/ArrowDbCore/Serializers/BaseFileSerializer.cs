using System.Collections.Concurrent;
using Microsoft.Win32.SafeHandles;

namespace ArrowDbCore.Serializers;

/// <summary>
/// Provides a base implementation for file-based serializers that ensures atomic writes
/// and single-owner writable semantics for the underlying database file.
/// </summary>
public abstract class BaseFileSerializer : IDbSerializer, IDisposable {
    private readonly string _dbFilePath;
    private readonly SafeFileHandle? _ownershipHandle;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFileSerializer"/> class.
    /// </summary>
    /// <param name="path">The path to the database file.</param>
    /// <exception cref="ArrowDbOwnershipException">
    /// Thrown when another ArrowDb process already owns the same file-backed database path.
    /// </exception>
    protected BaseFileSerializer(string path) {
        _dbFilePath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(_dbFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
            throw new DirectoryNotFoundException($"The directory '{directory}' does not exist.");
        }

        _ownershipHandle = AcquireOwnershipHandle(_dbFilePath);
    }

    /// <summary>
    /// Finalizer to ensure the ownership handle is released when the serializer is garbage collected.
    /// </summary>
    ~BaseFileSerializer() {
        try {
            _ownershipHandle?.Dispose();
        } catch {
            // Finalizers must never throw.
        }
    }

    /// <inheritdoc />
    public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default) {
        if (!File.Exists(_dbFilePath) || new FileInfo(_dbFilePath).Length == 0) {
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
        }

        using var fileStream = File.OpenRead(_dbFilePath);
        return DeserializeData(fileStream);
    }

    /// <inheritdoc />
    public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default) {
        string tempFilePath = $"{_dbFilePath}.{Guid.NewGuid():N}.tmp";
        try {
            using (var fileStream = File.Create(tempFilePath)) {
                SerializeData(fileStream, data);
            }

            File.Move(tempFilePath, _dbFilePath, true);
        } finally {
            if (File.Exists(tempFilePath)) {
                File.Delete(tempFilePath);
            }
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

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) return;

        _ownershipHandle?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static SafeFileHandle AcquireOwnershipHandle(string dbFilePath) {
        string lockFilePath = $"{dbFilePath}.lock";
        try {
            return File.OpenHandle(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        } catch (IOException ex) {
            throw new ArrowDbOwnershipException($"The database file '{dbFilePath}' is already owned by another process.", ex);
        }
    }
}
