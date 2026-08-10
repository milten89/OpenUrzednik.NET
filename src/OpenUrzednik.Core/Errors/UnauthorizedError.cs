using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the user is unauthorized to perform the requested action.
/// </summary>
/// <param name="Message">The message associated with the error.</param>
public record UnauthorizedError(string Message) : OpenUrzednikError(Message)
{
    /// <inheritdoc />
    public override Exception ToException()
        => new UnauthorizedException(Message);
}
