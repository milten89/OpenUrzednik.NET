namespace OpenUrzednik.Core.Errors;

public sealed class SerializationError : OpenUrzednikError
{
    public const string ErrorCode = "serializationError";

    public Exception Exception { get; }

    public SerializationError(string message, Exception exception)
        : base(ErrorCode, message)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception = exception;
    }

    public override Exception ToException()
        => Exception;
}
