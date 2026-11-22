using OpenUrzednik.TestCommon;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class ResilienceRejectionHandlerTest
{
    private static HttpMessageInvoker CreateSut(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inner)
        => new(new ResilienceRejectionHandler { InnerHandler = new DelegatingStubHttpMessageHandler(inner) });

    private static HttpRequestMessage CreateRequest() => new(HttpMethod.Get, "https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/3245174504?date=2026-11-02");

    // Returns the given task as is: unlike DelegatingStubHttpMessageHandler, it doesn't await, so it captures no context itself.
    private sealed class PendingHandler(Task<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response;
    }
}
