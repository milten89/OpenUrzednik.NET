using System.Net;

using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Currency;

public partial class NbpCurrencyExchangeRateClientTest
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
        var sut = new NbpCurrencyExchangeRateClient(httpClient, timeProvider: TestClock.FarFuture());
        var date = new DateTime(2026, 10, 2);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var posts = await PostCountingSynchronizationContext.CountPostsAsync(() => method switch
        {
            "GetLatestAsync" => sut.GetLatestAsync("USD", TableType.A, cancellationToken),
            "GetTopCountAsync" => sut.GetTopCountAsync("USD", 5, TableType.A, cancellationToken),
            "GetTodayAsync" => sut.GetTodayAsync("USD", TableType.A, cancellationToken),
            "GetAsync(date)" => sut.GetAsync("USD", date, TableType.A, cancellationToken),
            "GetAsync(from, to)" => sut.GetAsync("USD", date.AddDays(-7), date, TableType.A, cancellationToken),
            "GetLastPublishedBeforeAsync" => sut.GetLastPublishedBeforeAsync("USD", date, TableType.A, cancellationToken),
            "GetBuySellLatestAsync" => sut.GetBuySellLatestAsync("USD", cancellationToken),
            "GetBuySellTopCountAsync" => sut.GetBuySellTopCountAsync("USD", 5, cancellationToken),
            "GetBuySellTodayAsync" => sut.GetBuySellTodayAsync("USD", cancellationToken),
            "GetBuySellAsync(date)" => sut.GetBuySellAsync("USD", date, cancellationToken),
            "GetBuySellAsync(from, to)" => sut.GetBuySellAsync("USD", date.AddDays(-7), date, cancellationToken),
            "GetBuySellLastPublishedBeforeAsync" => sut.GetBuySellLastPublishedBeforeAsync("USD", date, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        });

        // Assert
        posts.ShouldBe(0);
    }
}
