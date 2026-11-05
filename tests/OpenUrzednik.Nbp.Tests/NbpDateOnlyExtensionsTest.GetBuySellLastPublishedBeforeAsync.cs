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
    public async Task GetBuySellLastPublishedBeforeAsync_CurrencyDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpCurrencyExchangeRateClient>();
        client.GetBuySellLastPublishedBeforeAsync("USD", Midnight(2026, 10, 5), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<BuySellExchangeRates>(error));

        // Act
        var result = await client.GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public async Task GetBuySellLastPublishedBeforeAsync_TableDate_CallsDateTimeMethodWithMidnight()
    {
        // Arrange
        var error = new TestError("from the client");
        var client = Substitute.For<INbpExchangeRateTableClient>();
        client.GetBuySellLastPublishedBeforeAsync(Midnight(2026, 10, 5), Arg.Any<CancellationToken>())
            .Returns(OpenUrzednikResult.Failure<BuySellExchangeRateTable>(error));

        // Act
        var result = await client.GetBuySellLastPublishedBeforeAsync(new DateOnly(2026, 10, 5), TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void GetBuySellLastPublishedBeforeAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((INbpCurrencyExchangeRateClient)null!).GetBuySellLastPublishedBeforeAsync("USD", new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
