using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a requested resource was not found.
/// </summary>
/// <param name="message">The message associated with the error.</param>
/// <param name="statusCode">HTTP status code of the response that caused the error, if any.</param>
public sealed class NotFoundError(string message, int? statusCode = null) : OpenUrzednikError(ErrorCode, message, statusCode)
{
    public const string ErrorCode = "notFound";

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new NotFoundException(Message);
}
