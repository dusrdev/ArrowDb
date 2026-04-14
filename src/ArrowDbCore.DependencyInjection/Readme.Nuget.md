# ArrowDb.DependencyInjection

Dependency injection support for ArrowDb.

This package provides `IArrowDbProvider`, the public generic `ArrowDbProvider<TSerializer>`, and an optional hosted-service primer for eager startup initialization.

## Install

```bash
dotnet add package ArrowDb.DependencyInjection
```

## Register

```csharp
builder.Services.AddSingleton(new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray));
builder.Services.AddSingleton<IArrowDbProvider, ArrowDbProvider<FileSerializer>>();
```

For AES-backed storage:

```csharp
builder.Services.AddSingleton(_ => Aes.Create());
builder.Services.AddSingleton(serviceProvider =>
    new AesFileSerializer(
        path,
        serviceProvider.GetRequiredService<Aes>(),
        ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray));
builder.Services.AddSingleton<IArrowDbProvider, ArrowDbProvider<AesFileSerializer>>();
```

If you want eager host-startup initialization for a singleton provider, also add:

```csharp
builder.Services.AddArrowDbInitialization();
```

## Consume

```csharp
public sealed class MyService {
    private readonly IArrowDbProvider _provider;

    public MyService(IArrowDbProvider provider) {
        _provider = provider;
    }

    public async Task<int> CountAsync() {
        ArrowDb db = await _provider.GetAsync();
        return db.Count;
    }
}
```

`ArrowDbProvider<TSerializer>` does not dispose the serializer by default. That fits the common DI case where the serializer is registered separately and the container owns it.

If you want the provider to own the serializer lifetime instead, register it with a factory and pass `disposeSerializer: true`:

```csharp
builder.Services.AddSingleton<IArrowDbProvider>(_ =>
    new ArrowDbProvider<FileSerializer>(
        new FileSerializer(path, ArrowDbJsonContext.Default.ConcurrentDictionaryStringByteArray),
        disposeSerializer: true));
```

If you register the provider as a singleton and add `AddArrowDbInitialization()`, the host will prime it during startup.
