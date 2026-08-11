using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public class NullOpenUrzednikLoggerTest
{
    [Fact]
    public void Instance_AlwaysReturnSameInstance()
    {
        // Act
        var instance1 = NullOpenUrzednikLogger.Instance;
        var instance2 = NullOpenUrzednikLogger.Instance;

        // Assert
        ReferenceEquals(instance1, instance2).ShouldBeTrue();
    }

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

    [Fact]
    public void Log_NullArgs_NotThrowAnyException()
    {
        // Act & Assert
        Should.NotThrow(() => NullOpenUrzednikLogger.Instance.Log(OpenUrzednikLogLevel.Trace, null, null!));
    }

    [Fact]
    public void Log_1Gen_NullArgs_NotThrowAnyException()
    {
        // Act & Assert
        Should.NotThrow(() => NullOpenUrzednikLogger.Instance.Log(OpenUrzednikLogLevel.Trace, null, null!,
                                                           null!, 0));
    }

    [Fact]
    public void Log_2Gen_NullArgs_NotThrowAnyException()
    {
        // Act & Assert
        Should.NotThrow(() => NullOpenUrzednikLogger.Instance.Log(OpenUrzednikLogLevel.Trace, null, null!,
                                                           null!, 0,
                                                           null!, 0.0));
    }

    [Fact]
    public void Log_3Gen_NullArgs_NotThrowAnyException()
    {
        // Act & Assert
        Should.NotThrow(() => NullOpenUrzednikLogger.Instance.Log(OpenUrzednikLogLevel.Trace, null, null!,
                                                           null!, 0,
                                                           null!, 0.0,
                                                           null!, (string)null!));
    }

    [Fact]
    public void Log_NullArgsAndProperties_NotThrowAnyException()
    {
        // Act & Assert
        Should.NotThrow(() => NullOpenUrzednikLogger.Instance.Log(OpenUrzednikLogLevel.Trace, null, null!, null!));
    }
}
