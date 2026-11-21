#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    [Fact]
    public async Task SearchByRegonAsync_Date_CallsDateTimeMethodWithMidnightAndToken()
    {
        // Arrange
        var error = new TestError("from the client");
        using var cts = new CancellationTokenSource();
        var client = Substitute.For<IWhiteListClient>();
        client.SearchByRegonAsync("123456785", Midnight(2026, 10, 5), cts.Token).Returns(OpenUrzednikResult.Failure<SubjectSearchResult>(error));

        // Act
        var result = await client.SearchByRegonAsync("123456785", new DateOnly(2026, 10, 5), cts.Token);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void SearchByRegonAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((IWhiteListClient)null!).SearchByRegonAsync("123456785", new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
