using System.Collections.Concurrent;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

namespace ArrowDbCore.Serializers;

/// <summary>
/// Provides a base implementation for file-based serializers that ensures atomic writes
/// and single-owner writable semantics for the underlying database file.
/// </summary>
public abstract class BaseFileSerializer : IDbSerializer {
    private static readonly FileStreamOptions ReadStreamOptions = new() {
        Access = FileAccess.Read,
        Mode = FileMode.Open,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        Share = FileShare.Read,
    };

    private static readonly FileStreamOptions WriteStreamOptions = new() {
        Access = FileAccess.Write,
        Mode = FileMode.Create,
        Options = FileOptions.Asynchronous,
        Share = FileShare.None,
    };

    private readonly string _dbFilePath;
    private readonly SafeFileHandle? _ownershipHandle;
    private string? _lastTempFilePath;
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
        Dispose(disposing: false);
    }

    /// <inheritdoc />
    public async ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        try {
            await using FileStream fileStream = new(_dbFilePath, ReadStreamOptions);
            if (fileStream.Length == 0) {
                return new ConcurrentDictionary<string, byte[]>();
            }

            return await DeserializeDataAsync(fileStream, cancellationToken);
        } catch (FileNotFoundException) {
            return new ConcurrentDictionary<string, byte[]>();
        }
    }

    /// <inheritdoc />
    public async ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        string tempFilePath = GenerateTempFilePath();
        try {
            await using (FileStream fileStream = new(tempFilePath, WriteStreamOptions)) {
                await SerializeDataAsync(fileStream, data, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            }

            File.Move(tempFilePath, _dbFilePath, true);
        } finally {
            TryDeleteFile(tempFilePath);
        }
    }

    /// <summary>
    /// When overridden in a derived class, serializes the data to the provided stream.
    /// </summary>
    /// <param name="stream">The stream to write the data to.</param>
    /// <param name="data">The data to serialize.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    protected abstract ValueTask SerializeDataAsync(Stream stream, ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken);

    /// <summary>
    /// When overridden in a derived class, deserializes the data from the provided stream.
    /// </summary>
    /// <param name="stream">The stream to read the data from.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The deserialized dictionary.</returns>
    protected abstract ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeDataAsync(Stream stream, CancellationToken cancellationToken);

    /// <inheritdoc />
    public bool IsDisposed => _disposed;

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() {
        if (_disposed) {
            return ValueTask.CompletedTask;
        }

        Dispose(disposing: true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Releases serializer resources.
    /// </summary>
    /// <param name="disposing">Indicates whether disposal was triggered explicitly.</param>
    protected virtual void Dispose(bool disposing) {
        if (_disposed) {
            return;
        }

        _ownershipHandle?.Dispose();
        _disposed = true;
    }

    private static SafeFileHandle AcquireOwnershipHandle(string dbFilePath) {
        string lockFilePath = $"{dbFilePath}.lock";
        try {
            return File.OpenHandle(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        } catch (IOException ex) {
            throw new ArrowDbOwnershipException($"The database file '{dbFilePath}' is already owned by another process.", ex);
        }
    }

    private string GenerateTempFilePath() {
        string? lastTempFilePath = _lastTempFilePath;
        string tempFilePath;
        do {
            tempFilePath = $"{_dbFilePath}.{RandomNumberGenerator.GetHexString(4)}.tmp";
        } while (string.Equals(tempFilePath, lastTempFilePath, StringComparison.Ordinal));
        _lastTempFilePath = tempFilePath;
        return tempFilePath;
    }

    private static void TryDeleteFile(string path) {
        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch (IOException) {
        } catch (UnauthorizedAccessException) {
        }
    }
}
