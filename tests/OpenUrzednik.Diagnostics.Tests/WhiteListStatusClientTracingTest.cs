using System.Diagnostics;
using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0023, ADR-0025 confirmation: the spans and the metric of a real WL2 status client reach an ActivityListener and a MeterListener,
// with the NIP only masked and the request path only as its route template. End-to-end through WhiteListStatusClient.
public sealed class WhiteListStatusClientTracingTest : IDisposable
{
    private const string Nip = "7368125416";
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));
    private static readonly DateTime Date = new(2026, 11, 2);

    private readonly ActivityRecorder _recorder = new();

    public void Dispose() => _recorder.Dispose();

    private WhiteListStatusClient CreateSut(HttpClient httpClient, IOpenUrzednikMetrics? metrics = null)
        => new(httpClient, null, Clock, new OpenUrzednikTelemetry { TraceSource = new ActivityTraceSource(_recorder.Source), Metrics = metrics });

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private IEnumerable<string> AllTagsAndEvents()
        => _recorder.Stopped.SelectMany(activity => activity.TagObjects.Select(tag => $"{tag.Value}")
            .Concat(activity.Events.SelectMany(e => e.Tags.Select(tag => $"{tag.Value}")))
            .Append(activity.StatusDescription ?? string.Empty));

    [Fact]
    public async Task GetVatStatusByNipAsync_WithADate_RecordsOperationAndHttpActivitiesWithoutTheNip()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Json(HttpStatusCode.OK,
            """{"result":{"subject":{"nip":"7368125416","status":"Czynny","statusCode":"C","statusVat":"Czynny","requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"Oytfp-99b3aea"}}}""")));

        // Act
        var result = await CreateSut(httpClient).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("mf_white_list.status.nip");
        operation.Kind.ShouldBe(ActivityKind.Internal);
        operation.GetTagItem("mf_white_list.nip").ShouldBe("******5416");
        operation.GetTagItem("mf_white_list.date").ShouldBe("2026-11-02");
        operation.GetTagItem("error.type").ShouldBeNull();
        operation.Status.ShouldBe(ActivityStatusCode.Unset);
        http.OperationName.ShouldBe("mf_white_list.http.get");
        http.ParentSpanId.ShouldBe(operation.SpanId);
        http.GetTagItem("url.path").ShouldBe("/api2/status/nip/{nip}");
        http.GetTagItem("http.response.status_code").ShouldBe(200);
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("68125"));
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NoDate_RecordsNoDateTag()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Json(HttpStatusCode.OK,
            """{"result":{"subject":{"statusCode":"C","requestedDate":"21-11-2026","responseDateTime":"21-11-2026 19:58:33","requestId":"cD13R-99b3ae9"}}}""")));

        // Act
        await CreateSut(httpClient).GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var operation = _recorder.Stopped[1];
        operation.GetTagItem("mf_white_list.nip").ShouldBe("******5416");
        operation.GetTagItem("mf_white_list.date").ShouldBeNull();
    }

    [Theory]
    [InlineData("B", "notFound")]
    [InlineData("I", "badRequest")]
    [InlineData("X", "serviceUnavailable")]
    public async Task GetVatStatusByNipAsync_NoStatus_RecordsTheErrorTypeWithoutTheNip(string code, string errorType)
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Json(HttpStatusCode.OK,
            $$$$"""{"result":{"subject":{"nip":"{{{{Nip}}}}","status":"x {{{{Nip}}}}","statusCode":"{{{{code}}}}","requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34","requestId":"MdAhc-99b3aea"}}}""")));

        // Act
        var result = await CreateSut(httpClient).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        var operation = _recorder.Stopped[1];
        operation.GetTagItem("error.type").ShouldBe(errorType);
        operation.Status.ShouldBe(ActivityStatusCode.Error);
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("68125"));
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_RateLimited_RecordsTheErrorWithoutTheNip()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json((HttpStatusCode)429, """{"message":"Limit żądań dla tego adresu IP został na dziś wyczerpany.","code":"WL-191"}""")));

        // Act
        var result = await CreateSut(httpClient).GetVatStatusByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        _recorder.Stopped[1].GetTagItem("error.type").ShouldBe(RateLimitExceededError.ErrorCode);
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("68125"));
    }
}
