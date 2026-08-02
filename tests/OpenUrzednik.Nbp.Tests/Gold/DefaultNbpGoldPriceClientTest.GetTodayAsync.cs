using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
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
    
    [Fact]
    public async Task GetTodayAsync_EmptyArrayResponse_ReturnsFailureWithNotFoundError()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        using var sut = CreateClient(faker, urlBuilder, CreateJsonResponse(HttpStatusCode.OK, []), out _);
        
        // Act
        var result = await sut.GetTodayAsync(TestContext.Current.CancellationToken);
        
        // Arrange
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
    
    [Fact]
    public async Task GetTodayAsync_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        var faker = new Faker().WithConstantSeed();
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.Today().Returns(faker.Internet.UrlRootedPath());
        using var sut = CreateClient(faker, urlBuilder, new HttpResponseMessage(HttpStatusCode.NotFound), out _);

        var result = await sut.GetTodayAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<NotFoundError>();
    }
}