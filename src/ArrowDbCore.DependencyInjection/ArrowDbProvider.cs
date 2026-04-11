namespace ArrowDbCore.DependencyInjection;

/// <summary>
/// Provides lazy asynchronous access to an <see cref="ArrowDb"/> instance backed by a specific serializer.
/// </summary>
/// <typeparam name="TSerializer">The serializer type used by the provider.</typeparam>
public sealed class ArrowDbProvider<TSerializer> : IArrowDbProvider, IDisposable, IAsyncDisposable
    where TSerializer : IDbSerializer
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly TSerializer _serializer;
    private readonly bool _disposeSerializer;
    private ArrowDb? _arrowDb;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrowDbProvider{TSerializer}"/> class.
    /// </summary>
    /// <param name="serializer">The serializer instance used by this provider.</param>
    /// <param name="disposeSerializer">Whether this provider owns the serializer lifetime.</param>
    public ArrowDbProvider(TSerializer serializer, bool disposeSerializer = false)
    {
        _serializer = serializer;
        _disposeSerializer = disposeSerializer;
    }

    /// <inheritdoc />
    public ValueTask<ArrowDb> GetAsync(CancellationToken cancellationToken = default)
    {
        ArrowDb? arrowDb = _arrowDb;
        if (arrowDb is not null)
        {
            return ValueTask.FromResult(arrowDb);
        }

        return GetAsyncCore(cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeSerializer)
        {
            _serializer.Dispose();
        }

        _semaphore.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposeSerializer)
        {
            await _serializer.DisposeAsync();
        }

        _semaphore.Dispose();
    }

    private async ValueTask<ArrowDb> GetAsyncCore(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            ArrowDb? arrowDb = _arrowDb;
            if (arrowDb is not null)
            {
                return arrowDb;
            }

            _arrowDb = await ArrowDb.CreateCustom(_serializer, _disposeSerializer, cancellationToken);
            return _arrowDb;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
