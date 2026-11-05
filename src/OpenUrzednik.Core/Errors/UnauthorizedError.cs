using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the user is unauthorized to perform the requested action.
/// </summary>
public sealed class UnauthorizedError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "unauthorized";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    public UnauthorizedError(string message, int? statusCode = null)
        : base(ErrorCode, message)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new UnauthorizedException(Message);
}
