using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp;

public class NbpGoldPriceClientTest : IClassFixture<NbpHttpClientFixture>
{
    private readonly HttpClient _httpClient;
    private readonly NbpUrlBuilderFactory _urlBuilderFactory = new();
    private readonly NbpGoldPriceClient _client;

    public NbpGoldPriceClientTest(NbpHttpClientFixture fixture)
    {
        _httpClient = fixture.HttpClient;
        _client = new NbpGoldPriceClient(_httpClient, _urlBuilderFactory);
    }

    [ManualFact]
    public async Task GetLatestAsync_ReturnLatestGoldPrice()
    {
        // Arrange

        // Act
        var result = await _client.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Date.ShouldBeLessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now));
        result.Value.Price.ShouldBeGreaterThan(0m);
    }

    [ManualFact]
    public async Task GetTodayAsync_ReturnTodayGoldPrice()
    {
        // Arrange

        // Act
        var result = await _client.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today.DayOfWeek == DayOfWeek.Saturday ||
            today.DayOfWeek == DayOfWeek.Sunday)
        {
            result.IsFailure.ShouldBeTrue();
            result.Errors.ShouldContain(x => x is NotFoundError, 1);
        }
        else
        {
            result.IsSuccess.ShouldBeTrue();
            result.Value.Date.ShouldBeLessThanOrEqualTo(today);
            result.Value.Price.ShouldBeGreaterThan(0m);
        }
    }

    [ManualFact]
    public async Task GetTopCountAsync_ReturnLatestXTopCountGoldPrice()
    {
        // Arrange
        const int topCount = 5;

        // Act
        var result = await _client.GetTopCountAsync(topCount, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(topCount);
    }

    [ManualFact]
    public async Task GetAsync_ReturnGoldPriceFromSelectedDate()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var result = await _client.GetAsync(today, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Date.ShouldBeLessThanOrEqualTo(today);
        result.Value.Price.ShouldBeGreaterThan(0m);
    }

    [ManualFact]
    public async Task GetAsync_ReturnGoldPriceFromSelectedDateRange()
    {
        // Arrange
        var daysBefore = 10;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var before = today.AddDays(-daysBefore);

        // Act
        var result = await _client.GetAsync(before, today, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeLessThan(daysBefore);
    }
}
