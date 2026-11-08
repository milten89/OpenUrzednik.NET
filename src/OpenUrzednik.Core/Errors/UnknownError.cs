using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that an unknown error has occurred, whatever the transport (ADR-0022).
/// An HTTP response with an unexpected status makes it an <c>HttpUnknownError</c> (<c>OpenUrzednik.Transport.Http</c>).
/// </summary>
public class UnknownError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "unknown";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    public UnknownError(string message)
        : base(ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    [Obsolete("A condition type takes no transport details: use UnknownError(string), or HttpUnknownError (OpenUrzednik.Transport.Http) for an error caused by an HTTP response (ADR-0022). Removed in 2.0.")]
    public UnknownError(string message, int? statusCode = null)
        : this(message)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new UnknownException(Message);
}
