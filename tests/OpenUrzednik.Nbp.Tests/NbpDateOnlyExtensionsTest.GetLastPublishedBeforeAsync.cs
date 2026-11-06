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
    public async Task GetLastPublishedBeforeAsync_GoldDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpGoldPriceClient>();
        client.GetLastPublishedBeforeAsync(Midnight(2026, 10, 5), Arg.Any<CancellationToken>()).Returns(OpenUrzednikResult.Failure<GoldPrice>(error));

        // Act
        var result = await client.GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_CurrencyDate_CallsDateTimeMethodWithMidnightAndTable()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpCurrencyExchangeRateClient>();
        client.GetLastPublishedBeforeAsync("USD", Midnight(2026, 10, 5), TableType.B, Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<CurrencyExchangeRates>(error));

        // Act
        var result = await client.GetLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), TableType.B, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetLastPublishedBeforeAsync_TableDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpExchangeRateTableClient>();
        client.GetLastPublishedBeforeAsync(TableType.A, Midnight(2026, 10, 5), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<ExchangeRateTable>(error));

        // Act
        var result = await client.GetLastPublishedBeforeAsync(TableType.A, new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void GetLastPublishedBeforeAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((INbpGoldPriceClient)null!).GetLastPublishedBeforeAsync(new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
