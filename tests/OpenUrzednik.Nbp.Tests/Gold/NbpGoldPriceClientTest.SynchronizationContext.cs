using System.Net;

using OpenUrzednik.Nbp.Tests.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Gold;

public partial class NbpGoldPriceClientTest
{
    [Theory]
    [InlineData("GetLatestAsync")]
    [InlineData("GetTopCountAsync")]
    [InlineData("GetTodayAsync")]
    [InlineData("GetAsync(date)")]
    [InlineData("GetAsync(from, to)")]
    [InlineData("GetLastPublishedBeforeAsync")]
    public async Task EveryMethod_CallerSynchronizationContext_DoesNotResumeOnIt(string method)
    {
        // Arrange
        using var httpClient = new HttpClient(new AsyncStubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = new NbpGoldPriceClient(httpClient, timeProvider: TestClock.FarFuture());
        var date = new DateTime(2026, 10, 2);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var posts = await PostCountingSynchronizationContext.CountPostsAsync(() => method switch
        {
            "GetLatestAsync" => sut.GetLatestAsync(cancellationToken),
            "GetTopCountAsync" => sut.GetTopCountAsync(5, cancellationToken),
            "GetTodayAsync" => sut.GetTodayAsync(cancellationToken),
            "GetAsync(date)" => sut.GetAsync(date, cancellationToken),
            "GetAsync(from, to)" => sut.GetAsync(date.AddDays(-7), date, cancellationToken),
            "GetLastPublishedBeforeAsync" => sut.GetLastPublishedBeforeAsync(date, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        });

        // Assert
        posts.ShouldBe(0);
    }
}
