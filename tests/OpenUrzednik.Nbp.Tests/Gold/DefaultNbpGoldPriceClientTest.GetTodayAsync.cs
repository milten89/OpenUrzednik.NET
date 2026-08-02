using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Fact]
    public async Task GetTodayAsync_SuccessfulResponse_ReturnsMappedGoldPriceFromFirstArrayElement()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        var dto = new GoldPriceDtoFaker().LinkRandomizerTo(faker).Generate();
        using var sut = CreateClient(faker, urlBuilder, CreateJsonResponse(HttpStatusCode.OK, [dto]), out _);

        // Act
        var result = await sut.GetTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(dto.Date, dto.Price));
    }
}