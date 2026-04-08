using System.Collections.Concurrent;
using System.Text;

using ArrowDbCore.Serializers;

namespace ArrowDbCore.Tests.Unit;

public sealed class FileSerializerAsync {
    [Fact]
    public async Task BaseFileSerializer_SerializeAsync_WhenCanceledBeforeCommit_LeavesOriginalFileAndDeletesTemp() {
        string path = Path.GetTempFileName();
        AsyncTrackingFileSerializer? serializer = null;

        try {
            await File.WriteAllTextAsync(path, "original", TestContext.Current.CancellationToken);

            serializer = new AsyncTrackingFileSerializer(path) {
                BlockSerialize = true,
            };

            using var cancellationTokenSource = new CancellationTokenSource();
            Task serializeTask = serializer.SerializeAsync(new ConcurrentDictionary<string, byte[]>(), cancellationTokenSource.Token).AsTask();

            await serializer.SerializeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            string? tempFilePath = serializer.SerializeStreamPaths.SingleOrDefault();

            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serializeTask);
            Assert.Equal("original", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.NotNull(tempFilePath);
            Assert.False(File.Exists(tempFilePath));
        } finally {
            serializer?.Dispose();
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task BaseFileSerializer_DeserializeAsync_WhenCanceled_ThrowsAndLeavesFileUnchanged() {
        string path = Path.GetTempFileName();
        AsyncTrackingFileSerializer? serializer = null;

        try {
            await File.WriteAllTextAsync(path, "existing", TestContext.Current.CancellationToken);

            serializer = new AsyncTrackingFileSerializer(path) {
                BlockDeserialize = true,
            };

            using var cancellationTokenSource = new CancellationTokenSource();
            Task deserializeTask = serializer.DeserializeAsync(cancellationTokenSource.Token).AsTask();

            await serializer.DeserializeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => deserializeTask);
            Assert.Equal("existing", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        } finally {
            serializer?.Dispose();
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task BaseFileSerializer_SerializeAsync_UsesUniqueTempFilePerWrite() {
        string path = Path.GetTempFileName();
        AsyncTrackingFileSerializer? serializer = null;

        try {
            serializer = new AsyncTrackingFileSerializer(path);

            await serializer.SerializeAsync(new ConcurrentDictionary<string, byte[]>());
            await serializer.SerializeAsync(new ConcurrentDictionary<string, byte[]>());

            Assert.Equal(2, serializer.SerializeStreamPaths.Count);
            Assert.NotEqual(serializer.SerializeStreamPaths[0], serializer.SerializeStreamPaths[1]);
            Assert.All(serializer.SerializeStreamPaths, tempFilePath => {
                Assert.StartsWith($"{path}.", tempFilePath, StringComparison.Ordinal);
                Assert.EndsWith(".tmp", tempFilePath, StringComparison.Ordinal);
                Assert.NotEqual(path, tempFilePath);
            });
        } finally {
            serializer?.Dispose();
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    private sealed class AsyncTrackingFileSerializer : BaseFileSerializer {
        public readonly TaskCompletionSource SerializeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource DeserializeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly List<string> SerializeStreamPaths = [];
        public bool BlockSerialize;
        public bool BlockDeserialize;

        public AsyncTrackingFileSerializer(string path)
            : base(path) {
        }

        protected override async ValueTask SerializeDataAsync(Stream stream, ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken) {
            if (stream is FileStream fileStream) {
                SerializeStreamPaths.Add(fileStream.Name);
            }

            SerializeStarted.TrySetResult();
            if (BlockSerialize) {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            byte[] bytes = Encoding.UTF8.GetBytes("payload");
            await stream.WriteAsync(bytes, cancellationToken);
        }

        protected override async ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeDataAsync(Stream stream, CancellationToken cancellationToken) {
            DeserializeStarted.TrySetResult();
            if (BlockDeserialize) {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return new ConcurrentDictionary<string, byte[]>();
        }
    }
}
