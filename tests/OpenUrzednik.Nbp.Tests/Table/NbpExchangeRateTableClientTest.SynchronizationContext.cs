using System.Net;

using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Table;

public partial class NbpExchangeRateTableClientTest
{
    [Theory]
    [InlineData("GetLatestAsync")]
    [InlineData("GetTopCountAsync")]
    [InlineData("GetTodayAsync")]
    [InlineData("GetAsync(date)")]
    [InlineData("GetAsync(from, to)")]
    [InlineData("GetLastPublishedBeforeAsync")]
    [InlineData("GetBuySellLatestAsync")]
    [InlineData("GetBuySellTopCountAsync")]
    [InlineData("GetBuySellTodayAsync")]
    [InlineData("GetBuySellAsync(date)")]
    [InlineData("GetBuySellAsync(from, to)")]
    [InlineData("GetBuySellLastPublishedBeforeAsync")]
    public async Task EveryMethod_CallerSynchronizationContext_DoesNotResumeOnIt(string method)
    {
        // Arrange
        using var httpClient = new HttpClient(new AsyncStubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = new NbpExchangeRateTableClient(httpClient, timeProvider: TestClock.FarFuture());
        var date = new DateTime(2026, 10, 2);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var posts = await PostCountingSynchronizationContext.CountPostsAsync(() => method switch
        {
            "GetLatestAsync" => sut.GetLatestAsync(TableType.A, cancellationToken),
            "GetTopCountAsync" => sut.GetTopCountAsync(TableType.A, 5, cancellationToken),
            "GetTodayAsync" => sut.GetTodayAsync(TableType.A, cancellationToken),
            "GetAsync(date)" => sut.GetAsync(TableType.A, date, cancellationToken),
            "GetAsync(from, to)" => sut.GetAsync(TableType.A, date.AddDays(-7), date, cancellationToken),
            "GetLastPublishedBeforeAsync" => sut.GetLastPublishedBeforeAsync(TableType.A, date, cancellationToken),
            "GetBuySellLatestAsync" => sut.GetBuySellLatestAsync(cancellationToken),
            "GetBuySellTopCountAsync" => sut.GetBuySellTopCountAsync(5, cancellationToken),
            "GetBuySellTodayAsync" => sut.GetBuySellTodayAsync(cancellationToken),
            "GetBuySellAsync(date)" => sut.GetBuySellAsync(date, cancellationToken),
            "GetBuySellAsync(from, to)" => sut.GetBuySellAsync(date.AddDays(-7), date, cancellationToken),
            "GetBuySellLastPublishedBeforeAsync" => sut.GetBuySellLastPublishedBeforeAsync(date, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        });

        // Assert
        posts.ShouldBe(0);
    }
}
