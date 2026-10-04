namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Status of a span, as in OpenTelemetry.
/// </summary>
public enum OpenUrzednikSpanStatus
{
    /// <summary>No status set (the default).</summary>
    Unset,

    /// <summary>The operation succeeded.</summary>
    Ok,

    /// <summary>The operation failed.</summary>
    Error,
}
