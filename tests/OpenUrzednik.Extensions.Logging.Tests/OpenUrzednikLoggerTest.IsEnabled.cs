using Microsoft.Extensions.Logging;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class OpenUrzednikLoggerTest
{
    [Theory]
    [InlineData(OpenUrzednikLogLevel.Debug, true)]
    [InlineData(OpenUrzednikLogLevel.Information, true)]
    [InlineData(OpenUrzednikLogLevel.Trace, false)]
    [InlineData(OpenUrzednikLogLevel.None, false)]
    public void IsEnabled_LoggerMinimumIsDebug_FollowsTheLogger(OpenUrzednikLogLevel level, bool expected)
    {
        // Arrange
        _logger.MinimumLevel = LogLevel.Debug;

        // Act
        var enabled = CreateSut().IsEnabled(level);

        // Assert
        enabled.ShouldBe(expected);
    }
}
