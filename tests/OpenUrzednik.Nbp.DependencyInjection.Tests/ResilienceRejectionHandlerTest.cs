using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class ResilienceRejectionHandlerTest
{
    private static HttpMessageInvoker CreateSut(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inner)
        => new(new ResilienceRejectionHandler { InnerHandler = new DelegatingStubHttpMessageHandler(inner) });

    private static HttpRequestMessage CreateRequest() => new(HttpMethod.Get, "https://api.nbp.pl/api/cenyzlota");
}
