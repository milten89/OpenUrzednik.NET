using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// The NBP-specific log events, built from <see cref="NbpLogEvents"/>. The HTTP events are in <c>RestLogEvents</c>.
/// </summary>
internal static class NbpEvents
{
    internal static readonly OpenUrzednikEventId ValidationFailed = new(NbpLogEvents.ValidationFailed, nameof(NbpLogEvents.ValidationFailed));
    internal static readonly OpenUrzednikEventId NoData = new(NbpLogEvents.NoData, nameof(NbpLogEvents.NoData));
    internal static readonly OpenUrzednikEventId MappingFailed = new(NbpLogEvents.MappingFailed, nameof(NbpLogEvents.MappingFailed));
}
