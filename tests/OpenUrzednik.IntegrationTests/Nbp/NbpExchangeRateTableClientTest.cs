using OpenUrzednik.Nbp;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.Nbp;

// Real NBP API (explicit tests, never in default CI).
public class NbpExchangeRateTableClientTest : IClassFixture<NbpHttpClientFixture>
{
    private readonly NbpExchangeRateTableClient _client;

    public NbpExchangeRateTableClientTest(NbpHttpClientFixture fixture)
        => _client = new NbpExchangeRateTableClient(fixture.HttpClient);

    // Guards the limits in TopCountValidator from the other side: the largest count the client allows must be accepted.
    [ManualTheory]
    [InlineData(TableType.A, 67)]
    [InlineData(TableType.B, 14)]
    public async Task GetTopCountAsync_AtTableLimit_IsAcceptedByTheApi(TableType table, int topCount)
    {
        // Act
        var result = await _client.GetTopCountAsync(table, topCount, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(topCount);
    }

    // Table B is published on Wednesdays: for a Thursday the last one before is the day before.
    [ManualFact]
    public async Task GetLastPublishedBeforeAsync_TableBOnThursday_ReturnsWednesdaysTable()
    {
        // Arrange
        var date = new DateTime(2026, 10, 1); // a Thursday

        // Act
        var result = await _client.GetLastPublishedBeforeAsync(TableType.B, date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("039/B/NBP/2026");
        result.Value.PublicationDate.ShouldBe(new DateTime(2026, 9, 30));
    }

    [ManualFact]
    public async Task GetBuySellLastPublishedBeforeAsync_Monday_ReturnsThePreviousFridaysTable()
    {
        // Arrange
        var date = new DateTime(2026, 10, 5); // a Monday; the Friday before is 2026-10-02

        // Act
        var result = await _client.GetBuySellLastPublishedBeforeAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TableId.ShouldBe("192/C/NBP/2026");
        result.Value.PublicationDate.ShouldBe(new DateTime(2026, 10, 2));
    }

    [ManualFact]
    public async Task GetBuySellTopCountAsync_AtTableLimit_IsAcceptedByTheApi()
    {
        // Act
        var result = await _client.GetBuySellTopCountAsync(67, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(67);
    }
}
