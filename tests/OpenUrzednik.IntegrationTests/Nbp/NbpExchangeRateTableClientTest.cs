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
