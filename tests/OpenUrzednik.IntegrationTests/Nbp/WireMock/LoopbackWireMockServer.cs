using System.Net;
using System.Net.Sockets;

using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

/// <summary>
/// Starts the WireMock servers of the NBP tests. Without a URL WireMock listens on every network interface, which makes
/// Windows ask for a firewall exception; the tests only call it from this machine, so it listens on the loopback interface
/// only (127.0.0.1 and ::1).
/// </summary>
internal static class LoopbackWireMockServer
{
    /// <summary>Starts an HTTPS server with WireMock's localhost certificate on a free loopback port.</summary>
    public static WireMockServer StartHttps()
        => WireMockServer.Start(new WireMockServerSettings { Urls = [$"https://127.0.0.1:{FindFreePort()}"] });

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
