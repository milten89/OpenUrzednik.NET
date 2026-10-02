using Microsoft.Extensions.Logging;

using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

// ADR-0003 confirmation: entries a real client writes reach the ILogger.
// End-to-end through NbpGoldPriceClient, so not split into one file per method.
public sealed class NbpClientLoggingTest
{
    [Fact]
    public async Task GetTopCountAsync_ValidationFails_LogsDebugEntry()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage()));
        var sut = new NbpGoldPriceClient(httpClient, logger: new OpenUrzednikLogger(logger));

        // Act
        await sut.GetTopCountAsync(0, TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.Message.ShouldBe("Validation failed for GetTopCountAsync");
        entry.State.ShouldBe([new("operation", "GetTopCountAsync"), new("{OriginalFormat}", "Validation failed for {operation}")]);
    }

    [Fact]
    public async Task GetLatestAsync_ConnectionFails_LogsWarningWithException()
    {
        // Arrange
        var logger = new RecordingLogger();
        var thrown = new HttpRequestException("Connection refused");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(thrown));
        var sut = new NbpGoldPriceClient(httpClient, logger: new OpenUrzednikLogger(logger));

        // Act
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.Where(e => e.Level == LogLevel.Warning).ShouldHaveSingleItem();
        entry.Message.ShouldBe("NBP API request to cenyzlota failed");
        entry.Exception.ShouldBeSameAs(thrown);
    }
}
