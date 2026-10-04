using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Telemetry;

public partial class NullOpenUrzednikLoggerTest
{
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
