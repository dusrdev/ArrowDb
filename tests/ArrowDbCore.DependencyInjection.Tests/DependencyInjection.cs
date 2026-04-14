using System.Collections.Concurrent;
using System.Security.Cryptography;

using ArrowDbCore.DependencyInjection;
using ArrowDbCore.Serializers;
using ArrowDbCore.Tests.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArrowDbCore.DependencyInjection.Tests;

public sealed class DependencyInjection
{
    [Fact]
    public async Task InitializationHostedService_PrimesRegisteredProvider_AndReturnsSameInstance()
    {
        var serializer = new TrackingSerializer();
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(serializer);
                services.AddSingleton<IArrowDbProvider, ArrowDbProvider<TrackingSerializer>>();
                services.AddArrowDbInitialization();
            })
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, serializer.DeserializeCalls);

        IArrowDbProvider provider = host.Services.GetRequiredService<IArrowDbProvider>();
        ArrowDb first = await provider.GetAsync(TestContext.Current.CancellationToken);
        ArrowDb second = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task InitializationHostedService_WhenFileInitializationFails_HostStartupFails()
    {
        string path = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(path, "not json", TestContext.Current.CancellationToken);

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray));
                    services.AddSingleton<IArrowDbProvider, ArrowDbProvider<FileSerializer>>();
                    services.AddArrowDbInitialization();
                })
                .Build();

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => host.StartAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task GenericProvider_WithInMemorySerializer_InitializesAndSupportsReads()
    {
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(new InMemorySerializer());
                services.AddSingleton<IArrowDbProvider, ArrowDbProvider<InMemorySerializer>>();
                services.AddArrowDbInitialization();
            })
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        IArrowDbProvider provider = host.Services.GetRequiredService<IArrowDbProvider>();
        ArrowDb db = await provider.GetAsync(TestContext.Current.CancellationToken);
        Assert.True(db.Upsert("seed", 1, JContext.Default.Int32));
        Assert.True(db.TryGetValue("seed", JContext.Default.Int32, out int value));
        Assert.Equal(1, value);
    }

    [Fact]
    public async Task GenericProvider_WithAesFileSerializer_InitializesSuccessfully()
    {
        string path = Path.GetTempFileName();

        try
        {
            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(_ => Aes.Create());
                    services.AddSingleton(serviceProvider =>
                        new AesFileSerializer(
                            path,
                            serviceProvider.GetRequiredService<Aes>(),
                            ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray));
                    services.AddSingleton<IArrowDbProvider, ArrowDbProvider<AesFileSerializer>>();
                    services.AddArrowDbInitialization();
                })
                .Build();

            await host.StartAsync(TestContext.Current.CancellationToken);

            IArrowDbProvider provider = host.Services.GetRequiredService<IArrowDbProvider>();
            ArrowDb db = await provider.GetAsync(TestContext.Current.CancellationToken);
            Assert.True(db.Upsert("seed", 1, JContext.Default.Int32));
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task HostShutdown_DisposesOwnedSerializer_AndRetainedArrowDbBlocksPersistence()
    {
        string path = Path.GetTempFileName();
        ArrowDb? db = null;
        FileSerializer? serializer = null;

        try
        {
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    serializer = new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray);
                    services.AddSingleton<IArrowDbProvider>(_ => new ArrowDbProvider<FileSerializer>(serializer, disposeSerializer: true));
                    services.AddArrowDbInitialization();
                })
                .Build();

            await host.StartAsync(TestContext.Current.CancellationToken);

            IArrowDbProvider provider = host.Services.GetRequiredService<IArrowDbProvider>();
            db = await provider.GetAsync(TestContext.Current.CancellationToken);
            Assert.True(db.Upsert("seed", 1, JContext.Default.Int32));

            await host.StopAsync(TestContext.Current.CancellationToken);
            host.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => db.SerializeAsync(TestContext.Current.CancellationToken));
            Assert.True(serializer!.IsDisposed);

            using FileStream lockStream = new($"{path}.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Assert.NotNull(lockStream);
        }
        finally
        {
            FileBackedTestHelpers.DeleteArtifacts(path);
        }
    }

    [Fact]
    public async Task GenericProvider_IsLazyWithoutInitializationHostedService()
    {
        var serializer = new TrackingSerializer();
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(serializer);
                services.AddSingleton<IArrowDbProvider, ArrowDbProvider<TrackingSerializer>>();
            })
            .Build();

        Assert.Equal(0, serializer.DeserializeCalls);

        IArrowDbProvider provider = host.Services.GetRequiredService<IArrowDbProvider>();
        ArrowDb db = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(db);
        Assert.Equal(1, serializer.DeserializeCalls);
    }

    [Fact]
    public async Task ProviderOwnedSerializer_IsDisposedWhenHostStops()
    {
        var serializer = new TrackingSerializer();
        IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IArrowDbProvider>(_ => new ArrowDbProvider<TrackingSerializer>(serializer, disposeSerializer: true));
                services.AddArrowDbInitialization();
            })
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
        host.Dispose();

        Assert.True(serializer.IsDisposed);
    }

    [Fact]
    public async Task Provider_DoesNotDisposeExternalSerializerByDefault()
    {
        var serializer = new TrackingSerializer();
        var provider = new ArrowDbProvider<TrackingSerializer>(serializer);

        ArrowDb db = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(db);

        await provider.DisposeAsync();

        Assert.False(serializer.IsDisposed);
    }

    private sealed class TrackingSerializer : IDbSerializer
    {
        public int DeserializeCalls;
        public bool IsDisposed { get; private set; }

        public ValueTask<ConcurrentDictionary<string, byte[]>> DeserializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            Interlocked.Increment(ref DeserializeCalls);
            return ValueTask.FromResult(new ConcurrentDictionary<string, byte[]>());
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
