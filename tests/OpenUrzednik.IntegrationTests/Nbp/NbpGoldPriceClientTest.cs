using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp;

public class NbpGoldPriceClientTest : IClassFixture<NbpHttpClientFixture>
{
    private readonly HttpClient _httpClient;
    private readonly NbpGoldPriceClient _client;

    public NbpGoldPriceClientTest(NbpHttpClientFixture fixture)
    {
        _httpClient = fixture.HttpClient;
        _client = new NbpGoldPriceClient(_httpClient);
    }

    [ManualFact]
    public async Task GetLatestAsync_ReturnLatestGoldPrice()
    {
        // Arrange

        // Act
        var result = await _client.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Date.ShouldBeLessThanOrEqualTo(WarsawToday());
        result.Value.Price.ShouldBeGreaterThan(0m);
    }

    [ManualFact]
    public async Task GetTodayAsync_ReturnTodayGoldPrice()
    {
        // Arrange

        // Act
        var result = await _client.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var today = WarsawToday();
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
        var today = WarsawToday();

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
        var today = WarsawToday();
        var before = today.AddDays(-daysBefore);

        // Act
        var result = await _client.GetAsync(before, today, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeLessThan(daysBefore);
    }

    private static DateOnly WarsawToday()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw")).DateTime);
}
