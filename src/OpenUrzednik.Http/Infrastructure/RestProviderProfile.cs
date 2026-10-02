using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Describes a REST provider to <see cref="RestRequestExecutor"/>: its names in telemetry and messages, and an optional
/// override of how error responses become errors. For provider authors.
/// </summary>
public sealed class RestProviderProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestProviderProfile"/> class.
    /// </summary>
    /// <param name="name">Short lowercase name used in span names, e.g. <c>nbp</c> gives <c>nbp.http.get</c>.</param>
    /// <param name="displayName">Name used in error messages and logs, e.g. <c>NBP API</c>.</param>
    /// <param name="mapErrorAsync">
    /// Optional override for non-success responses. Return <see langword="null"/> to use the default mapping (ADR-0002).
    /// Expected failures, e.g. an error body that isn't valid JSON, must be returned as errors or <see langword="null"/>:
    /// anything the override throws reaches the caller of <see cref="RestRequestExecutor.GetAsync{TDto}"/>.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="displayName"/> is empty.</exception>
    public RestProviderProfile(string name, string displayName, Func<ErrorResponseContext, Task<OpenUrzednikError?>>? mapErrorAsync = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Name = name;
        DisplayName = displayName;
        MapErrorAsync = mapErrorAsync;
    }

    /// <summary>
    /// Gets the short name used in span names (<c>&lt;name&gt;.http.get</c>).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name used in error messages and logs.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the optional override for non-success responses. It runs before the default mapping; a <see langword="null"/> result falls back to it.
    /// Exceptions it throws aren't caught: they reach the caller and mark the span as failed.
    /// </summary>
    public Func<ErrorResponseContext, Task<OpenUrzednikError?>>? MapErrorAsync { get; }
}
