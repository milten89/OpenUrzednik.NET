using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that an unknown error has occurred.
/// </summary>
/// <param name="Message">The message associated with the error.</param>
public record UnknownError(string Message) : OpenUrzednikError(Message)
{
    /// <inheritdoc />
    public override Exception ToException()
        => new UnknownException(Message);
}
