using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList.WireMock;

// ADR-0002 error paths and the API's codes of ADR-0023's error table. The client validates every input the API would reject, so a valid
// request gets the captured rejection.
public partial class WhiteListClientWireMockTest
{
    [Theory]
    [InlineData("error-400-wl-115.json", "WL-115", "Nieprawidłowy NIP.")]
    [InlineData("error-400-wl-118.json", "WL-118", "Data sprzed zakresu rejestru.")]
    [InlineData("error-400-wl-103.json", "WL-103", "Data nie może być datą przyszłą.")]
    public async Task SearchByNipAsync_Returns400_ReturnsBadRequestErrorWithTheCodeAndMessage(string fixture, string code, string message)
    {
        // Arrange
        _server.GivenFixture(NipPath, Day, fixture, HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.WhiteListCode.ShouldBe(code);
        error.Message.ShouldBe($"MF White List API rejected the request to {_server.Urls[0]}/api/search/nip/{{nip}}: {code} {message}");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
        error.Metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey].ShouldBe(code);
    }

    [Theory]
    [InlineData("error-400-wl-107.json", "WL-107")]
    public async Task SearchByRegonAsync_Returns400_ReturnsBadRequestErrorWithTheCode(string fixture, string code)
    {
        // Arrange
        _server.GivenFixture(RegonPath, Day, fixture, HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>().WhiteListCode.ShouldBe(code);
    }

    [Theory]
    [InlineData("error-400-wl-111.json", "WL-111")]
    public async Task SearchByBankAccountAsync_Returns400_ReturnsBadRequestErrorWithTheCode(string fixture, string code)
    {
        // Arrange
        _server.GivenFixture(BankAccountPath, Day, fixture, HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.WhiteListCode.ShouldBe(code);
        error.Message.ShouldContain("/api/search/bank-account/{account}");
    }

    [Fact]
    public async Task SearchByNipAsync_Returns400Wl195_ReturnsServiceUnavailableError()
    {
        // Arrange: the body is built from the error table (WL-195, "repeat the query"), not captured
        _server.GivenFixture(NipPath, Day, "error-400-wl-195.from-error-table.json", HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>();
        error.WhiteListCode.ShouldBe("WL-195");
        error.ShouldBeAssignableTo<ServiceUnavailableError>();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task SearchByNipAsync_Returns429Wl191WithoutRetryAfter_ReturnsRateLimitErrorUntilMidnight()
    {
        // Arrange: the body is built from the error table (WL-191, the daily limit), not captured
        _server.GivenFixture(NipPath, Day, "error-429-wl-191.from-error-table.json", HttpStatusCode.TooManyRequests);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12)); // from 12:00 in Warsaw to midnight
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(429);
    }

    [Fact]
    public async Task SearchByNipAsync_Returns429WithRetryAfter_ReturnsRateLimitErrorWithTheServersDelay()
    {
        // Arrange
        _server.GivenRetryAfter(NipPath, "30");

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.WhiteListCode.ShouldBe("WL-191");
    }

    [Fact]
    public async Task SearchByNipAsync_Returns500Wl197_ReturnsUnknownError()
    {
        // Arrange: the body is built from the error table (WL-197, the historical status can't be determined), not captured
        _server.GivenFixture(NipPath, Day, "error-500-wl-197.from-error-table.json", HttpStatusCode.InternalServerError);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListUnknownError>();
        error.WhiteListCode.ShouldBe("WL-197");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(500);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task SearchByNipAsync_Returns5xx_ReturnsServiceUnavailableError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(NipPath, statusCode);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>();
        error.WhiteListCode.ShouldBeNull();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task SearchByNipAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(NipPath, statusCode);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpUnauthorizedError>();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task SearchByNipAsync_Returns404_ReturnsNotFoundErrorWithoutTheNip()
    {
        // Arrange
        _server.GivenStatus(NipPath, HttpStatusCode.NotFound);

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(404);
        error.Message.ShouldNotContain(Nip);
    }

    [Fact]
    public async Task SearchByNipAsync_ConnectionFails_ReturnsConnectionFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await sut.SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>().Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SearchByNipAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange: hand-written, the API can't produce it
        _server.GivenBody(NipPath, Day, """{"result":{"subject":{"name":"Nazwa Firmy 1","nip":""");

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        error.ToException().ShouldBeOfType<OpenUrzednikSerializationException>();
    }

    [Fact]
    public async Task SearchByNipAsync_Returns200WithoutTheProof_ReturnsSerializationError()
    {
        // Arrange: hand-written, the API can't produce it (requestId is in every captured answer)
        _server.GivenBody(NipPath, Day, """{"result":{"subject":null,"requestDateTime":"20-11-2026 22:44:25"}}""");

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task SearchByNipAsync_Returns200WithAnUnknownStatus_ReturnsSerializationError()
    {
        // Arrange: the captured answer with a status the contract doesn't list
        _server.GivenBody(NipPath, Day, WhiteListFixtures.Load("search-nip.json").Replace("\"Czynny\"", "\"Aktywny\""));

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().Message.ShouldBe("MF White List API response has an unknown 'statusVat'.");
    }
}
