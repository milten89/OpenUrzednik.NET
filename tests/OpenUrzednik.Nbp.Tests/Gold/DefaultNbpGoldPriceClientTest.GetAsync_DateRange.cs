using System.Net;

using Bogus;

using NSubstitute;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.Nbp.Tests.Fakes;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class DefaultNbpGoldPriceClientTest
{
    [Fact]
    public async Task GetAsync_DateRange_ToDateBeforeMinDate_ReturnsFailureWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var to = faker.Date.BeforeGoldMinDate();
        var from = to.AddDays(-faker.Random.Int(1, DateRangeValidator.MaxDateRange));
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_DateRange_ExceedsMaxDays_ReturnsFailureWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterGoldMinDate();
        var to = from.AddDays(DateRangeValidator.MaxDateRange + 1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<ValidationError>();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_DateRange_ToDateBeforeMinDateAndRangeExceedsMaxDays_ReturnsCombinedValidationErrorsWithoutSendingRequest()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.BeforeGoldMinDate();
        var to = from.AddDays(DateRangeValidator.MaxDateRange + 1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.OK), out var handler);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldAllBe(e => e is ValidationError);
        handler.Request.ShouldBeNull();
    }
    
    [Fact]
    public async Task GetAsync_DateRange_SuccessfulResponse_ReturnsAllMappedGoldPricesInOriginalOrder()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var count = faker.Random.Int(3, 10);
        var from = faker.Date.AfterGoldMinDate();
        var to = from.AddDays(count);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        var dtos = new GoldPriceDtoFaker().LinkRandomizerTo(faker).Generate(count).ToArray();
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, dtos), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        // Act
        var result = await sut.GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(dtos.Select(x => new GoldPrice(x.Date, x.Price)));
    }
    
    [Fact]
    public async Task GetAsync_DateRange_HttpRequestFails_ReturnsFailureWithoutAttemptingMapping()
    {
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterGoldMinDate();
        var to = from.AddDays(1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, new HttpResponseMessage(HttpStatusCode.TooManyRequests), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);

        var result = await sut.GetAsync(from, to, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldBeOfType<RateLimitExceededError>();
    }
    
    [Fact]
    public async Task GetAsync_DateRange_EmptyArrayResponse_ReturnsSuccessWithEmptyArray()
    {
        // Arrange
        var faker = new Faker().WithConstantSeed();
        var from = faker.Date.AfterGoldMinDate();
        var to = from.AddDays(1);
        var urlBuilder = Substitute.For<INbpUrlBuilder>();
        urlBuilder.ForDateRange(from, to).Returns(faker.Internet.UrlRootedPath());
        using var httpClient = CreateHttpClient(faker, CreateJsonResponse(HttpStatusCode.OK, []), out _);
        var sut = CreateApiClient(httpClient, urlBuilder);
        
        // Act
        var result = await sut.GetAsync(from, to, TestContext.Current.CancellationToken);
        
        // Arrange
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }
}