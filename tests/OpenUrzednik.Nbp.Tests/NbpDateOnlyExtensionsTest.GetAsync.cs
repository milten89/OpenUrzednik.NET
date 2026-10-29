#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests;

public partial class NbpDateOnlyExtensionsTest
{
    [Fact]
    public async Task GetAsync_GoldDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpGoldPriceClient>();
        client.GetAsync(Midnight(2026, 10, 2), Arg.Any<CancellationToken>()).Returns(OpenUrzednikResult.Failure<GoldPrice>(error));

        // Act
        var result = await client.GetAsync(new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetAsync_GoldDateRange_CallsDateTimeMethodWithMidnights()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpGoldPriceClient>();
        client.GetAsync(Midnight(2026, 10, 1), Midnight(2026, 10, 2), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(error));

        // Act
        var result = await client.GetAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetAsync_CurrencyDate_CallsDateTimeMethodWithMidnightAndTable()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpCurrencyExchangeRateClient>();
        client.GetAsync("USD", Midnight(2026, 10, 2), TableType.B, Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<CurrencyExchangeRates>(error));

        // Act
        var result = await client.GetAsync("USD", new DateOnly(2026, 10, 2), TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetAsync_CurrencyDateRange_CallsDateTimeMethodWithMidnightsAndTable()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpCurrencyExchangeRateClient>();
        client.GetAsync("USD", Midnight(2026, 10, 1), Midnight(2026, 10, 2), TableType.B, Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<CurrencyExchangeRates>(error));

        // Act
        var result = await client.GetAsync("USD", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetAsync_TableDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpExchangeRateTableClient>();
        client.GetAsync(TableType.A, Midnight(2026, 10, 2), Arg.Any<CancellationToken>()).Returns(OpenUrzednikResult.Failure<ExchangeRateTable>(error));

        // Act
        var result = await client.GetAsync(TableType.A, new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetAsync_TableDateRange_CallsDateTimeMethodWithMidnights()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpExchangeRateTableClient>();
        client.GetAsync(TableType.A, Midnight(2026, 10, 1), Midnight(2026, 10, 2), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(error));

        // Act
        var result = await client.GetAsync(TableType.A, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void GetAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => ((INbpGoldPriceClient)null!).GetAsync(new DateOnly(2026, 10, 2)))
            .ParamName.ShouldBe("client");
    }
}
#endif
