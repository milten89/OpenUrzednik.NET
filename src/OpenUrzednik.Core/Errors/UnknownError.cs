using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that an unknown error has occurred.
/// </summary>
/// <param name="message">The message associated with the error.</param>
public sealed class UnknownError(string message) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "unknownError";

    /// <inheritdoc />
    public override Exception ToException()
        => new UnknownException(Message);
}
