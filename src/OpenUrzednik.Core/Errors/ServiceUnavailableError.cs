using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service is currently unavailable: it returned a 5xx status,
/// or it could not be reached because of a network failure.
/// </summary>
/// <param name="message">The message associated with the error.</param>
/// <param name="statusCode">HTTP status code of the response that caused the error, if any.</param>
/// <param name="exception">The exception that caused the error (e.g. an <see cref="System.Net.Http.HttpRequestException"/>), if any.</param>
public sealed class ServiceUnavailableError(string message, int? statusCode = null, Exception? exception = null) : OpenUrzednikError(ErrorCode, message, statusCode)
{
    public const string ErrorCode = "serviceUnavailable";

    /// <summary>
    /// Gets the exception that caused the error, if any.
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    public override Exception ToException()
        => Exception is null
            ? new ServiceUnavailableException(Message)
            : new ServiceUnavailableException(Message, Exception);
}
