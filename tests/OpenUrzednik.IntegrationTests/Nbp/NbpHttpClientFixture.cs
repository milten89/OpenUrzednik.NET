using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Options;

namespace OpenUrzednik.IntegrationTests.Nbp;

public sealed class NbpHttpClientFixture : IDisposable
{
    public HttpClient HttpClient { get; }

    public NbpHttpClientFixture()
        => HttpClient = new HttpClient().ConfigureForNbpApi(new NbpOptions());

    public void Dispose()
        => HttpClient.Dispose();
}
