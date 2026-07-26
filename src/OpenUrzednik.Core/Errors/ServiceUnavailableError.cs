using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service is currently unavailable.
/// </summary>
/// <param name="Message">The message associated with the error.</param>
public record ServiceUnavailableError(string Message) : OpenUrzednikError(Message)
{
    /// <inheritdoc />
    public override Exception ToException()
        => new ServiceUnavailableException(Message);
}
