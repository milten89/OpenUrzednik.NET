namespace OpenUrzednik.Nbp.Tests;

/// <summary>
/// Answers every request with a new response from <paramref name="createResponse"/>, completing asynchronously and away from
/// the caller's <see cref="SynchronizationContext"/>, so the awaits in the clients really suspend
/// (<see cref="TestCommon.StubHttpMessageHandler"/> completes synchronously).
/// </summary>
internal sealed class AsyncStubHttpMessageHandler(Func<HttpResponseMessage> createResponse) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        return createResponse();
    }
}
