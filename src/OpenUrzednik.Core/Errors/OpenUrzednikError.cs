using System.Collections.ObjectModel;

using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents a base class for errors in the OpenUrzednik application. Error types describe the condition, whatever the
/// transport, and take no transport details; an error caused by an HTTP response is the condition's HTTP subclass in the
/// <c>OpenUrzednik.Transport.Http</c> package (e.g. <c>HttpNotFoundError</c>, ADR-0021).
/// </summary>
public abstract class OpenUrzednikError
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyMetadata = ReadOnlyDictionary<string, object?>.Empty;

    // Entries added with AddMetadata, in order (1.0 behaviour).
    private Dictionary<string, object?>? _added;

    // Metadata is built on first read (ADR-0021): CollectMetadata, then the AddMetadata entries.
    private IReadOnlyDictionary<string, object?>? _metadata;

    // Counts AddMetadata calls, so a build that overlapped one is never cached.
    private int _version;

    /// <summary>
    /// Gets the identification code associated with this error.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error message associated with this error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the metadata associated with this error: the error's details as values that serialize the same way everywhere
    /// (JSON, logs, OpenTelemetry attributes), e.g. to copy into a ProblemDetails response (ADR-0021). The error types' own keys hold
    /// strings, booleans, numbers (<see cref="int"/>, <see cref="long"/>, <see cref="double"/>), <see langword="null"/>, arrays of
    /// one of these, ISO 8601 strings for dates, times and durations, and invariant strings for <see cref="decimal"/> values.
    /// It is built on first read.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata => Volatile.Read(ref _metadata) ?? BuildMetadata();

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
    /// Adds metadata to the error with the specified key and value. The value is stored as passed; prefer the value types listed on
    /// <see cref="Metadata"/>. Call it from the constructor: like a <see cref="Dictionary{TKey, TValue}"/>, it isn't safe to call while
    /// another thread reads <see cref="Metadata"/>.
    /// </summary>
    /// <param name="key">Metadata key.</param>
    /// <param name="value">Metadata value.</param>
    /// <exception cref="ArgumentException">An entry with the same key was already added.</exception>
    protected void AddMetadata(string key, object? value)
    {
        (_added ??= []).Add(key, value);
        // A later read rebuilds the metadata with this entry; a build running now isn't cached.
        Interlocked.Increment(ref _version);
        Volatile.Write(ref _metadata, null);
    }

    /// <summary>
    /// Adds the error's own metadata when <see cref="Metadata"/> is first read, so a value that is never read is never converted or
    /// boxed. An override calls the base method first, then adds its entries in the forms listed on <see cref="Metadata"/>.
    /// Entries added with <see cref="AddMetadata"/> follow and replace an entry with the same key.
    /// </summary>
    /// <param name="metadata">The metadata being built.</param>
    protected virtual void CollectMetadata(IDictionary<string, object?> metadata) { }

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

    private IReadOnlyDictionary<string, object?> BuildMetadata()
    {
        var version = Volatile.Read(ref _version);
        var metadata = new Dictionary<string, object?>();
        CollectMetadata(metadata);
        var added = _added;
        if (added is not null)
        {
            foreach (var entry in added)
                metadata[entry.Key] = entry.Value;
        }

        // A local, not the property in the comparison, so that mutation testing can compile its mutants (ADR-0020).
        var count = metadata.Count;
        IReadOnlyDictionary<string, object?> built = count == 0 ? EmptyMetadata : metadata;
        // Two threads may both build it; the first one stored wins, and both results hold the same entries.
        var stored = Interlocked.CompareExchange(ref _metadata, built, null) ?? built;
        // An AddMetadata call during the build may have missed this result: drop it from the cache, so the next read rebuilds.
        if (Volatile.Read(ref _version) != version)
            Interlocked.CompareExchange(ref _metadata, null, stored);
        return stored;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Code}: {Message}";
}
