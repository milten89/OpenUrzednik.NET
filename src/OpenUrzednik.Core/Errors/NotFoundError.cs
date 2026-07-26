using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a requested resource was not found.
/// </summary>
/// <param name="Message">The message associated with the error.</param>
public record NotFoundError(string Message) : OpenUrzednikError(Message)
{
    /// <inheritdoc />
    public override Exception ToException()
        => new NotFoundException(Message);
}
