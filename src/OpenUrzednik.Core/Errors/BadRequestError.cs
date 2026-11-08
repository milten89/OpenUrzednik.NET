using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service rejected the request as invalid, for example because a parameter is outside the
/// range the API accepts, whatever the transport (ADR-0021). An HTTP response that caused it makes it an <c>HttpBadRequestError</c> (<c>OpenUrzednik.Transport.Http</c>).
/// </summary>
public class BadRequestError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "badRequest";

    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error, including the server message if one was returned.</param>
    public BadRequestError(string message)
        : base(ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error, including the server message if one was returned.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    [Obsolete("A condition type takes no transport details: use BadRequestError(string), or HttpBadRequestError (OpenUrzednik.Transport.Http) for an error caused by an HTTP response (ADR-0021). Removed in 2.0.")]
    public BadRequestError(string message, int? statusCode = null)
        : this(message)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new BadRequestException(Message);
}
