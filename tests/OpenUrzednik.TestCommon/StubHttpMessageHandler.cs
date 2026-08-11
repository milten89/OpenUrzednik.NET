namespace OpenUrzednik.TestCommon;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage? _response;
    private readonly Exception? _exceptionToThrow;

    public HttpRequestMessage? Request { get; private set; }

    public StubHttpMessageHandler(HttpResponseMessage response) => _response = response;
    public StubHttpMessageHandler(Exception exceptionToThrow) => _exceptionToThrow = exceptionToThrow;


    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        return _exceptionToThrow is not null
            ? throw _exceptionToThrow
            : Task.FromResult(_response!);
    }
}
