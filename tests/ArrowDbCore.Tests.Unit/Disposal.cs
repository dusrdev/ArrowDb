using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

using ArrowDbCore.Serializers;
using ArrowDbCore.Tests.Common;

namespace ArrowDbCore.Tests.Unit;

public sealed class Disposal
{
    [Fact]
    public async Task InMemorySerializer_WhenDisposed_ReportsDisposedAndThrowsFromAsyncMethods()
    {
        var serializer = new InMemorySerializer();

        await serializer.DisposeAsync();

        Assert.True(serializer.IsDisposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => serializer.DeserializeAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => serializer.SerializeAsync(new ConcurrentDictionary<string, byte[]>()).AsTask());
    }

    [Fact]
    public async Task FileSerializer_WhenDisposed_ReportsDisposedAndThrowsFromAsyncMethods()
    {
        string path = Path.GetTempFileName();
        FileSerializer? serializer = null;

        try
        {
            serializer = new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);

            await serializer.DisposeAsync();

            Assert.True(serializer.IsDisposed);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => serializer.DeserializeAsync().AsTask());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => serializer.SerializeAsync(new ConcurrentDictionary<string, byte[]>()).AsTask());
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public void AesFileSerializer_Dispose_DoesNotDisposeSuppliedAes()
    {
        string path = Path.GetTempFileName();
        using Aes aes = Aes.Create();
        AesFileSerializer? serializer = null;

        try
        {
            serializer = new AesFileSerializer(path, aes, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);

            serializer.Dispose();

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            Assert.NotNull(encryptor);
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task ArrowDb_WhenSerializerDisposed_PersistenceApisThrowAndInMemoryOperationsStillWork()
    {
        ArrowDb db = await ArrowDb.CreateInMemory();
        Assert.True(db.Upsert("seed", 1, JContext.Default.Int32));

        db.Serializer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => db.BeginTransaction());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => db.SerializeAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => db.RollbackAsync());

        Assert.True(db.TryGetValue("seed", JContext.Default.Int32, out int value));
        Assert.Equal(1, value);
        Assert.True(db.Upsert("seed", 2, JContext.Default.Int32));
        Assert.True(db.TryGetValue("seed", JContext.Default.Int32, out value));
        Assert.Equal(2, value);
    }

    [Fact]
    public async Task CreateCustom_WhenDeserializeFails_DisposesSerializer()
    {
        var serializer = new FailingSerializer();

        await Assert.ThrowsAsync<InvalidOperationException>(() => ArrowDb.CreateCustom(serializer).AsTask());

        Assert.True(serializer.IsDisposed);
    }

    [Fact]
    public async Task CreateFromFile_WhenDeserializeFails_DisposesSerializerAndReleasesOwnership()
    {
        string path = Path.GetTempFileName();
        FileSerializer? serializer = null;

        try
        {
            await File.WriteAllTextAsync(path, "not json", TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<JsonException>(() => ArrowDb.CreateFromFile(path).AsTask());

            serializer = new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);
            Assert.False(serializer.IsDisposed);
        }
        finally
        {
            serializer?.Dispose();
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    private sealed class FailingSerializer : IDbSerializer
    {
        public bool IsDisposed { get; private set; }

        public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }

        public ValueTask SerializeAsync(ConcurrentDictionary<string, byte[]> data, CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return ValueTask.CompletedTask;
        }

        public void Dispose() => IsDisposed = true;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
