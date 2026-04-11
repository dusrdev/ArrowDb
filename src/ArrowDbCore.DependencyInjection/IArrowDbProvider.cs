namespace ArrowDbCore.DependencyInjection;

/// <summary>
/// Provides asynchronous access to a DI-managed <see cref="ArrowDb"/> instance.
/// </summary>
public interface IArrowDbProvider {
    /// <summary>
    /// Gets the initialized <see cref="ArrowDb"/> instance.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that cancels only the wait operation.</param>
    ValueTask<ArrowDb> GetAsync(CancellationToken cancellationToken = default);
}
