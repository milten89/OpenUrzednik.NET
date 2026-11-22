using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon.Attributes;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList;

// Explicit test against the ministry's test host (ADR-0023): WhiteListRetry on a real network response, as an app's resilience handler
// runs it. The API sends errors chunked, without Content-Length, and network content can be read only once, so the filter must leave the
// body readable for the error mapping (ADR-0023, Resilience; review of #263). One request per run.
public class WhiteListRetryTest
{
    // The test host's test subject "Nazwa Firmy 1".
    private const string TestNip = "3245174504";

    [ManualFact]
    public async Task ShouldRetryAsync_RealChunked400_LeavesTheBodyForTheErrorMapping()
    {
        // Arrange
        // The client rejects an out-of-range date before sending, so the handler moves the sent date before the register's range: the
        // host then answers 400 WL-118, which the filter reads first, as a resilience handler would, and the client maps afterwards.
        var filter = new RetryFilterHandler { InnerHandler = new HttpClientHandler() };
        using var httpClient = new HttpClient(filter);
        var client = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = WhiteListOptions.TestApiUrl });

        // Act
        var result = await client.SearchByNipAsync(TestNip, DateTime.Today.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert: the body stayed readable for the error mapping after the filter read it on a real chunked stream; "no retry" holds for
        // WL-118 whether or not the filter parsed the code (the unit tests in WhiteListRetryTest.NetworkBody.cs cover the parsing)
        filter.ShouldRetry.ShouldBe(false);
        filter.ContentLength.ShouldBeNull();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.WhiteListCode.ShouldBe("WL-118");
    }

    private sealed class RetryFilterHandler : DelegatingHandler
    {
        public bool? ShouldRetry { get; private set; }

        public long? ContentLength { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!.ToString();
            request.RequestUri = new Uri(uri.Substring(0, uri.IndexOf("date=", StringComparison.Ordinal) + "date=".Length) + "2020-12-31");

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            ContentLength = response.Content.Headers.ContentLength;
            ShouldRetry = await WhiteListRetry.ShouldRetryAsync(response, cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
