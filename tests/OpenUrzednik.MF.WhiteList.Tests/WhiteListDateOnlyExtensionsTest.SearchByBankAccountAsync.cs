#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    [Fact]
    public async Task SearchByBankAccountAsync_Date_CallsDateTimeMethodWithMidnightAndToken()
    {
        // Arrange
        var error = new TestError("from the client");
        using var cts = new CancellationTokenSource();
        var client = Substitute.For<IWhiteListClient>();
        client.SearchByBankAccountAsync("61109010140000071219812874", Midnight(2026, 10, 5), cts.Token).Returns(OpenUrzednikResult.Failure<SubjectsSearchResult>(error));

        // Act
        var result = await client.SearchByBankAccountAsync("61109010140000071219812874", new DateOnly(2026, 10, 5), cts.Token);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void SearchByBankAccountAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((IWhiteListClient)null!).SearchByBankAccountAsync("61109010140000071219812874", new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
