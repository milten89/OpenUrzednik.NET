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

// Shared helpers and the telemetry tests of the two checks (ADR-0023, ADR-0025): the identifier and the account number only masked, the
// date as an ISO date; a check has no not-on-the-list outcome. The tests of each method are in one file per method.
public sealed partial class WhiteListClientTest
{
    // The answers of check-nip-not-assigned.json and check-nip-assigned.json: 18:14:16 and 18:14:17 on 2026-11-21 in Warsaw (CET).
    private static readonly DateTimeOffset CheckAnswered = new(2026, 11, 21, 18, 14, 16, TimeSpan.FromHours(1));
    private static readonly DateTimeOffset CheckAnsweredLater = new(2026, 11, 21, 18, 14, 17, TimeSpan.FromHours(1));

    // The checks: method, span name, identifier tag, its masked value, display path of the HTTP span.
    public static TheoryData<string, string, string, string, string> CheckMethods => new()
    {
        { "nip", "mf_white_list.check.nip_bank_account", "mf_white_list.nip", "******5416", "/api/check/nip/{nip}/bank-account/{account}" },
        { "regon", "mf_white_list.check.regon_bank_account", "mf_white_list.regon", "*****8162", "/api/check/regon/{regon}/bank-account/{account}" },
    };

    // Fixture, status, error.type of a check: the answers and the API's error answers (ADR-0025); no not-found among them.
    public static TheoryData<string, int, string?> CheckMetricCases(string assigned, string notAssigned) => new()
    {
        { assigned, 200, null },
        { notAssigned, 200, null },
        { "error-429-wl-191.from-error-table.json", 429, RateLimitExceededError.ErrorCode },
        { "error-400-wl-115.json", 400, BadRequestError.ErrorCode },
        { "error-400-wl-195.from-error-table.json", 400, ServiceUnavailableError.ErrorCode },
        { "error-500-wl-197.from-error-table.json", 500, UnknownError.ErrorCode },
    };

    // Status and body of every kind of answer to a check; {id} is replaced with the identifier and the account the test sends, as an API
    // that echoes them would.
    public static TheoryData<int, string> CheckLeakAnswers(string assigned, string notAssigned) => new()
    {
        { 200, WhiteListFixtures.Load(assigned) },
        { 200, WhiteListFixtures.Load(notAssigned) },
        { 200, """{"result":{"accountAssigned":"{id}","requestDateTime":"21-11-2026 18:14:16","requestId":"wAaOW-99b2nhg"}}""" },
        { 400, """{"message":"Nieprawidłowy identyfikator {id}.","code":"WL-115"}""" },
        { 429, """{"message":"Limit dla {id} wyczerpany.","code":"WL-191"}""" },
        { 500, """{"message":"Błąd dla {id}.","code":"WL-197"}""" },
        { 503, """{"message":"Błąd dla {id}.","code":"WL-100"}""" },
        { 404, "Not found: {id}" },
        { 200, "{" },
    };

    [Theory]
    [MemberData(nameof(CheckMethods))]
    public async Task Check_Always_StartsTheSpanWithTheMaskedIdentifierAccountAndDate(string method, string spanName, string tag, string masked, string displayPath)
    {
        // Arrange
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound), out _, traceSource: tracer);

        // Act
        await CallCheckAsync(sut, method, Date);

        // Assert
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
        span.Received(1).SetTag("mf_white_list.bank_account", "**********************2874");
        span.Received(1).SetTag("mf_white_list.date", "2026-11-02");
        span.Received(1).SetTag("url.path", displayPath);
        span.Received(2).Dispose(); // the operation span and its HTTP child (one substitute for both)
    }

    [Theory]
    [MemberData(nameof(CheckMethods))]
    public async Task Check_SpanNotRecording_DoesNotMaskForATag(string method, string spanName, string tag, string masked, string displayPath)
    {
        // Arrange: a span nobody records gets no masked values, so the call doesn't build them (#345)
        var (_, span, tracer) = CreateTelemetrySubstitutes();
        span.IsRecording.Returns(false);
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound), out _, traceSource: tracer);

        // Act
        await CallCheckAsync(sut, method, Date);

        // Assert
        tracer.Received(1).StartSpan(spanName);
        span.DidNotReceive().SetTag(tag, Arg.Any<string>());
        span.DidNotReceive().SetTag("mf_white_list.bank_account", Arg.Any<string>());
        span.DidNotReceive().SetTag(Arg.Any<string>(), masked);
        span.DidNotReceive().SetTag("url.path", displayPath);
    }

    [Theory]
    [MemberData(nameof(CheckMethods))]
    public async Task Check_NotAssigned_IsASuccessWithNoNotFoundLoggedOrRecorded(string method, string spanName, string tag, string masked, string displayPath)
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(Fixture(method == "nip" ? "check-nip-not-on-list.json" : "check-regon-not-assigned.json"), out _, logger, tracer);

        // Act
        var error = await CallCheckAsync(sut, method, Date);

        // Assert
        error.ShouldBeNull();
        span.DidNotReceive().SetTag("error.type", Arg.Any<string>());
        logger.DidNotReceive().Log(Arg.Any<OpenUrzednikLogLevel>(), WhiteListEvents.NotOnList, Arg.Any<Exception?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>());
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
        span.Received(1).SetTag("url.path", displayPath);
    }

    [Theory]
    [MemberData(nameof(CheckMethods))]
    public async Task Check_InvalidDate_LogsValidationFailedWithTheMethodName(string method, string spanName, string tag, string masked, string displayPath)
    {
        // Arrange
        var (logger, span, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK), out var handler, logger, tracer);

        // Act
        await CallCheckAsync(sut, method, MinDate.AddDays(-1));

        // Assert
        logger.Received(1).Log(OpenUrzednikLogLevel.Debug, WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}",
            "operation", method == "nip" ? "CheckNipBankAccountAsync" : "CheckRegonBankAccountAsync");
        span.Received(1).SetTag("error.type", ValidationError.ErrorCode);
        handler.Request.ShouldBeNull();
        tracer.Received(1).StartSpan(spanName);
        span.Received(1).SetTag(tag, masked);
        span.DidNotReceive().SetTag("url.path", displayPath);
    }

#if NET
    [Theory]
    [MemberData(nameof(CheckMethods))]
    public async Task Check_DefaultMetrics_RecordOnTheWhiteListMeterWithoutIdentifiers(string method, string spanName, string tag, string masked, string displayPath)
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
        await CallCheckAsync(sut, method, Date);

        // Assert
        var (instrumentName, recordedTags) = recorded.ShouldHaveSingleItem();
        instrumentName.ShouldBe("openurzednik.client.operation.duration");
        recordedTags["openurzednik.operation"].ShouldBe(spanName);
        recordedTags["openurzednik.provider"].ShouldBe("mf_white_list");
        recordedTags["error.type"].ShouldBe(RateLimitExceededError.ErrorCode);
        recordedTags.Values.ShouldNotContain(masked);
        recordedTags.Keys.ShouldNotContain(tag);
        recordedTags.Keys.ShouldNotContain("mf_white_list.bank_account");
        recordedTags.Values.ShouldAllBe(value => !$"{value}".Contains(displayPath));
    }
#endif

    // Calls one check with the test's identifier and account; returns the first error, or null on success.
    private static async Task<OpenUrzednikError?> CallCheckAsync(WhiteListClient sut, string method, DateTime date)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var errors = method == "nip"
            ? (await sut.CheckNipBankAccountAsync(Nip, BankAccount, date, cancellationToken)).Errors
            : (await sut.CheckRegonBankAccountAsync(Regon, BankAccount, date, cancellationToken)).Errors;
        return errors.Count == 0 ? null : errors[0];
    }
}
