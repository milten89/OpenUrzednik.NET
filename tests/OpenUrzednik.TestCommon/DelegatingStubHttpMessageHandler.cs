namespace OpenUrzednik.TestCommon;

public sealed class DelegatingStubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendDelegate;

    public DelegatingStubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendDelegate)
    {
        ArgumentNullException.ThrowIfNull(sendDelegate);

        _sendDelegate = sendDelegate;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _sendDelegate(request, cancellationToken);
    }
}
