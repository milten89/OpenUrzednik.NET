using System.Net;
using System.Net.Http.Json;

using Bogus;

using NSubstitute;

using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    private static DefaultNbpGoldPriceClient CreateClient(Faker faker, INbpUrlBuilder urlBuilder, HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        var urlBuilderFactory = Substitute.For<INbpUrlBuilderFactory>();
        urlBuilderFactory.GetGoldBuilder().Returns(urlBuilder);

        var baseAddress = faker.Internet.UrlWithPath("https").TrimEnd('/') + '/';
        handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };

        return new DefaultNbpGoldPriceClient(httpClient, urlBuilderFactory, TimeProvider.System);
    }
    
    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, GoldPriceDto[] dtos)
        => new(statusCode) { Content = JsonContent.Create(dtos, NbpJsonContext.Default.GoldPriceDtoArray) };
}