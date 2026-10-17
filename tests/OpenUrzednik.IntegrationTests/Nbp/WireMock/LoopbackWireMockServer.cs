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
    /// Starts an HTTPS server with WireMock's localhost certificate. Port 0 lets the system pick a free port when the server
    /// binds, so test classes running in parallel can't race for the same port; <c>Urls</c> reports the port it got.
    /// </summary>
    public static WireMockServer StartHttps()
        => WireMockServer.Start(new WireMockServerSettings { Urls = ["https://127.0.0.1:0"] });
}
