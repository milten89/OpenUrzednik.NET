using System.Net;
using System.Text;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Nbp.Tests.Common;

// Shared helpers; tests are in one file per method.
public sealed partial class NbpRequestPipelineTest : IDisposable
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly List<HttpClient> _httpClients = [];

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
    }

    private NbpRequestPipeline CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        var telemetry = new OpenUrzednikTelemetry();
        return new NbpRequestPipeline(NbpConnection.Create(httpClient, null, telemetry, TimeProvider.System), telemetry);
    }

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static IOpenUrzednikSpan CreateRecordingSpan()
    {
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        return span;
    }
}
