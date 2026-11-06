#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests;

public partial class NbpDateOnlyExtensionsTest
{
    [Fact]
    public async Task GetBuySellAsync_CurrencyDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpCurrencyExchangeRateClient>();
        client.GetBuySellAsync("USD", Midnight(2026, 10, 2), Arg.Any<CancellationToken>()).Returns(OpenUrzednikResult.Failure<BuySellExchangeRates>(error));

        // Act
        var result = await client.GetBuySellAsync("USD", new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetBuySellAsync_CurrencyDateRange_CallsDateTimeMethodWithMidnights()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpCurrencyExchangeRateClient>();
        client.GetBuySellAsync("USD", Midnight(2026, 10, 1), Midnight(2026, 10, 2), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<BuySellExchangeRates>(error));

        // Act
        var result = await client.GetBuySellAsync("USD", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetBuySellAsync_TableDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpExchangeRateTableClient>();
        client.GetBuySellAsync(Midnight(2026, 10, 2), Arg.Any<CancellationToken>()).Returns(OpenUrzednikResult.Failure<BuySellExchangeRateTable>(error));

        // Act
        var result = await client.GetBuySellAsync(new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetBuySellAsync_TableDateRange_CallsDateTimeMethodWithMidnights()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpExchangeRateTableClient>();
        client.GetBuySellAsync(Midnight(2026, 10, 1), Midnight(2026, 10, 2), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(error));

        // Act
        var result = await client.GetBuySellAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void GetBuySellAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((INbpExchangeRateTableClient)null!).GetBuySellAsync(new DateOnly(2026, 10, 2)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
