using System.Net;
using System.Net.Http.Json;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class DefaultNbpCurrencyExchangeRateClientTest
{
    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        var baseAddress = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/';
        handler = new StubHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    private static INbpUrlBuilderFactory CreateUrlBuilderFactory(NbpTable table, string currency, INbpUrlBuilder urlBuilder)
    {
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        urlBuilderFactory.GetCurrencyBuilder(table, currency).Returns(urlBuilder);
        return urlBuilderFactory;
    }

    private static DefaultNbpCurrencyExchangeRateClient CreateApiClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory)
        => new(httpClient, urlBuilderFactory, new FakeTimeProvider());

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, CurrencyExchangeRatesDto dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.CurrencyExchangeRatesDto) };

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, CountryExchangeRatesDto dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.CountryExchangeRatesDto) };

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, BuySellCurrencyExchangeRatesDto dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.BuySellCurrencyExchangeRatesDto) };
}
