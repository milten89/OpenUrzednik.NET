namespace OpenUrzednik.Core.Errors;

public record SerializationError(string Message, Exception Exception) : OpenUrzednikError(Message)
{
    public override Exception ToException()
    {
        return Exception;
    }
}
