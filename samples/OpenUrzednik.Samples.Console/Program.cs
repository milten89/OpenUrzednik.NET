// Calls the real NBP API (https://api.nbp.pl) with the clients from OpenUrzednik.Nbp, without a DI container.
using System.Globalization;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Table;

// Only for this output: decimal points and ISO dates regardless of the machine's culture. The library doesn't need it.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// One HttpClient for the whole app; the clients don't change it, so it can be shared.
using var httpClient = new HttpClient();

var gold = new NbpGoldPriceClient(httpClient);
var rates = new NbpCurrencyExchangeRateClient(httpClient);
var tables = new NbpExchangeRateTableClient(httpClient);

// 1. Check the result: expected failures (no data, timeouts, HTTP errors) come back as errors, not exceptions.
var latestGold = await gold.GetLatestAsync();
if (latestGold.IsSuccess)
    Console.WriteLine($"Gold, 1 g: {latestGold.Value.Price} PLN ({latestGold.Value.Date:yyyy-MM-dd})");
else
    Console.WriteLine($"Gold price unavailable: {latestGold.Errors[0].Message}");

// 2. Match handles both cases in one expression.
var usd = await rates.GetTopCountAsync("USD", 5);
Console.WriteLine(usd.Match(
    value => $"USD, last {value.Rates.Count} mid rates: {string.Join(", ", value.Rates.Select(r => r.Price))}",
    errors => $"USD rates unavailable: {errors[0].Message}"));

// 3. Table C: bid (NBP buys the currency) and ask (NBP sells it).
var tableC = await tables.GetBuySellLatestAsync();
if (tableC.TryGetValue(out var table))
{
    Console.WriteLine($"Table {table.TableId}, traded {table.TradingDate:yyyy-MM-dd}:");
    foreach (var rate in table.Rates.Take(3))
        Console.WriteLine($"  {rate.CurrencyCode}: bid {rate.Bid}, ask {rate.Ask}");
}
else
{
    Console.WriteLine($"Table C unavailable: {tableC.Errors[0].Message}");
}

// 4. Invalid input is checked before any request is sent: the result holds a ValidationError.
var invalid = await rates.GetTopCountAsync("USD", 0);
if (invalid.IsFailure && invalid.Errors[0] is ValidationError validation)
    Console.WriteLine($"Validation: {validation.Message}");

// 5. Prefer exceptions? EnsureSuccess() throws an OpenUrzednikException subtype for a failed result.
try
{
    GoldPrice price = (await gold.GetAsync(new DateOnly(2025, 12, 25))).EnsureSuccess(); // Christmas: no price published
    Console.WriteLine(price.Price);
}
catch (NotFoundException ex)
{
    Console.WriteLine($"EnsureSuccess threw {ex.GetType().Name}: {ex.Message}");
}
catch (OpenUrzednikException ex)
{
    Console.WriteLine($"EnsureSuccess threw {ex.GetType().Name} ({ex.Error?.Code}): {ex.Message}");
}
