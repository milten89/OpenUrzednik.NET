using System.Diagnostics;
using System.Net;
using System.Text;

using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0003 confirmation: spans a real client starts reach an ActivityListener.
// End-to-end through NbpGoldPriceClient, so not split into one file per method.
public sealed class NbpClientTracingTest : IDisposable
{
    private readonly ActivityRecorder _recorder = new();

    public void Dispose() => _recorder.Dispose();

    private NbpGoldPriceClient CreateSut(HttpClient httpClient)
        => new(httpClient, traceSource: new ActivityTraceSource(_recorder.Source));

    [Fact]
    public async Task GetLatestAsync_Success_RecordsOperationAndHttpActivities()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"data":"2026-10-02","cena":517.55}]""", Encoding.UTF8, "application/json"),
        };
        using var httpClient = new HttpClient(new StubHttpMessageHandler(response));

        // Act
        var result = await CreateSut(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _recorder.Stopped.Count.ShouldBe(2);
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("nbp.gold.latest");
        operation.Status.ShouldBe(ActivityStatusCode.Unset);
        http.OperationName.ShouldBe("nbp.http.get");
        http.ParentSpanId.ShouldBe(operation.SpanId);
        http.GetTagItem("http.request.method").ShouldBe("GET");
        http.GetTagItem("url.path").ShouldBe("/api/cenyzlota");
        http.GetTagItem("http.response.status_code").ShouldBe(200);
    }

    [Fact]
    public async Task GetLatestAsync_ServiceUnavailable_MarksActivitiesAsErrors()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        // Act
        var result = await CreateSut(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        http.Status.ShouldBe(ActivityStatusCode.Error);
        http.GetTagItem("http.response.status_code").ShouldBe(503);
        operation.Status.ShouldBe(ActivityStatusCode.Error);
        operation.Events.ShouldContain(e => e.Name == "error");
    }
}
