using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service is currently unavailable.
/// </summary>
/// <param name="message">The message associated with the error.</param>
/// <param name="statusCode">HTTP status code of the response that caused the error, if any.</param>
public sealed class ServiceUnavailableError(string message, int? statusCode = null) : OpenUrzednikError(ErrorCode, message, statusCode)
{
    public const string ErrorCode = "serviceUnavailable";

    /// <inheritdoc />
    public override Exception ToException()
        => new ServiceUnavailableException(Message);
}
