using System.Net;
using System.Net.Http.Json;

using Bogus;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class NbpExchangeRateTableClientTest
{
    private static HttpClient CreateHttpClient(Faker faker, HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        var baseAddress = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/';
        handler = new StubHttpMessageHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    private static NbpExchangeRateTableClient CreateApiClient(HttpClient httpClient, NbpTable table, INbpUrlBuilder urlBuilder,
        IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? tracer = null)
    {
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        urlBuilderFactory.GetTableBuilder(table).Returns(urlBuilder);

        return new NbpExchangeRateTableClient(httpClient, urlBuilderFactory, new FakeTimeProvider(), logger, tracer);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, ExchangeRateTableDto[] dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.ExchangeRateTableDtoArray) };

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, BuySellExchangeRateTableDto[] dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.BuySellExchangeRateTableDtoArray) };
}
