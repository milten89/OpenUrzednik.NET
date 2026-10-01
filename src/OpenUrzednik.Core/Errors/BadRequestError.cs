using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the server rejected the request as invalid (HTTP 400),
/// for example because a parameter is outside the range the API accepts.
/// </summary>
/// <param name="message">The message associated with the error, including the server message if one was returned.</param>
/// <param name="statusCode">HTTP status code of the response that caused the error, if any.</param>
public sealed class BadRequestError(string message, int? statusCode = null) : OpenUrzednikError(ErrorCode, message, statusCode)
{
    public const string ErrorCode = "badRequest";

    /// <inheritdoc />
    public override Exception ToException()
        => new BadRequestException(Message);
}
