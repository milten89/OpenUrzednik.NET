using System.Net;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

// ADR-0023, ADR-0025 confirmation: entries a real WL2 status client writes reach the ILogger, and no message or state carries the NIP.
// End-to-end through WhiteListStatusClient, so not split into one file per method.
public sealed class WhiteListStatusClientLoggingTest
{
    private const string Nip = "7368125416";
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));
    private static readonly DateTime Date = new(2026, 11, 2);

    private static WhiteListStatusClient CreateSut(HttpClient httpClient, RecordingLogger logger)
        => new(httpClient, null, Clock, new OpenUrzednikTelemetry { Logger = new OpenUrzednikLogger(logger) });

    private static HttpResponseMessage Answer(string statusCode, string status)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$$$"""{"result":{"subject":{"nip":"{{{{Nip}}}}","status":"{{{{status}}}} {{{{Nip}}}}","statusCode":"{{{{statusCode}}}}","statusVat":null,"requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"MdAhc-99b3aea"}}}""",
                Encoding.UTF8, "application/json"),
        };

    private static void ShouldNotCarryTheNip(RecordingLogger logger)
        => logger.Entries.ShouldAllBe(e => !e.Message.Contains("68125") && e.State.All(item => !$"{item.Value}".Contains("68125")));

    [Fact]
    public async Task GetVatStatusByNipAsync_DateOutOfRange_LogsDebugEntry()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage()));

        // Act
        await CreateSut(httpClient, logger).GetVatStatusByNipAsync(Nip, new DateTime(2020, 12, 31), TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.EventId.ShouldBe(new EventId(WhiteListLogEvents.ValidationFailed, nameof(WhiteListLogEvents.ValidationFailed)));
        entry.Message.ShouldBe("Validation failed for GetVatStatusByNipAsync");
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NotInTheRegister_LogsNotOnListAtDebugWithoutTheNip()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Answer("B", "Brak")));

        // Act
        await CreateSut(httpClient, logger).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.Where(e => e.EventId.Id == WhiteListLogEvents.NotOnList).ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.EventId.Name.ShouldBe(nameof(WhiteListLogEvents.NotOnList));
        entry.Message.ShouldBe("MF White List API has no entity on the list for GetVatStatusByNipAsync");
        logger.Entries.ShouldNotContain(e => e.EventId.Id == WhiteListLogEvents.QueryRejected);
        ShouldNotCarryTheNip(logger);
    }

    [Theory]
    [InlineData("I", "Błąd zapytania")]
    [InlineData("D", "Data spoza zakresu")]
    [InlineData("X", "Usługa nieaktywna")]
    public async Task GetVatStatusByNipAsync_RejectedOrInactive_LogsQueryRejectedAtWarningWithTheCodeOnly(string code, string status)
    {
        // Arrange: the API's text echoes the NIP, as it may
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Answer(code, status)));

        // Act
        await CreateSut(httpClient, logger).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var entry = logger.Entries.Where(e => e.EventId.Id == WhiteListLogEvents.QueryRejected).ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Name.ShouldBe(nameof(WhiteListLogEvents.QueryRejected));
        entry.Message.ShouldBe($"MF White List API rejected GetVatStatusByNipAsync with code {code}");
        logger.Entries.ShouldNotContain(e => e.EventId.Id == WhiteListLogEvents.NotOnList);
        ShouldNotCarryTheNip(logger);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Status_LogsNothing()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Answer("C", "Czynny")));

        // Act
        var result = await CreateSut(httpClient, logger).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_BadRequestEchoingTheNip_LogsNothingUnmasked()
    {
        // Arrange
        var logger = new RecordingLogger();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent($$"""{"message":"Nieprawidłowy {{Nip}}.","code":"WL-115"}""", Encoding.UTF8, "application/json"),
        }));

        // Act
        await CreateSut(httpClient, logger).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.EventId.Id == WhiteListLogEvents.ErrorResponse);
        ShouldNotCarryTheNip(logger);
    }
}
