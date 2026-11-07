using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenUrzednik.Http.DependencyInjection;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Http.Tests.DependencyInjection;

// Shared fields and helpers; tests are in one file per method.
public partial class DisplayPathHttpClientLoggingTest
{
    private const string ClientName = "OpenUrzednik.Test";
    private const string LogicalCategory = "System.Net.Http.HttpClient." + ClientName + ".LogicalHandler";
    private const string ClientCategory = "System.Net.Http.HttpClient." + ClientName + ".ClientHandler";
    private const string BaseAddress = "https://api.example.com/api/";

    // A fake identifier carried by the request URI, and the display path that hides it (ADR-0003, ADR-0019).
    private const string Identifier = "9876543210";
    private const string IdentifierPath = "search/nip/" + Identifier + "?date=2026-11-06";
    private const string DisplayPath = "search/nip/{nip}?date=2026-11-06";

    private static readonly JsonTypeInfo<TestDto> TypeInfo = (JsonTypeInfo<TestDto>)new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    }.GetTypeInfo(typeof(TestDto));

    // The provider's named client with the display-path logging, a stub primary handler and a recording logger.
    // configure runs after AddOpenUrzednikDisplayPathLogging, as an app's own configuration would.
    private static ServiceProvider BuildProvider(HttpMessageHandler primaryHandler, RecordingLoggerProvider recorder, Action<IServiceCollection, IHttpClientBuilder>? configure = null,
        string clientName = ClientName)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(recorder).SetMinimumLevel(LogLevel.Trace));
        var builder = services.AddHttpClient(clientName)
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler)
            .AddOpenUrzednikDisplayPathLogging();
        configure?.Invoke(services, builder);
        return services.BuildServiceProvider();
    }

    private static RestRequestExecutor CreateExecutor(ServiceProvider provider, string clientName = ClientName)
        => new(provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName), new Uri(BaseAddress), new RestProviderProfile("test", "Test API"));

    private static HttpMessageHandler CreateOkHandler()
        => new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"name\":\"n\"}", System.Text.Encoding.UTF8, "application/json") });

    private static IReadOnlyList<LogEntry> HttpClientEntries(RecordingLoggerProvider recorder)
        => [.. recorder.Entries.Where(entry => entry.Category.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal))];

    // The elapsed time in the end entries, in any culture's number format.
    private static readonly System.Text.RegularExpressions.Regex ElapsedPattern = new(@"after [0-9.,]+ms", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private sealed record TestDto(string Name);
}
