using Polly;
using Polly.Timeout;

namespace OpenUrzednik.Nbp.DependencyInjection;

/// <summary>
/// Turns the exceptions a resilience handler throws when it rejects a request into the ones the request executor
/// returns as errors (ADR-0004): a timeout becomes a <see cref="TaskCanceledException"/> (a <c>RequestTimeoutError</c>),
/// an open circuit breaker or a rate limiter an <see cref="HttpRequestException"/> (a <c>ServiceUnavailableError</c>).
/// It must be the outermost handler, so it sees what the resilience handler throws.
/// </summary>
internal sealed class ResilienceRejectionHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutRejectedException ex)
        {
            // No inner TimeoutException: that would make the executor report HttpClient.Timeout as the limit that elapsed.
            throw new TaskCanceledException(ex.Message, ex);
        }
        catch (ExecutionRejectedException ex)
        {
            throw new HttpRequestException(ex.Message, ex);
        }
    }
}
