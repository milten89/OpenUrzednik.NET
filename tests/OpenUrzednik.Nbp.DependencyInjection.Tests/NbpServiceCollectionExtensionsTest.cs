using System.Net;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class NbpServiceCollectionExtensionsTest
{
    private const string GoldPriceJson = """[{"data":"2026-10-02","cena":517.55}]""";

    // Registers the clients with a stub primary handler, so no request leaves the test.
    // The stub returns one response, which the client disposes, so each test makes one request.
    private static ServiceProvider BuildProvider(out StubHttpMessageHandler handler, Action<IServiceCollection>? configureServices = null,
        HttpResponseMessage? response = null)
    {
        var stub = handler = new StubHttpMessageHandler(response ?? Json(GoldPriceJson));
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddOpenUrzednikNbp().ConfigurePrimaryHttpMessageHandler(() => stub);
        return services.BuildServiceProvider();
    }

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
