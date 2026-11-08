using System.Net;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public sealed partial class NbpRequestPipelineTest
{
    [Fact]
    public async Task GetLastAsync_ValidationFails_DoesNotBuildPathOrSend()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out var handler);
        var error = new ValidationError("Invalid.", "rule", "name", null);
        var pathBuilt = false;

        // Act
        var result = await sut.GetLastAsync<GoldPriceDto[], string>(CreateRecordingSpan(), "Operation", OpenUrzednikResult.Failure(error), () =>
        {
            pathBuilt = true;
            return "cenyzlota";
        }, JsonContext.GoldPriceDtoArray, dtos => "mapped", TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
        pathBuilt.ShouldBeFalse();
        handler.Request.ShouldBeNull();
    }

    [Fact]
    public async Task GetLastAsync_MapperReturnsModel_ReturnsIt()
    {
        // Arrange
        var sut = CreateSut(Json("""[{"data":"2026-08-21","cena":530.10},{"data":"2026-08-20","cena":522.32}]"""), out _);

        // Act
        var result = await sut.GetLastAsync<GoldPriceDto[], string>(Substitute.For<IOpenUrzednikSpan>(), "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dtos => $"{dtos.Length} items", TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe("2 items");
    }

    [Fact]
    public async Task GetLastAsync_MapperReturnsNull_ReturnsNotFoundErrorWithoutStatusCodeAndRecordsIt()
    {
        // Arrange
        var sut = CreateSut(Json("[]"), out _);
        var span = CreateRecordingSpan();

        // Act
        var result = await sut.GetLastAsync<GoldPriceDto[], string>(span, "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, _ => null, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<NotFoundError>();
        error.Metadata.ContainsKey(HttpStatusExtensions.StatusCodeMetadataKey).ShouldBeFalse();
        span.Received(1).SetTag("error.type", NotFoundError.ErrorCode);
    }

    [Fact]
    public async Task GetLastAsync_MapperRejectsPayload_ReturnsSerializationError()
    {
        // Arrange
        var sut = CreateSut(Json("[null]"), out _);

        // Act
        var result = await sut.GetLastAsync<GoldPriceDto[], string>(CreateRecordingSpan(), "Operation", OpenUrzednikResult.Success(),
            () => "cenyzlota", JsonContext.GoldPriceDtoArray, dtos => NbpPayload.Latest(dtos, static x => x.Date) is null ? null : "found", TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetLastAsync_RequestFails_RecordsErrorsAndDoesNotMap()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), out _);
        var span = CreateRecordingSpan();
        var mapped = false;

        // Act
        var result = await sut.GetLastAsync<GoldPriceDto[], string>(span, "Operation", OpenUrzednikResult.Success(), () => "cenyzlota", JsonContext.GoldPriceDtoArray, _ =>
        {
            mapped = true;
            return "mapped";
        }, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpServiceUnavailableError>();
        mapped.ShouldBeFalse();
        span.Received(1).AddEvent("error", "error.type", ServiceUnavailableError.ErrorCode);
    }

    [Fact]
    public async Task GetLastAsync_CallerSynchronizationContext_DoesNotResumeOnIt()
    {
        // Arrange
        var sut = CreateAsyncSut("""[{"data":"2026-08-20","cena":522.32}]""");

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => sut.GetLastAsync<GoldPriceDto[], string>(Substitute.For<IOpenUrzednikSpan>(),
            "Operation", OpenUrzednikResult.Success(), () => "cenyzlota", JsonContext.GoldPriceDtoArray, dtos => "mapped", TestContext.Current.CancellationToken));

        // Assert
        posts.ShouldBe(0);
    }
}
