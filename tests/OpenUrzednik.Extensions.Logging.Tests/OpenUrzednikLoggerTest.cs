using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Extensions.Logging.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class OpenUrzednikLoggerTest
{
    private static readonly OpenUrzednikEventId TestEvent = new(1001, "RequestTimedOut");

    private readonly RecordingLogger _logger = new();

    private OpenUrzednikLogger CreateSut() => new(_logger);
}
