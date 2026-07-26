namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents the base class for exceptions thrown by the OpenUrzednik library.
/// </summary>
public abstract class OpenUrzednikException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public OpenUrzednikException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public OpenUrzednikException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
