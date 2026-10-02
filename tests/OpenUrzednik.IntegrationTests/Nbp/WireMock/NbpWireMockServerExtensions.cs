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

    public static void GivenDelay(this WireMockServer server, string path, TimeSpan delay)
        => server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("[]")
                .WithDelay(delay));

    public static void ShouldHaveReceivedGet(this WireMockServer server, string path)
        => server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
}
