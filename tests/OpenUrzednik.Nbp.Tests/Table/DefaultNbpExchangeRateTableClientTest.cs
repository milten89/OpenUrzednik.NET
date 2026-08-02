using System.Net;
using System.Net.Http.Json;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class DefaultNbpExchangeRateTableClientTest
{
    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        var baseAddress = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/';
        handler = new StubHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }
    
    private static DefaultNbpExchangeRateTableClient CreateApiClient(HttpClient httpClient, NbpTable table, INbpUrlBuilder urlBuilder)
    {
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        urlBuilderFactory.GetTableBuilder(table).Returns(urlBuilder);
 
        return new DefaultNbpExchangeRateTableClient(httpClient, urlBuilderFactory, new FakeTimeProvider());
    }
    
    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, ExchangeRateTableDto[] dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.ExchangeRateTableDtoArray) };
    
    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, BuySellExchangeRateTableDto[] dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.BuySellExchangeRateTableDtoArray) };
}