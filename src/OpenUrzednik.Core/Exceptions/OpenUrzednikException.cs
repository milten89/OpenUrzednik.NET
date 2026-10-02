using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents the base class for exceptions thrown by the OpenUrzednik library.
/// </summary>
public abstract class OpenUrzednikException : Exception
{
    /// <summary>
    /// Gets the error code, the same as <see cref="OpenUrzednikError.Code"/>.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error this exception was created from, with its metadata.
    /// <see langword="null"/> when the exception was created directly, not by <see cref="OpenUrzednikError.ToException"/>
    /// or <c>EnsureSuccess()</c>. When there were several errors, this is the first one.
    /// </summary>
    public OpenUrzednikError? Error { get; private set; }

    /// <summary>
    /// Gets every error of the failed result, in order. Empty when <see cref="Error"/> is <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<OpenUrzednikError> Errors { get; private set; } = [];

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

    internal void SetErrors(OpenUrzednikError error, IReadOnlyList<OpenUrzednikError> errors)
        => (Error, Errors) = (error, errors);
}
