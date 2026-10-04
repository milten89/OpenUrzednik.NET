// A minimal API over the NBP clients registered with OpenUrzednik.Nbp.DependencyInjection.
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Table;

var builder = WebApplication.CreateBuilder(args);

// Registers the three NBP clients on one named HttpClient; logs go to the app's ILoggerFactory.
// The standard resilience handler (Microsoft.Extensions.Http.Resilience) adds retries, timeouts and a circuit breaker;
// leave NbpOptions.Timeout unset with it, or that deadline cuts the retries short.
builder.Services.AddOpenUrzednikNbp()
    .AddStandardResilienceHandler();

var app = builder.Build();

app.MapGet("/gold/latest", async (INbpGoldPriceClient gold, CancellationToken cancellationToken)
    => ToHttpResult(await gold.GetLatestAsync(cancellationToken)));

app.MapGet("/rates/{currency}", async (string currency, int? last, INbpCurrencyExchangeRateClient rates, CancellationToken cancellationToken)
    => ToHttpResult(await rates.GetTopCountAsync(currency, last ?? 1, cancellationToken: cancellationToken)));

app.MapGet("/tables/c/latest", async (INbpExchangeRateTableClient tables, CancellationToken cancellationToken)
    => ToHttpResult(await tables.GetBuySellLatestAsync(cancellationToken)));

app.Run();

// Maps a result to an HTTP response: the value as 200, each error type to a matching status with a problem details body.
static IResult ToHttpResult<T>(OpenUrzednikResult<T> result)
    => result.Match(
        value => Results.Ok(value),
        errors => errors[0] switch
        {
            ValidationError => Results.ValidationProblem(errors
                .GroupBy(e => e.Metadata.TryGetValue("name", out var name) ? name?.ToString() ?? "" : "")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray())),
            NotFoundError error => Results.Problem(error.Message, statusCode: StatusCodes.Status404NotFound),
            RateLimitExceededError error => Results.Problem(error.Message, statusCode: StatusCodes.Status429TooManyRequests),
            RequestTimeoutError error => Results.Problem(error.Message, statusCode: StatusCodes.Status504GatewayTimeout),
            var error => Results.Problem(error.Message, statusCode: StatusCodes.Status502BadGateway),
        });
