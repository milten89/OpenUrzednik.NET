namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// What every measurement passed to <see cref="IOpenUrzednikMetrics.Record"/> has: enough to create the instrument it belongs
/// to and to record on it. The value and the kind's own settings come from the interface of its instrument kind, such as
/// <see cref="IOpenUrzednikHistogramMeasurement"/>.
/// </summary>
/// <remarks>
/// The library creates the measurements; the names, units and tags it records are listed in each package's telemetry
/// reference and are stable within a major version (ADR-0025). Tags never carry personal identifiers or secrets.
/// </remarks>
public interface IOpenUrzednikMeasurement
{
    /// <summary>The instrument's name, e.g. <c>openurzednik.client.operation.duration</c>.</summary>
    string Name { get; }

    /// <summary>The unit, in UCUM notation as OpenTelemetry uses it, e.g. <c>s</c>.</summary>
    string Unit { get; }

    /// <summary>The instrument's description.</summary>
    string Description { get; }

    /// <summary>The measurement's tags (attributes), e.g. <c>openurzednik.operation</c> and <c>error.type</c>.</summary>
    IReadOnlyList<KeyValuePair<string, object?>> Tags { get; }
}
