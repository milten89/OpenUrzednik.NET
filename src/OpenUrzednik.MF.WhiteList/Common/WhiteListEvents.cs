using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// The MF White List-specific log events, built from <see cref="WhiteListLogEvents"/>. The HTTP events are in <c>RestLogEvents</c>.
/// </summary>
internal static class WhiteListEvents
{
    internal static readonly OpenUrzednikEventId ValidationFailed = new(WhiteListLogEvents.ValidationFailed, nameof(WhiteListLogEvents.ValidationFailed));
    internal static readonly OpenUrzednikEventId NotOnList = new(WhiteListLogEvents.NotOnList, nameof(WhiteListLogEvents.NotOnList));
    internal static readonly OpenUrzednikEventId MappingFailed = new(WhiteListLogEvents.MappingFailed, nameof(WhiteListLogEvents.MappingFailed));
}
