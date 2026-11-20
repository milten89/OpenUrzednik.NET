#if NET
using System.Diagnostics.Metrics;
#endif
using System.Net;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList.Common;
#if NET
using OpenUrzednik.TestCommon;
#endif

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

// Spans, tags and log events of every method (ADR-0023, ADR-0025): identifiers only masked, the date as an ISO date.
public sealed partial class WhiteListClientTest
{
    public static TheoryData<string, string, string, string> Methods => new()
    {
        { "nip", "mf_white_list.search.nip", "mf_white_list.nip", "******5416" },
        { "regon", "mf_white_list.search.regon", "mf_white_list.regon", "*****8162" },
        { "bankAccount", "mf_white_list.search.bank_account", "mf_white_list.bank_account", "**********************2874" },
    };

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task SearchAsync_Always_StartsTheSpanWithTheMaskedIdentifierAndTheDate(string method, string spanName, string tag, string masked)
    {
        // Arrange
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound), out _, traceSource: tracer);

        // Act
        await CallAsync(sut, method, Date);

        // Assert
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
        span.Received(1).SetTag("mf_white_list.date", "2026-11-02");
        span.Received(2).Dispose(); // the operation span and its HTTP child (one substitute for both)
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task SearchAsync_NotOnTheList_LogsNotOnListAndRecordsNotFoundOnTheSpan(string method, string spanName, string tag, string masked)
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var notFound = method == "bankAccount" ? "search-bank-account-not-found.json" : "search-nip-not-found.json";
        var sut = CreateSut(Fixture(notFound), out _, logger, tracer);

        // Act
        var result = await CallAsync(sut, method, Date);

        // Assert
        result.ShouldBeOfType<WhiteListNotFoundError>();
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.NotOnList, null, "MF White List API has no entity on the list for {operation}",
            "operation", OperationOf(method));
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task SearchAsync_InvalidDate_LogsValidationFailedWithTheMethodName(string method, string spanName, string tag, string masked)
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK), out var handler, logger, tracer);

        // Act
        await CallAsync(sut, method, MinDate.AddDays(-1));

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}",
            "operation", OperationOf(method));
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        handler.Request.ShouldBeNull();
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task SearchAsync_Success_WritesTheRouteTemplateToTheHttpSpan(string method, string spanName, string tag, string masked)
    {
        // Arrange
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var success = method switch
        {
            "nip" => "search-nip.json",
            "regon" => "search-regon.json",
            _ => "search-bank-account.json",
        };
        var sut = CreateSut(Fixture(success), out _, traceSource: tracer);

        // Act
        var result = await CallAsync(sut, method, Date);

        // Assert
        result.ShouldBeNull();
        span.Received(1).SetTag("url.path", method switch
        {
            "nip" => "/api/search/nip/{nip}",
            "regon" => "/api/search/regon/{regon}",
            _ => "/api/search/bank-account/{account}",
        });
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
    }

#if NET
    [Theory]
    [MemberData(nameof(Methods))]
    public async Task SearchAsync_DefaultMetrics_RecordOnTheWhiteListMeter(string method, string spanName, string tag, string masked)
    {
        // Arrange: a client without metrics records on the shared OpenUrzednik.MF.WhiteList meter on .NET; other tests record on it at
        // the same time, so this one keeps only the measurements of its own host
        var host = $"{Guid.NewGuid():N}.example.com";
        var recorded = new List<(string Instrument, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "OpenUrzednik.MF.WhiteList") // the documented meter name, stable within a major
                    l.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            var copy = new Dictionary<string, object?>();
            foreach (var item in tags)
                copy[item.Key] = item.Value;
            if (Equals(copy.GetValueOrDefault("server.address"), host))
            {
                lock (recorded)
                    recorded.Add((instrument.Name, copy));
            }
        });
        listener.Start();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Fixture((HttpStatusCode)429, "error-429-wl-191.from-error-table.json")));
        var sut = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = $"https://{host}/" },
            new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Now));

        // Act
        await CallAsync(sut, method, Date);

        // Assert
        var (instrumentName, recordedTags) = recorded.ShouldHaveSingleItem();
        instrumentName.ShouldBe("openurzednik.client.operation.duration");
        recordedTags["openurzednik.operation"].ShouldBe(spanName);
        recordedTags["openurzednik.provider"].ShouldBe("mf_white_list");
        recordedTags["error.type"].ShouldBe(RateLimitExceededError.ErrorCode);
        recordedTags.Values.ShouldNotContain(masked);
        recordedTags.Keys.ShouldNotContain(tag);
    }
#endif

    private static string OperationOf(string method) => method switch
    {
        "nip" => "SearchByNipAsync",
        "regon" => "SearchByRegonAsync",
        _ => "SearchByBankAccountAsync",
    };

    // Calls one method with the test's identifier; returns the first error, or null on success.
    private static async Task<OpenUrzednikError?> CallAsync(WhiteListClient sut, string method, DateTime date)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var errors = method switch
        {
            "nip" => (await sut.SearchByNipAsync(Nip, date, cancellationToken)).Errors,
            "regon" => (await sut.SearchByRegonAsync(Regon, date, cancellationToken)).Errors,
            _ => (await sut.SearchByBankAccountAsync(BankAccount, date, cancellationToken)).Errors,
        };
        return errors.Count == 0 ? null : errors[0];
    }
}
