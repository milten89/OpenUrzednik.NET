using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service is currently unavailable.
/// </summary>
/// <param name="message">The message associated with the error.</param>
public sealed class ServiceUnavailableError(string message) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "serviceUnavailable";

    /// <inheritdoc />
    public override Exception ToException()
        => new ServiceUnavailableException(Message);
}
