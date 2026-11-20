namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Metrics that record nothing: the default on .NET Framework (netstandard2.0) when no metrics are passed.
/// </summary>
public sealed class NullOpenUrzednikMetrics : IOpenUrzednikMetrics
{
    /// <summary>The shared instance.</summary>
    public static readonly NullOpenUrzednikMetrics Instance = new();

    private NullOpenUrzednikMetrics() { }

    /// <inheritdoc/>
    /// <returns>Always <see langword="false"/>.</returns>
    public bool IsEnabled(string measurementName) => false;

    /// <inheritdoc/>
    /// <remarks>Does nothing, also for <see langword="null"/>.</remarks>
    public void Record(IOpenUrzednikMeasurement measurement) { }
}
