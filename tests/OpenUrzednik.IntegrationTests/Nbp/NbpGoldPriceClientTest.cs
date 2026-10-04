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
    public async Task GetLatestAsync_AnyDay_ReturnsPriceNotAfterToday()
    {
        // Arrange

        // Act
        var result = await _client.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Date.ShouldBeLessThanOrEqualTo(WarsawToday());
        result.Value.Price.ShouldBeGreaterThan(0m);
    }

    // Today's price exists only after the publication on a business day; before it, and on weekends and holidays, the API returns 404.
    // Asking for the latest price first tells which case applies, without relying on the publication hour.
    [ManualFact]
    public async Task GetTodayAsync_AnyDay_ReturnsTodaysPriceOnlyWhenPublished()
    {
        // Arrange
        var today = WarsawToday();
        var latest = await _client.GetLatestAsync(TestContext.Current.CancellationToken);
        latest.IsSuccess.ShouldBeTrue();

        // Act
        var result = await _client.GetTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        if (latest.Value.Date == today)
        {
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(latest.Value);
        }
        else if (result.IsSuccess)
        {
            // Published between the two calls.
            result.Value.Date.ShouldBe(today);
        }
        else
        {
            result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        }
    }

    [ManualFact]
    public async Task GetTopCountAsync_Five_ReturnsFivePrices()
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
    public async Task GetAsync_PastBusinessDay_ReturnsThatDaysPrice()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 2); // a Friday; published prices don't change

        // Act
        var result = await _client.GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Date.ShouldBe(date);
        result.Value.Price.ShouldBe(517.55m);
    }

    [ManualFact]
    public async Task GetAsync_Weekend_ReturnsNotFound()
    {
        // Arrange
        var date = new DateOnly(2026, 10, 3); // a Saturday

        // Act
        var result = await _client.GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
    }

    [ManualFact]
    public async Task GetAsync_LastTenDays_ReturnsThePricesInTheRange()
    {
        // Arrange
        var daysBefore = 10;
        var today = WarsawToday();
        var before = today.AddDays(-daysBefore);

        // Act
        var result = await _client.GetAsync(before, today, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeInRange(1, daysBefore - 1); // 11 days hold at most 9 business days
        result.Value.ShouldAllBe(x => x.Date >= before && x.Date <= today);
    }

    private static DateOnly WarsawToday()
    {
        // .NET Framework only knows the Windows time zone id.
#if NET
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, warsaw).DateTime);
#else
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, warsaw).DateTime.Date;
#endif
    }
}
