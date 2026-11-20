using System.Net;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

// ADR-0023, ADR-0025 confirmation: entries a real White List client writes reach the ILogger, and no message or state carries the
// identifier. End-to-end through WhiteListClient, so not split into one file per method.
public sealed class WhiteListClientLoggingTest
{
    private const string Nip = "7368125416";
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));

    private static WhiteListClient CreateSut(HttpClient httpClient, RecordingLogger logger)
        => new(httpClient, null, Clock, new OpenUrzednikTelemetry { Logger = new OpenUrzednikLogger(logger) });

    [Fact]
    public async Task SearchByNipAsync_DateOutOfRange_LogsDebugEntry()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage()));

        // Act
        await CreateSut(httpClient, logger).SearchByNipAsync(Nip, new DateTime(2020, 12, 31), TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.EventId.ShouldBe(new EventId(WhiteListLogEvents.ValidationFailed, nameof(WhiteListLogEvents.ValidationFailed)));
        entry.Message.ShouldBe("Validation failed for SearchByNipAsync");
    }

    [Fact]
    public async Task SearchByNipAsync_NotOnTheList_LogsNotOnListWithoutTheNip()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"result":{"subject":null,"requestDateTime":"20-11-2026 22:44:25","requestId":"rd9zq-99alll1"}}""",
                Encoding.UTF8, "application/json"),
        }));

        // Act
        await CreateSut(httpClient, logger).SearchByNipAsync(Nip, new DateTime(2026, 11, 2), TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.Where(e => e.EventId.Id == WhiteListLogEvents.NotOnList).ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.EventId.Name.ShouldBe(nameof(WhiteListLogEvents.NotOnList));
        entry.Message.ShouldBe("MF White List API has no entity on the list for SearchByNipAsync");
        entry.State.ShouldBe([new("operation", "SearchByNipAsync"), new("{OriginalFormat}", "MF White List API has no entity on the list for {operation}")]);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("68125") && e.State.All(item => !$"{item.Value}".Contains("68125")));
    }

    [Fact]
    public async Task SearchByNipAsync_BadRequestEchoingTheNip_LogsWarningWithTheNipMasked()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent($$"""{"message":"Nieprawidłowy NIP {{Nip}}.","code":"WL-115"}""", Encoding.UTF8, "application/json"),
        }));

        // Act
        await CreateSut(httpClient, logger).SearchByNipAsync(Nip, new DateTime(2026, 11, 2), TestContext.Current.CancellationToken);

        // Assert
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.EventId.Id == WhiteListLogEvents.ErrorResponse);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains("68125") && e.State.All(item => !$"{item.Value}".Contains("68125")));
    }
}
