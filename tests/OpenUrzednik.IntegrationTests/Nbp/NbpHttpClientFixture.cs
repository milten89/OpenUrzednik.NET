namespace OpenUrzednik.IntegrationTests.Nbp;

public sealed class NbpHttpClientFixture : IDisposable
{
    public HttpClient HttpClient { get; }

    public NbpHttpClientFixture()
        => HttpClient = new HttpClient();

    public void Dispose()
        => HttpClient.Dispose();
}
