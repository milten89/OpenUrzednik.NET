namespace OpenUrzednik.Core.Errors;

/// <summary>
/// A <see cref="RangeValidationError"/> that carries the range's bounds with their exact type, e.g.
/// <c>RangeValidationError&lt;DateTime&gt;</c> for a date range (ADR-0022). The bounds aren't boxed unless
/// <see cref="OpenUrzednikError.Metadata"/> is read.
/// </summary>
/// <typeparam name="T">The type of the bounds. Public dates are <see cref="DateTime"/> on every target (ADR-0005).</typeparam>
public sealed class RangeValidationError<T> : RangeValidationError
    where T : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RangeValidationError{T}"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="ruleName">The name of the rule that failed (e.g. <c>dateRange</c>).</param>
    /// <param name="startName">The name of the parameter or property that holds the start.</param>
    /// <param name="start">The range's start.</param>
    /// <param name="endName">The name of the parameter or property that holds the end.</param>
    /// <param name="end">The range's end.</param>
    public RangeValidationError(string message, string ruleName, string startName, T start, string endName, T end)
        : base(message, ruleName, startName, endName)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the range's start.
    /// </summary>
    public T Start { get; }

    /// <summary>
    /// Gets the range's end.
    /// </summary>
    public T End { get; }

    private protected override bool TryGetStartMetadata(out object? start) => MetadataValue.TryConvert(Start, out start);

    private protected override bool TryGetEndMetadata(out object? end) => MetadataValue.TryConvert(End, out end);
}
