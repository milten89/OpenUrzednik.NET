namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents the base class for exceptions thrown by the OpenUrzednik library.
/// </summary>
public abstract class OpenUrzednikException : Exception
{
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikException"/> class with a specified error message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    protected OpenUrzednikException(string code, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    protected OpenUrzednikException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
    }
}
