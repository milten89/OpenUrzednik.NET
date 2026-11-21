#if NET
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

public partial class WhiteListDateOnlyExtensionsTest
{
    [Fact]
    public async Task GetVatStatusByNipAsync_Date_CallsDateTimeMethodWithMidnightAndToken()
    {
        // Arrange
        var error = new TestError("from the client");
        using var cts = new CancellationTokenSource();
        var client = Substitute.For<IWhiteListStatusClient>();
        client.GetVatStatusByNipAsync("1234563218", Midnight(2026, 10, 5), cts.Token).Returns(OpenUrzednikResult.Failure<VatStatusResult>(error));

        // Act
        var result = await client.GetVatStatusByNipAsync("1234563218", new DateOnly(2026, 10, 5), cts.Token);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void GetVatStatusByNipAsync_NullClient_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = ((IWhiteListStatusClient)null!).GetVatStatusByNipAsync("1234563218", new DateOnly(2026, 10, 5)); })
            .ParamName.ShouldBe("client");
    }
}
#endif
