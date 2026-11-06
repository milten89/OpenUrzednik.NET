using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the server rejected the request as invalid (HTTP 400),
/// for example because a parameter is outside the range the API accepts.
/// </summary>
public sealed class BadRequestError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "badRequest";

    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error, including the server message if one was returned.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    public BadRequestError(string message, int? statusCode = null)
        : base(ErrorCode, message)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new BadRequestException(Message);
}
