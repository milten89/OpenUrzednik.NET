using System.Globalization;
using System.Net;
using System.Text;

using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class NbpCurrencyExchangeRateClientTest
{
    [Fact]
    public async Task GetAsync_ConcurrentCallsOnOneClient_EachResultMatchesItsRequest()
    {
        // Arrange
        const int callCount = 50;
        string[] currencies = ["USD", "EUR", "GBP", "CHF", "JPY", "CZK", "SEK", "NOK", "DKK", "CAD"];
        var firstDate = new DateTime(2024, 1, 1);
        using var handler = new DelegatingStubHttpMessageHandler(async (request, _) =>
        {
            // Request path: .../exchangerates/rates/a/{currency}/{yyyy-MM-dd}/
            var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
            var code = segments[segments.Length - 2];
            var date = DateTime.ParseExact(segments[segments.Length - 1], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var mid = 1m + (date - firstDate).Days / 100m;
            await Task.Yield();

            var json = $$"""{"table":"A","currency":"waluta {{code}}","code":"{{code}}","rates":[{"no":"1/A/NBP/2024","effectiveDate":"{{segments[segments.Length - 1]}}","mid":{{mid.ToString(CultureInfo.InvariantCulture)}}}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://nbp.example.com/api/") };
        var sut = new NbpCurrencyExchangeRateClient(httpClient, timeProvider: TestClock.FarFuture());

        // Act
        var calls = Enumerable.Range(0, callCount).Select(i =>
        {
            var currency = currencies[i % currencies.Length];
            var date = firstDate.AddDays(i);
            return Task.Run(async () => (currency, date, result: await sut.GetAsync(currency, date, cancellationToken: TestContext.Current.CancellationToken)));
        }).ToList();
        var responses = await Task.WhenAll(calls);

        // Assert
        responses.Length.ShouldBe(callCount);
        foreach (var (currency, date, result) in responses)
        {
            result.IsSuccess.ShouldBeTrue();
            result.Value.CurrencyCode.ShouldBe(currency);
            result.Value.Rates.Count.ShouldBe(1);
            result.Value.Rates[0].PublicationDate.ShouldBe(date);
            result.Value.Rates[0].Price.ShouldBe(1m + (date - firstDate).Days / 100m);
        }
    }
}
