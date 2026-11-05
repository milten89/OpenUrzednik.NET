// Native AOT smoke test (#118): runs the published libraries as native code, offline. A stub handler answers with bodies
// captured from the real NBP API; each check prints PASS or FAIL, and any failure makes the exit code 1.
using System.Diagnostics;
using System.Net;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Diagnostics;
using OpenUrzednik.Extensions.Logging;
using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Table;

var failures = 0;

void Check(string name, bool ok, string detail = "")
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}{(ok || detail.Length == 0 ? "" : $": {detail}")}");
    if (!ok)
        failures++;
}

string Describe<T>(OpenUrzednikResult<T> result) =>
    result.IsSuccess ? $"value {result.Value}" : string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Message}"));

// Spans: the shared OpenUrzednik.Nbp source must reach a listener.
var spans = new List<string>();
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == NbpTelemetry.SourceName,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStopped = activity => { lock (spans) spans.Add(activity.OperationName); },
};
ActivitySource.AddActivityListener(listener);

// Logs: a provider that formats every entry and reads its structured state, so the logging path runs as native code too.
var logs = new RecordingLoggerProvider();
using var loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(logs).SetMinimumLevel(LogLevel.Debug));

// 1. The clients without DI, with the logging and tracing adapters.
using (var httpClient = new HttpClient(new FixtureHandler()))
{
    var gold = new NbpGoldPriceClient(httpClient, null, null,
        loggerFactory.CreateOpenUrzednikLogger<NbpGoldPriceClient>(), ActivityTraceSource.GetShared(NbpTelemetry.SourceName));
    var goldLatest = await gold.GetLatestAsync();
    Check("gold: latest price", goldLatest.IsSuccess && goldLatest.Value.Price == 517.55m, Describe(goldLatest));

    var rates = new NbpCurrencyExchangeRateClient(httpClient, null, null,
        loggerFactory.CreateOpenUrzednikLogger<NbpCurrencyExchangeRateClient>());
    var usd = await rates.GetLatestAsync("USD");
    Check("currency: USD mid rate", usd.IsSuccess && usd.Value.Rates[0].Price == 3.8762m, Describe(usd));
    var usdBuySell = await rates.GetBuySellLatestAsync("USD");
    Check("currency: USD bid/ask", usdBuySell.IsSuccess && usdBuySell.Value.Rates[0].Ask == 3.9115m, Describe(usdBuySell));

    var tables = new NbpExchangeRateTableClient(httpClient);
    var tableA = await tables.GetLatestAsync(TableType.A);
    Check("table: A has USD", tableA.IsSuccess && tableA.Value.Rates.Any(r => r.CurrencyCode == "USD"), Describe(tableA));
    var tableC = await tables.GetBuySellLatestAsync();
    Check("table: C has USD", tableC.IsSuccess && tableC.Value.Rates.Any(r => r.CurrencyCode == "USD"), Describe(tableC));

    var invalid = await rates.GetTopCountAsync("USD", 0);
    Check("validation: returned as ValidationError", invalid.IsFailure && invalid.Errors[0] is ValidationError, Describe(invalid));
    Check("logging: entries with an event id, a message and structured state",
        logs.Entries.Any(e => e.EventId.Id != 0 && e.Message.Length > 0 && e.StateKeys.Contains("{OriginalFormat}")),
        string.Join("; ", logs.Entries.Select(e => $"{e.EventId} {e.Message}")));
}

// 2. Error paths: a 404 and a body that isn't JSON.
using (var httpClient = new HttpClient(new FixtureHandler(HttpStatusCode.NotFound, "error-404.txt")))
{
    var notFound = await new NbpGoldPriceClient(httpClient).GetLatestAsync();
    Check("error: 404 is NotFoundError with the status code",
        notFound.IsFailure && notFound.Errors[0] is NotFoundError && notFound.Errors[0].GetHttpStatusCode() == 404, Describe(notFound));
    try
    {
        notFound.EnsureSuccess();
        Check("error: EnsureSuccess throws NotFoundException", false, "no exception");
    }
    catch (NotFoundException)
    {
        Check("error: EnsureSuccess throws NotFoundException", true);
    }
}

using (var httpClient = new HttpClient(new FixtureHandler(HttpStatusCode.OK, body: "{ not json")))
{
    var malformed = await new NbpGoldPriceClient(httpClient).GetLatestAsync();
    Check("error: malformed JSON is a serialization error",
        malformed.IsFailure && malformed.Errors[0].Code == SerializationError.ErrorCode, Describe(malformed));
}

// 3. Dependency injection: the default and a keyed registration, with logging.
var services = new ServiceCollection();
services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Debug));
services.AddOpenUrzednikNbp().ConfigurePrimaryHttpMessageHandler(() => new FixtureHandler());
services.AddKeyedOpenUrzednikNbp("gateway", options => options.ApiUrl = "https://gateway.example.com/nbp/")
    .ConfigurePrimaryHttpMessageHandler(() => new FixtureHandler());
using (var provider = services.BuildServiceProvider())
{
    var diGold = await provider.GetRequiredService<INbpGoldPriceClient>().GetLatestAsync();
    Check("di: default client", diGold.IsSuccess, Describe(diGold));
    var keyedRates = await provider.GetRequiredKeyedService<INbpCurrencyExchangeRateClient>("gateway").GetLatestAsync("USD");
    Check("di: keyed client", keyedRates.IsSuccess, Describe(keyedRates));
}

lock (spans)
    Check("tracing: spans reach the OpenUrzednik.Nbp source", spans.Contains("nbp.gold.latest"), string.Join(", ", spans));

Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
return failures == 0 ? 0 : 1;

internal sealed record LogEntry(EventId EventId, string Message, IReadOnlyList<string> StateKeys);

// Keeps every entry: the formatted message and the keys of the structured state.
internal sealed class RecordingLoggerProvider : ILoggerProvider, ILogger
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_entries) return [.. _entries]; }
    }

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var keys = state is IReadOnlyList<KeyValuePair<string, object?>> values ? values.Select(v => v.Key).ToList() : [];
        lock (_entries)
            _entries.Add(new LogEntry(eventId, formatter(state, exception), keys));
    }

    public void Dispose() { }
}

// Answers by request path with an embedded fixture, or with a fixed status and body.
internal sealed class FixtureHandler(HttpStatusCode? status = null, string? resource = null, string? body = null) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath.TrimEnd('/');
        var name = resource ?? path switch
        {
            _ when path.EndsWith("/cenyzlota", StringComparison.Ordinal) => "gold-latest.json",
            _ when path.EndsWith("/rates/a/USD", StringComparison.OrdinalIgnoreCase) => "rates-a-latest.json",
            _ when path.EndsWith("/rates/c/USD", StringComparison.OrdinalIgnoreCase) => "rates-c-latest.json",
            _ when path.EndsWith("/tables/a", StringComparison.OrdinalIgnoreCase) => "tables-a-latest.json",
            _ when path.EndsWith("/tables/c", StringComparison.OrdinalIgnoreCase) => "tables-c-latest.json",
            _ => null,
        };
        var content = body ?? (name is null ? null : Read(name));
        return Task.FromResult(content is null
            ? new HttpResponseMessage(HttpStatusCode.NotImplemented) { RequestMessage = request, Content = new StringContent($"No fixture for {path}") }
            : new HttpResponseMessage(status ?? HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(content) });
    }

    private static string Read(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing embedded fixture {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
