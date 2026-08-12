using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the user is unauthorized to perform the requested action.
/// </summary>
/// <param name="message">The message associated with the error.</param>
public sealed class UnauthorizedError(string message) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "unauthorized";

    /// <inheritdoc />
    public override Exception ToException()
        => new UnauthorizedException(Message);
}
