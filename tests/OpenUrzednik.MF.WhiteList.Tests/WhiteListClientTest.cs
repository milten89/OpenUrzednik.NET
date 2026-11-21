using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.MF.WhiteList.Tests;

// Shared helpers; tests are in one file per method. Identifiers in requests are fake, with valid check digits, and their digit runs
// appear in no fixture, so the leak checks can't match the answer's own data.
public sealed partial class WhiteListClientTest : IDisposable
{
    private const string Nip = "7368125416";
    private const string Regon = "593728162";
    private const string BankAccount = "61109010140000071219812874";

    // 12:00 on 2026-11-20 in Warsaw (CET): "today" for the date checks, so the earliest date is 2021-01-01.
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Today = new(2026, 11, 20);
    private static readonly DateTime MinDate = new(2021, 1, 1);
    private static readonly DateTime Date = new(2026, 11, 2);

    // search-nip.json: the test host's fictitious subject for NIP 3245174504, its empty bases sent as "".
    private static readonly Subject TestHostSubject = new("Nazwa Firmy 1")
    {
        Nip = "3245174504",
        VatStatus = VatStatus.Active,
        Regon = "79156739856513",
        Krs = "1234567890",
        ResidenceAddress = "TAKA a Taka 191/369, 15-360 Miejscowość 1",
        RegistrationLegalDate = new DateTime(2017, 11, 26),
        RemovalDate = new DateTime(2017, 10, 18),
        RemovalBasis = "Ustawa o podatku od towarów i usług Art. 196",
        HasVirtualAccounts = false,
    };

    // search-regon.json and search-bank-account.json: the Ministry of Finance, a legal person, from production.
    private static readonly Subject MinistryOfFinance = new("MINISTERSTWO FINANSÓW")
    {
        Nip = "5260250274",
        VatStatus = VatStatus.Active,
        Regon = "000002217",
        ResidenceAddress = "ŚWIĘTOKRZYSKA 12, 00-916 WARSZAWA",
        RegistrationLegalDate = new DateTime(1996, 1, 1),
        AccountNumbers =
        [
            "59101010100075912222000000", "10101010100038252231000000", "49101010100038122225000000", "60101010100038252230000000",
            "09101010100075912223000000", "12101010100075912221000000", "18101010100046042231000000", "18101014690032611391200000",
            "64101010100075911339200000", "56113010170020117278200033", "07113010170200000000138215", "64101010100037242231000000",
            "06101010100075912225000000", "41101010100071712231000000", "03101010100075912227000000", "59113010880001312263200002",
            "78113010170200000000137228",
        ],
        HasVirtualAccounts = true,
    };

    // A day too late and a day too early, also with a time of day (only the date counts).
    public static TheoryData<DateTime> DatesOutOfRange => [Today.AddDays(1), Today.AddDays(1).AddHours(1), MinDate.AddDays(-1), MinDate.AddDays(-1).AddHours(23)];

    // The bounds themselves, also with a time of day.
    public static TheoryData<DateTime> DatesInRange => [Today, Today.AddHours(23).AddMinutes(59), MinDate, MinDate.AddHours(12)];

    // Fixture, status, error.type: success, not on the list and the API's error answers (ADR-0025).
    public static TheoryData<string, int, string?> MetricCases(string success, string notFound) => new()
    {
        { success, 200, null },
        { notFound, 200, NotFoundError.ErrorCode },
        { "error-429-wl-191.from-error-table.json", 429, RateLimitExceededError.ErrorCode },
        { "error-400-wl-118.json", 400, BadRequestError.ErrorCode },
        { "error-400-wl-195.from-error-table.json", 400, ServiceUnavailableError.ErrorCode },
        { "error-500-wl-197.from-error-table.json", 500, UnknownError.ErrorCode },
    };

    // Status and body of every kind of answer; {id} is replaced with the identifier the test sends, as an API that echoes it would.
    public static TheoryData<int, string> LeakAnswers(string success, string notFound) => new()
    {
        { 200, WhiteListFixtures.Load(success) },
        { 200, WhiteListFixtures.Load(notFound) },
        { 400, """{"message":"Nieprawidłowy identyfikator {id}.","code":"WL-115"}""" },
        { 429, """{"message":"Limit dla {id} wyczerpany.","code":"WL-191"}""" },
        { 500, """{"message":"Błąd dla {id}.","code":"WL-197"}""" },
        { 503, """{"message":"Błąd dla {id}.","code":"WL-100"}""" },
        { 404, "Not found: {id}" },
        { 200, "{" },
    };

    private readonly List<HttpClient> _httpClients = [];

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
    }

    private WhiteListClient CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler, IOpenUrzednikLogger? logger = null,
        IOpenUrzednikTraceSource? traceSource = null, IOpenUrzednikMetrics? metrics = null)
    {
        handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new WhiteListClient(httpClient, null, new FakeTimeProvider(Now),
            new OpenUrzednikTelemetry { Logger = logger, TraceSource = traceSource, Metrics = metrics });
    }

    private WhiteListClient CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler, RecordingTelemetry telemetry)
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
}
