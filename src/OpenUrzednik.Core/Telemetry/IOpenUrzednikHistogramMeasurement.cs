namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// A measurement recorded on a histogram, such as an operation's duration.
/// </summary>
public interface IOpenUrzednikHistogramMeasurement : IOpenUrzednikMeasurement
{
    /// <summary>The value, in <see cref="IOpenUrzednikMeasurement.Unit"/>.</summary>
    double Value { get; }

    /// <summary>
    /// The suggested bucket boundaries for a new histogram (<c>InstrumentAdvice</c>), in ascending order;
    /// <see langword="null"/> leaves them to the listener.
    /// </summary>
    IReadOnlyList<double>? BucketBoundaries { get; }
}
