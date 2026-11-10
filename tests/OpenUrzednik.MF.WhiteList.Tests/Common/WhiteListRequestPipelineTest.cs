using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

// Shared helpers; tests are in one file per method. Identifiers are fake.
public sealed partial class WhiteListRequestPipelineTest : IDisposable
{
    private const string RelativePath = "search/nip/1234567890";
    private const string DisplayPath = "search/nip/{nip}";

    private readonly List<HttpClient> _httpClients = [];

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
    }

    private WhiteListRequestPipeline CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler, OpenUrzednikTelemetry? telemetry = null)
    {
        handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        telemetry ??= new OpenUrzednikTelemetry();
        return new WhiteListRequestPipeline(WhiteListConnection.CreateSearch(httpClient, null, telemetry, new FakeTimeProvider()), telemetry);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static IOpenUrzednikSpan CreateRecordingSpan()
    {
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        return span;
    }

    private static IOpenUrzednikLogger CreateLogger()
    {
        var logger = Substitute.For<IOpenUrzednikLogger>();
        logger.IsEnabled(Arg.Any<OpenUrzednikLogLevel>()).Returns(true);
        return logger;
    }
}
