using Microsoft.Extensions.Logging;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

public sealed partial class OpenUrzednikLoggerTest
{
    [Theory]
    [InlineData(OpenUrzednikLogLevel.Trace, LogLevel.Trace)]
    [InlineData(OpenUrzednikLogLevel.Debug, LogLevel.Debug)]
    [InlineData(OpenUrzednikLogLevel.Information, LogLevel.Information)]
    [InlineData(OpenUrzednikLogLevel.Warning, LogLevel.Warning)]
    [InlineData(OpenUrzednikLogLevel.Error, LogLevel.Error)]
    [InlineData(OpenUrzednikLogLevel.Critical, LogLevel.Critical)]
    [InlineData(OpenUrzednikLogLevel.None, LogLevel.None)]
    [InlineData((OpenUrzednikLogLevel)42, LogLevel.None)]
    public void ToLogLevel_Level_MapsToTheSameMicrosoftLevel(OpenUrzednikLogLevel level, LogLevel expected)
    {
        // Act
        var logLevel = OpenUrzednikLogger.ToLogLevel(level);

        // Assert
        logLevel.ShouldBe(expected);
    }
}
