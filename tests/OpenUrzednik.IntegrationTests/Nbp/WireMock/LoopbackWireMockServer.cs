using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

/// <summary>
/// Starts the WireMock servers of the NBP tests. Without a URL WireMock listens on every network interface, which makes
/// Windows ask for a firewall exception; the tests only call it from this machine, so it listens on 127.0.0.1 only.
/// </summary>
internal static class LoopbackWireMockServer
{
    /// <summary>
    /// Starts an HTTPS server with WireMock's localhost certificate on a random free port.
    /// </summary>
    /// <remarks>
    /// Port 0 is the usual socket convention for "any free port": the operating system assigns an ephemeral port when the
    /// server binds. WireMock.Net doesn't document port 0 in <c>Urls</c>, but passes it to its web server on both .NET and
    /// .NET Framework (checked on net10.0 and net472) and reports the port it got in <see cref="WireMockServer.Urls"/>,
    /// which the tests use as the base address. Because the port is chosen at bind time, test classes running in parallel
    /// can't race for the same port, unlike a free port looked up before the server starts.
    /// </remarks>
    public static WireMockServer StartHttps()
        => WireMockServer.Start(new WireMockServerSettings { Urls = ["https://127.0.0.1:0"] });
}
