using System.Net;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public class NbpRequestPipelineTest
{
    private static readonly NbpJsonContext JsonContext = new();

    private static NbpRequestPipeline CreateSut(HttpResponseMessage response, out StubHttpMessageHandler handler)
    {
        handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        var telemetry = new OpenUrzednikTelemetry();
        return new NbpRequestPipeline(NbpConnection.Create(httpClient, null, telemetry, TimeProvider.System), telemetry);
    }

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task GetAsync_ValidationFails_DoesNotBuildPathOrSendAndRecordsErrors()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out var handler);
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var error = new ValidationError("Invalid.", "rule", "name", null);
        var pathBuilt = false;

        // Act
        var result = await sut.GetAsync(span, "Operation", OpenUrzednikResult.Failure(error), () =>
        {
            pathBuilt = true;
            return "cenyzlota";
        }, JsonContext.GoldPriceDtoArray, dtos => dtos.Length, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
        pathBuilt.ShouldBeFalse();
        handler.Request.ShouldBeNull();
        span.Received(1).AddEvent("error", "error.code", ValidationError.ErrorCode);
    }

    [Fact]
    public async Task GetAsync_Success_MapsPayload()
    {
        // Arrange
        var sut = CreateSut(Json("""[{"data":"2026-08-20","cena":522.32}]"""), out var handler);

        // Act
        var result = await sut.GetAsync(Substitute.For<IOpenUrzednikSpan>(), "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dtos => dtos.Length, TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(1);
        handler.Request.ShouldNotBeNull().RequestUri.ShouldBe(new Uri("https://api.nbp.pl/api/cenyzlota"));
    }

    [Fact]
    public async Task GetFirstAsync_EmptyArray_ReturnsNotFoundErrorWithoutStatusCode()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out _);
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);

        // Act
        var result = await sut.GetFirstAsync<GoldPriceDto, decimal>(span, "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dto => dto.Price, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata.ContainsKey(OpenUrzednikError.StatusCodeMetadataKey).ShouldBeFalse();
        span.Received(1).SetTag("error.code", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetFirstAsync_RequestFails_RecordsErrorsAndDoesNotMap()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), out _);
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(true);
        var mapped = false;

        // Act
        var result = await sut.GetFirstAsync<GoldPriceDto, decimal>(span, "Operation", OpenUrzednikResult.Success(), () => "cenyzlota", JsonContext.GoldPriceDtoArray, dto =>
        {
            mapped = true;
            return dto.Price;
        }, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        mapped.ShouldBeFalse();
        span.Received(1).AddEvent("error", "error.code", ServiceUnavailableError.ErrorCode);
    }
}
