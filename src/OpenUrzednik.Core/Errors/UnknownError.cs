using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that an unknown error has occurred.
/// </summary>
public sealed class UnknownError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "unknown";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    public UnknownError(string message, int? statusCode = null)
        : base(ErrorCode, message)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new UnknownException(Message);
}
