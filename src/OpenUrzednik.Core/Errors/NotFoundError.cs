using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a requested resource was not found, whatever the transport (ADR-0021).
/// An HTTP response that caused it makes it an <c>HttpNotFoundError</c> (<c>OpenUrzednik.Transport.Http</c>).
/// </summary>
public class NotFoundError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "notFound";

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    public NotFoundError(string message)
        : base(ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    [Obsolete("A condition type takes no transport details: use NotFoundError(string), or HttpNotFoundError (OpenUrzednik.Transport.Http) for an error caused by an HTTP response (ADR-0021). Removed in 2.0.")]
    public NotFoundError(string message, int? statusCode = null)
        : this(message)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new NotFoundException(Message);
}
