#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    [Fact]
    public async Task SearchByBankAccountsAsync_Date_CallsDateTimeMethodWithMidnightAndToken()
    {
        // Arrange
        IReadOnlyList<string> bankAccounts = ["61109010140000071219812874"];
        var error = new TestError("from the client");
        using var cts = new CancellationTokenSource();
        var client = Substitute.For<IWhiteListClient>();
        client.SearchByBankAccountsAsync(bankAccounts, Midnight(2026, 10, 5), cts.Token).Returns(OpenUrzednikResult.Failure<BatchSearchResult>(error));

        // Act
        var result = await client.SearchByBankAccountsAsync(bankAccounts, new DateOnly(2026, 10, 5), cts.Token);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void SearchByBankAccountsAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((IWhiteListClient)null!).SearchByBankAccountsAsync(["x"], new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
