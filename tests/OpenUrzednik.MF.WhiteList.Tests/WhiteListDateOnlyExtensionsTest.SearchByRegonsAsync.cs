#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    [Fact]
    public async Task SearchByRegonsAsync_Date_CallsDateTimeMethodWithMidnightAndToken()
    {
        // Arrange
        IReadOnlyList<string> regons = ["123456785"];
        var error = new TestError("from the client");
        using var cts = new CancellationTokenSource();
        var client = Substitute.For<IWhiteListClient>();
        client.SearchByRegonsAsync(regons, Midnight(2026, 10, 5), cts.Token).Returns(OpenUrzednikResult.Failure<BatchSearchResult>(error));

        // Act
        var result = await client.SearchByRegonsAsync(regons, new DateOnly(2026, 10, 5), cts.Token);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void SearchByRegonsAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((IWhiteListClient)null!).SearchByRegonsAsync(["x"], new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
