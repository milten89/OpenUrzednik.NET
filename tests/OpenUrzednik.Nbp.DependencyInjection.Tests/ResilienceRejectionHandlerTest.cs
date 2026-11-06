using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class ResilienceRejectionHandlerTest
{
    private static HttpMessageInvoker CreateSut(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inner)
        => new(new ResilienceRejectionHandler { InnerHandler = new DelegatingStubHttpMessageHandler(inner) });

    private static HttpRequestMessage CreateRequest() => new(HttpMethod.Get, "https://api.nbp.pl/api/cenyzlota");

    // Returns the given task as is: unlike DelegatingStubHttpMessageHandler, it doesn't await, so it captures no context itself.
    private sealed class PendingHandler(Task<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => response;
    }

    // Counts the continuations posted to it, then runs them on the thread pool.
    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        private int _posts;

        public int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            base.Post(d, state);
        }
    }
}
