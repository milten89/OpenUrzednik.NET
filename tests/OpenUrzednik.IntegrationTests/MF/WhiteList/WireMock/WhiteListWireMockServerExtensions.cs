using System.Net;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList.WireMock;

/// <summary>
/// Stubs and assertions shared by the MF White List WireMock tests. Bodies come from <see cref="WhiteListFixtures"/>.
/// </summary>
internal static class WhiteListWireMockServerExtensions
{
    /// <summary>How long <see cref="GivenSlowResponse"/> delays the response: far longer than any timeout the tests set.</summary>
    public static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(10);

    /// <summary>Upper bound for a call that should give up instead of waiting for a slow response (see the NBP tests' value).</summary>
    public static readonly TimeSpan GaveUpWithin = TimeSpan.FromSeconds(5);

    /// <summary>Answers a GET of <paramref name="path"/> with <paramref name="date"/> in the query, with a fixture as JSON.</summary>
    public static void GivenFixture(this WireMockServer server, string path, string date, string fixture, HttpStatusCode statusCode = HttpStatusCode.OK)
        => server.GivenBody(path, date, WhiteListFixtures.Load(fixture), statusCode);

    /// <summary>Answers a GET of <paramref name="path"/> with <paramref name="body"/> as JSON (hand-written only for what the API can't produce).</summary>
    public static void GivenBody(this WireMockServer server, string path, string date, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
        => server.Given(Request.Create().WithPath(path).WithParam("date", date).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

    /// <summary>Answers a GET of <paramref name="path"/> whatever its query (e.g. one without a date) with a fixture as JSON.</summary>
    public static void GivenFixtureForAnyQuery(this WireMockServer server, string path, string fixture, HttpStatusCode statusCode = HttpStatusCode.OK)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(WhiteListFixtures.Load(fixture)));

    /// <summary>Answers with a status and no body.</summary>
    public static void GivenStatus(this WireMockServer server, string path, HttpStatusCode statusCode)
        => server.Given(Request.Create().WithPath(path).UsingGet()).RespondWith(Response.Create().WithStatusCode(statusCode));

    /// <summary>Answers 429 with the table's WL-191 body and a <c>Retry-After</c>.</summary>
    public static void GivenRetryAfter(this WireMockServer server, string path, string retryAfter)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Retry-After", retryAfter)
                .WithBody(WhiteListFixtures.Load("error-429-wl-191.from-error-table.json")));

    /// <summary>Answers <paramref name="path"/> with a captured body after <see cref="SlowResponseDelay"/>.</summary>
    public static void GivenSlowResponse(this WireMockServer server, string path)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(WhiteListFixtures.Load("search-nip.json"))
                .WithDelay(SlowResponseDelay));

    public static void ShouldHaveReceivedGet(this WireMockServer server, string path, string date)
        => server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Query!["date"].Contains(date) &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
}
