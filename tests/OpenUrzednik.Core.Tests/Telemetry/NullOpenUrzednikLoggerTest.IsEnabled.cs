using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikLoggerTest
{
    [Theory]
    [InlineData(OpenUrzednikLogLevel.Trace)]
    [InlineData(OpenUrzednikLogLevel.Debug)]
    [InlineData(OpenUrzednikLogLevel.Information)]
    [InlineData(OpenUrzednikLogLevel.Warning)]
    [InlineData(OpenUrzednikLogLevel.Error)]
    [InlineData(OpenUrzednikLogLevel.Critical)]
    [InlineData(OpenUrzednikLogLevel.None)]
    public void IsEnabled_ReturnFalse(OpenUrzednikLogLevel logLevel)
    {
        // Act & Assert
        NullOpenUrzednikLogger.Instance.IsEnabled(logLevel).ShouldBeFalse();
    }
}
