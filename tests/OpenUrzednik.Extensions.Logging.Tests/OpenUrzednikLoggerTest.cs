namespace OpenUrzednik.Extensions.Logging.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class OpenUrzednikLoggerTest
{
    private readonly RecordingLogger _logger = new();

    private OpenUrzednikLogger CreateSut() => new(_logger);
}
