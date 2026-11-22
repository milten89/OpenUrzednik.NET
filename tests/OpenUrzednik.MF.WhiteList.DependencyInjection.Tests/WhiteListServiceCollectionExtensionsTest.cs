using System.Net;
using System.Text;

using Microsoft.Extensions.DependencyInjection;

using OpenUrzednik.TestCommon;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    private const string Nip = "3245174504";

    // A day the API accepts for the next five years.
    private static readonly DateTime Date = new(2026, 11, 2);

    // A body captured from the API (tests/OpenUrzednik.IntegrationTests/MF/WhiteList/Fixtures), linked into this project.
    private static readonly string SearchNipJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "search-nip.json"));

    // Registers the clients with a stub primary handler, so no request leaves the test.
    // The stub returns one response, which the client disposes, so each test makes one request.
    private static ServiceProvider BuildProvider(out StubHttpMessageHandler handler, Action<IServiceCollection>? configureServices = null,
        HttpResponseMessage? response = null)
    {
        var stub = handler = new StubHttpMessageHandler(response ?? Json(SearchNipJson));
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        services.AddOpenUrzednikMFWhiteList().ConfigurePrimaryHttpMessageHandler(() => stub);
        return services.BuildServiceProvider();
    }

    private const string OwnClientName = "own-white-list";

    // A client the app registers itself; the stub stands in for a resilience handler that rejects every request.
    private static ServiceProvider BuildOwnClientProvider(Exception rejection, Action<IHttpClientBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient(OwnClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new DelegatingStubHttpMessageHandler((_, _) => throw rejection));
        configure(builder);
        return services.BuildServiceProvider();
    }

    // The app's handlers in order, without the factory's own lifetime wrapper around them.
    private static List<DelegatingHandler> GetDelegatingHandlers(IServiceProvider provider, string name)
    {
        var handlers = new List<DelegatingHandler>();
        for (HttpMessageHandler? handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(name);
             handler is DelegatingHandler delegating;
             handler = delegating.InnerHandler)
        {
            if (delegating.GetType().Assembly != typeof(IHttpClientFactory).Assembly)
                handlers.Add(delegating);
        }

        return handlers;
    }

    private static int CountRejectionHandlers(IServiceProvider provider, string name)
        => GetDelegatingHandlers(provider, name).Count(handler => handler is ResilienceRejectionHandler);

    private sealed class PassThroughHandler : DelegatingHandler;


    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
