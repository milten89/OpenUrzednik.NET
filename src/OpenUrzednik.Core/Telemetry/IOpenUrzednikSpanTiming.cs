namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// A span that can stop its clock and report how long it took, so a metric of the same operation records exactly the
/// span's duration (ADR-0025). A capability of <see cref="IOpenUrzednikSpan"/>: the library checks for it with <c>is</c>.
/// </summary>
public interface IOpenUrzednikSpanTiming
{
    /// <summary>
    /// Fixes the span's end time now and returns its duration. Ending the span later keeps that end time; calling this again
    /// moves the end time to the new moment.
    /// </summary>
    /// <returns>The span's duration; <see langword="null"/> if the span measures no time.</returns>
    TimeSpan? StopTiming();
}
