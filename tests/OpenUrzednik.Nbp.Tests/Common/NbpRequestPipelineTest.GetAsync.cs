using System.Net;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public sealed partial class NbpRequestPipelineTest
{
    [Fact]
    public async Task GetAsync_ValidationFails_DoesNotBuildPathOrSendAndRecordsErrors()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out var handler);
        var span = CreateRecordingSpan();
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
        // RecordErrors adds one "error" event per error.
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
    public async Task GetAsync_MapperRejectsPayload_ReturnsSerializationError()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out _);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetAsync<Dto.GoldPriceDto[], int>(span, "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, _ => throw new InvalidPayloadException("NBP API response is missing 'rates'."),
            TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().Message.ShouldBe("NBP API response is missing 'rates'.");
        span.Received(1).SetTag("error.code", SerializationError.ErrorCode);
    }

    [Fact]
    public async Task GetAsync_RequestFails_RecordsErrors()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), out _);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetAsync(span, "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dtos => dtos.Length, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ServiceUnavailableError>();
        span.Received(1).AddEvent("error", "error.code", ServiceUnavailableError.ErrorCode);
    }
}
