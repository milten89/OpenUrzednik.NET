using System.Globalization;
using System.Net;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

/// <summary>
/// Stubs and assertions shared by the NBP WireMock tests. Bodies come from <see cref="NbpFixtures"/>.
/// </summary>
internal static class NbpWireMockServerExtensions
{
    private const string PlainTextUtf8 = "text/plain; charset=utf-8";

    /// <summary>How long <see cref="GivenSlowResponse"/> delays the response: far longer than any timeout the tests set.</summary>
    public static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Upper bound for a call that should give up instead of waiting for a slow response. Well below <see cref="SlowResponseDelay"/>,
    /// so a client that waits for the response fails the check, and generous enough for a cold CI runner (net472 needed 2.9 s for a 0.1 s timeout).
    /// </summary>
    public static readonly TimeSpan GaveUpWithin = TimeSpan.FromSeconds(5);

    public static void GivenJson(this WireMockServer server, string path, string body)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

    public static void GivenFixture(this WireMockServer server, string path, string fixture)
        => server.GivenJson(path, NbpFixtures.Load(fixture));

    /// <summary>NBP sends error messages as plain text, e.g. <c>404 NotFound - Not Found - Brak danych</c>.</summary>
    public static void GivenError(this WireMockServer server, string path, HttpStatusCode statusCode, string? fixture = null)
    {
        var response = Response.Create().WithStatusCode(statusCode);
        if (fixture is not null)
            response = response.WithHeader("Content-Type", PlainTextUtf8).WithBody(NbpFixtures.Load(fixture));

        server.Given(Request.Create().WithPath(path).UsingGet()).RespondWith(response);
    }

    public static void GivenRetryAfter(this WireMockServer server, string path, string retryAfter)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Retry-After", retryAfter));

    /// <summary>Answers <paramref name="path"/> with an empty array after <see cref="SlowResponseDelay"/>.</summary>
    public static void GivenSlowResponse(this WireMockServer server, string path)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]")
                .WithDelay(SlowResponseDelay));

    /// <summary>The date as the NBP API writes it in paths, independent of the current culture.</summary>
    public static string ToIso(this DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static void ShouldHaveReceivedGet(this WireMockServer server, string path)
        => server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
}
