using System.Net;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Dto;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public sealed partial class NbpRequestPipelineTest
{
    [Fact]
    public async Task GetFirstAsync_ValidationFails_DoesNotBuildPathOrSend()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out var handler);
        var error = new ValidationError("Invalid.", "rule", "name", null);
        var pathBuilt = false;

        // Act
        var result = await sut.GetFirstAsync<GoldPriceDto, decimal>(CreateRecordingSpan(), "Operation", OpenUrzednikResult.Failure(error), () =>
        {
            pathBuilt = true;
            return "cenyzlota";
        }, JsonContext.GoldPriceDtoArray, dto => dto.Price, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
        pathBuilt.ShouldBeFalse();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetFirstAsync_SeveralItems_MapsFirstItem()
    {
        // Arrange
        var sut = CreateSut(Json("""[{"data":"2026-08-20","cena":522.32},{"data":"2026-08-21","cena":530.10}]"""), out _);

        // Act
        var result = await sut.GetFirstAsync<GoldPriceDto, decimal>(Substitute.For<IOpenUrzednikSpan>(), "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dto => dto.Price, TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(522.32m);
    }

    [Fact]
    public async Task GetFirstAsync_EmptyArray_ReturnsNotFoundErrorWithoutStatusCode()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out _);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetFirstAsync<GoldPriceDto, decimal>(span, "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dto => dto.Price, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata.ContainsKey(OpenUrzednikError.StatusCodeMetadataKey).ShouldBeFalse();
        // RecordError (one error) sets the error.code tag; RecordErrors adds events instead.
        span.Received(1).SetTag("error.code", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetFirstAsync_RequestFails_RecordsErrorsAndDoesNotMap()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), out _);
        var span = CreateRecordingSpan();
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
