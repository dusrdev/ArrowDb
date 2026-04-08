namespace ArrowDbCore;

/// <summary>
/// Thrown when a file-backed <see cref="ArrowDb"/> cannot acquire exclusive ownership
/// of the underlying database file.
/// </summary>
public sealed class ArrowDbOwnershipException : IOException {
    /// <summary>
    /// Initializes a new instance of the <see cref="ArrowDbOwnershipException"/> class.
    /// </summary>
    public ArrowDbOwnershipException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrowDbOwnershipException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ArrowDbOwnershipException(string? message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrowDbOwnershipException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ArrowDbOwnershipException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
