using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that an unknown error has occurred.
/// </summary>
/// <param name="message">The message associated with the error.</param>
/// <param name="statusCode">HTTP status code of the response that caused the error, if any.</param>
public sealed class UnknownError(string message, int? statusCode = null) : OpenUrzednikError(ErrorCode, message, statusCode)
{
    public const string ErrorCode = "unknownError";

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new UnknownException(Message);
}
