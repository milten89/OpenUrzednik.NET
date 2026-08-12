using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a requested resource was not found.
/// </summary>
/// <param name="message">The message associated with the error.</param>
public sealed class NotFoundError(string message) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "notFound";

    /// <inheritdoc />
    public override Exception ToException()
        => new NotFoundException(Message);
}
