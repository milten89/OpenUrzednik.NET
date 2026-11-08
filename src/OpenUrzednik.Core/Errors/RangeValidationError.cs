namespace OpenUrzednik.Core.Errors;

/// <summary>
/// A <see cref="ValidationError"/> for a rejected range of two parameters, e.g. a <c>from</c> date later than <c>to</c> (ADR-0022).
/// Check for it with <c>is RangeValidationError</c> to read the parameter names and metadata, or with <c>is RangeValidationError&lt;T&gt;</c>
/// for the bounds with their exact type. <see cref="ValidationError.Name"/> is <see langword="null"/>: the rule spans two parameters.
/// </summary>
/// <remarks>Only <see cref="RangeValidationError{T}"/> derives from this type.</remarks>
public abstract class RangeValidationError : ValidationError
{
    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="StartName"/>.</summary>
    public const string StartNameMetadataKey = "startName";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the range's start, in its metadata form (e.g. <c>"2026-01-05"</c>).</summary>
    public const string StartMetadataKey = "start";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="EndName"/>.</summary>
    public const string EndNameMetadataKey = "endName";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the range's end, in its metadata form.</summary>
    public const string EndMetadataKey = "end";

    private protected RangeValidationError(string message, string ruleName, string startName, string endName)
        : base(message, ruleName, name: null, value: null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startName);
        ArgumentException.ThrowIfNullOrWhiteSpace(endName);

        StartName = startName;
        EndName = endName;
    }

    /// <summary>
    /// Gets the name of the parameter or property that holds the range's start (e.g. <c>from</c>).
    /// </summary>
    public string StartName { get; }

    /// <summary>
    /// Gets the name of the parameter or property that holds the range's end (e.g. <c>to</c>).
    /// </summary>
    public string EndName { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        base.CollectMetadata(metadata);
        metadata[StartNameMetadataKey] = StartName;
        // Bounds with no metadata form (ADR-0022) are left out; RangeValidationError<T>'s Start and End still have them.
        if (TryGetStartMetadata(out var start))
            metadata[StartMetadataKey] = start;
        metadata[EndNameMetadataKey] = EndName;
        if (TryGetEndMetadata(out var end))
            metadata[EndMetadataKey] = end;
    }

    /// <summary>Gets the range's start in its metadata form.</summary>
    private protected abstract bool TryGetStartMetadata(out object? start);

    /// <summary>Gets the range's end in its metadata form.</summary>
    private protected abstract bool TryGetEndMetadata(out object? end);
}
