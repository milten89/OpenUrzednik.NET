using System.Collections.ObjectModel;

using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents a base class for errors in the OpenUrzednik application.
/// </summary>
public abstract class OpenUrzednikError
{
    /// <summary>
    /// The metadata key under which errors caused by an HTTP response store its status code (as <see cref="int"/>).
    /// </summary>
    public const string StatusCodeMetadataKey = "statusCode";

    private static readonly IReadOnlyDictionary<string, object?> EmptyMetadata = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

    private Dictionary<string, object?>? _metadata;

    /// <summary>
    /// Gets the identification code associated with this error.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message associated with this error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the metadata associated with this error.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata => _metadata ?? EmptyMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikError"/> class with the specified error message.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    protected OpenUrzednikError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenUrzednikError"/> class with the specified error message
    /// and the HTTP status code of the response that caused the error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">HTTP status code stored in <see cref="Metadata"/> under <see cref="StatusCodeMetadataKey"/>; nothing is stored when <see langword="null"/>.</param>
    protected OpenUrzednikError(string code, string message, int? statusCode)
        : this(code, message)
    {
        if (statusCode.HasValue)
            AddMetadata(StatusCodeMetadataKey, statusCode.Value);
    }

    /// <summary>
    /// Adds metadata to the error with the specified key and value.
    /// </summary>
    /// <param name="key">Metadata key.</param>
    /// <param name="value">Metadata value.</param>
    protected void AddMetadata(string key, object? value)
        => (_metadata ??= []).Add(key, value);

    /// <summary>
    /// Creates an exception that represents this error. The exception keeps this error in
    /// <see cref="OpenUrzednikException.Error"/> and <see cref="OpenUrzednikException.Errors"/>.
    /// </summary>
    /// <returns>An exception that represents this error.</returns>
    public OpenUrzednikException ToException()
    {
        var exception = CreateException()
            ?? throw new InvalidOperationException($"{GetType().Name}.{nameof(CreateException)}() returned null.");
        exception.SetErrors(this, [this]);
        return exception;
    }

    /// <summary>
    /// Creates the exception returned by <see cref="ToException"/>. Pass the original exception, if any, as its
    /// <see cref="System.Exception.InnerException"/>. <see cref="ToException"/> attaches this error to it,
    /// so return a new instance on every call.
    /// </summary>
    /// <returns>A new exception that represents this error.</returns>
    protected abstract OpenUrzednikException CreateException();

    /// <inheritdoc />
    public override string ToString()
        => $"{Code}: {Message}";
}
