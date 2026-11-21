using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

// Shared helpers and the constructor tests; the tests of the method are in WhiteListStatusClientTest.GetVatStatusByNipAsync.cs. The NIP in
// requests is fake, with a valid check digit, and its digit run appears in no fixture, so the leak checks can't match the answer's own data.
public sealed partial class WhiteListStatusClientTest : IDisposable
{
    private const string Nip = "7368125416";

    // 12:00 on 2026-11-20 in Warsaw (CET): "today" for the date checks, so the earliest date is 2021-01-01.
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Today = new(2026, 11, 20);
    private static readonly DateTime MinDate = new(2021, 1, 1);
    private static readonly DateTime Date = new(2026, 11, 2);

    // The captured answers (status-nip-*.json): 19:58:34 on 2026-11-21 in Warsaw (CET).
    private static readonly DateTimeOffset Answered = new(2026, 11, 21, 19, 58, 34, TimeSpan.FromHours(1));

    // A day too late and a day too early, also with a time of day (only the date counts).
    public static TheoryData<DateTime> DatesOutOfRange => [Today.AddDays(1), Today.AddDays(1).AddHours(1), MinDate.AddDays(-1), MinDate.AddDays(-1).AddHours(23)];

    // The bounds themselves, also with a time of day.
    public static TheoryData<DateTime> DatesInRange => [Today, Today.AddHours(23).AddMinutes(59), MinDate, MinDate.AddHours(12)];

    private readonly List<HttpClient> _httpClients = [];

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
    }

    private WhiteListStatusClient CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler, IOpenUrzednikLogger? logger = null,
        IOpenUrzednikTraceSource? traceSource = null, IOpenUrzednikMetrics? metrics = null)
    {
        handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new WhiteListStatusClient(httpClient, null, new FakeTimeProvider(Now),
            new OpenUrzednikTelemetry { Logger = logger, TraceSource = traceSource, Metrics = metrics });
    }

    private WhiteListStatusClient CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler, RecordingTelemetry telemetry)
        => CreateSut(response, out handler, telemetry, telemetry);

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Fixture(string name)
        => Json(HttpStatusCode.OK, WhiteListFixtures.Load(name));

    private static HttpResponseMessage Fixture(HttpStatusCode statusCode, string name)
        => Json(statusCode, WhiteListFixtures.Load(name));

    private static (IOpenUrzednikLogger Logger, IOpenUrzednikSpan Span, IOpenUrzednikTraceSource Tracer) CreateTelemetrySubstitutes()
    {
        var logger = Substitute.For<IOpenUrzednikLogger>();
        logger.IsEnabled(Arg.Any<OpenUrzednikLogLevel>()).Returns(true);
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan(Arg.Any<string>()).Returns(span);
        return (logger, span, tracer);
    }

    [Fact]
    public void Ctor_NullHttpClient_Throws()
    {
        // Act
        var exception = Should.Throw<ArgumentNullException>(() => new WhiteListStatusClient(null!));

        // Assert
        exception.ParamName.ShouldBe("httpClient");
    }

    [Theory]
    [InlineData("http://wykazpodatnikowvat.mf.gov.pl/")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api2/")]
    [InlineData("https://wykazpodatnikowvat.mf.gov.pl/api/")]
    public void Ctor_InvalidApiUrl_Throws(string apiUrl)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act & Assert
        Should.Throw<ArgumentException>(() => new WhiteListStatusClient(httpClient, new WhiteListOptions { ApiUrl = apiUrl }));
    }

    [Fact]
    public void Ctor_NonPositiveTimeout_Throws()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act & Assert
        Should.Throw<ArgumentException>(() => new WhiteListStatusClient(httpClient, new WhiteListOptions { Timeout = TimeSpan.Zero }));
    }

    [Fact]
    public async Task Ctor_HttpClientOnly_SendsToTheProductionHostUnderApi2()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(Fixture("status-nip-active-no-date.json"));
        using var httpClient = new HttpClient(handler);
        var sut = new WhiteListStatusClient(httpClient);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri($"https://wykazpodatnikowvat.mf.gov.pl/api2/status/nip/{Nip}"));
    }

    [Fact]
    public async Task Ctor_TestApiUrl_SendsToTheTestHostUnderApi2()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(Fixture("status-nip-active-no-date.json"));
        using var httpClient = new HttpClient(handler);
        var sut = new WhiteListStatusClient(httpClient, new WhiteListOptions { ApiUrl = WhiteListOptions.TestApiUrl });

        // Act
        await sut.GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri($"https://wykazpodatnikowvat-test.mf.gov.pl/api2/status/nip/{Nip}"));
    }

    [Fact]
    public async Task Ctor_BaseAddressOnTheHttpClient_IsTheHostRoot()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(Fixture("status-nip-active-no-date.json"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var sut = new WhiteListStatusClient(httpClient);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri($"https://example.test/api2/status/nip/{Nip}"));
    }

    [Fact]
    public async Task Ctor_TimeProvider_DecidesToday()
    {
        // Arrange: 23:30 on 2026-11-20 in UTC is already 2026-11-21 in Warsaw, so 2026-11-21 isn't in the future there
        var handler = new StubHttpMessageHandler(Fixture("status-nip-active.json"));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 23, 30, 0, TimeSpan.Zero));
        var sut = new WhiteListStatusClient(httpClient, null, clock);

        // Act
        var result = await sut.GetVatStatusByNipAsync(Nip, new DateTime(2026, 11, 21), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Ctor_TelemetryWithLogger_LogsThroughIt()
    {
        // Arrange
        var (logger, _, tracer) = CreateTelemetrySubstitutes();
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK), out _, logger, tracer);

        // Act
        await sut.GetVatStatusByNipAsync(Nip, Today.AddDays(1), TestContext.Current.CancellationToken);

        // Assert
        logger.Received().Log(OpenUrzednikLogLevel.Debug, MF.WhiteList.Common.WhiteListEvents.ValidationFailed, null, "Validation failed for {operation}",
            "operation", "GetVatStatusByNipAsync");
    }
}
