using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.IntegrationTests.Nbp.WireMock;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.MF.WhiteList.Search;
using OpenUrzednik.MF.WhiteList.Status;
using OpenUrzednik.Transport.Http;

using Shouldly;

using WireMock.Server;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList.WireMock;

// The WL2 status client against captured answers and every ADR-0002 error path (ADR-0023). Every outcome of the service is HTTP 200; the
// NIP in requests is fake, with a valid check digit, and WireMock answers by path, so the captured answer needn't be about it.
public sealed class WhiteListStatusClientWireMockTest : IDisposable
{
    private const string Nip = "7368125416";
    private const string Path = "/api2/status/nip/" + Nip;
    private const string DisplayPath = "/api2/status/nip/{nip}";
    private const string Day = "2026-11-02";

    // 12:00 on 2026-11-20 in Warsaw: the date is in the list's range, and midnight is 12 hours away.
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Date = new(2026, 11, 2);

    // The captured answers: 19:58:34 and 19:58:33 on 2026-11-21 in Warsaw (CET).
    private static readonly DateTimeOffset Answered = new(2026, 11, 21, 19, 58, 34, TimeSpan.FromHours(1));

    private readonly WireMockServer _server = LoopbackWireMockServer.StartHttps();
    private readonly List<HttpClient> _httpClients = [];

    private WhiteListStatusClient CreateSut(TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        // A plain HttpClient: the host root and timeout go to the client, the HttpClient isn't configured.
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new WhiteListStatusClient(httpClient, new WhiteListOptions { ApiUrl = _server.Urls[0] + "/", Timeout = timeout }, new FakeTimeProvider(Now));
    }

    private static Task<OpenUrzednikResult<VatStatusResult>> StatusAsync(WhiteListStatusClient sut, DateTime? date = null)
        => sut.GetVatStatusByNipAsync("PL 736-812-54-16", date ?? Date, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("status-nip-active.json", VatStatus.Active, "Oytfp-99b3aea")]
    [InlineData("status-nip-exempt.json", VatStatus.Exempt, "a5npN-99b3aea")]
    [InlineData("status-nip-not-registered.json", VatStatus.NotRegistered, "JBvoN-99b3aea")]
    public async Task GetVatStatusByNipAsync_Returns200WithAStatus_MapsItWithTheProof(string fixture, VatStatus status, string requestId)
    {
        // Arrange
        _server.GivenFixture(Path, Day, fixture);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new VatStatusResult(status, Date, requestId, Answered));
        result.Value.Date.Kind.ShouldBe(DateTimeKind.Unspecified);
        _server.ShouldHaveReceivedGet(Path, Day);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_NoDate_SendsNoDateAndReturnsTodayFromTheAnswer()
    {
        // Arrange
        _server.GivenFixtureForAnyQuery(Path, "status-nip-active-no-date.json");

        // Act
        var result = await CreateSut().GetVatStatusByNipAsync(Nip, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new VatStatusResult(VatStatus.Active, new DateTime(2026, 11, 21), "cD13R-99b3ae9", new DateTimeOffset(2026, 11, 21, 19, 58, 33, TimeSpan.FromHours(1))));
        var entry = _server.LogEntries.ShouldHaveSingleItem();
        entry.RequestMessage!.Path.ShouldBe(Path);
        (entry.RequestMessage.Query?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns200WithB_ReturnsNotFoundErrorWithTheProof()
    {
        // Arrange
        _server.GivenFixture(Path, Day, "status-nip-not-on-list.json");

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.RequestId.ShouldBe("MdAhc-99b3aea");
        error.RequestDateTime.ShouldBe(Answered);
        error.Message.ShouldNotContain(Nip);
        error.Message.ShouldNotContain("0074370710");
    }

    [Theory]
    [InlineData("status-nip-invalid-nip.json", "I")]
    [InlineData("status-nip-date-out-of-range.json", "D")]
    public async Task GetVatStatusByNipAsync_Returns200WithIOrD_ReturnsQueryRejectedErrorWithTheCode(string fixture, string code)
    {
        // Arrange
        _server.GivenFixture(Path, Day, fixture);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListQueryRejectedError>();
        error.WhiteListCode.ShouldBe(code);
        error.ShouldBeAssignableTo<BadRequestError>();
        error.Message.ShouldNotContain("1234567890");
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns200WithX_ReturnsServiceInactiveErrorWithTheCode()
    {
        // Arrange: the body is built from the contract (X, "service inactive"), not captured
        _server.GivenFixture(Path, Day, "status-nip-service-inactive.from-contract.json");

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceInactiveError>();
        error.WhiteListCode.ShouldBe("X");
        error.ShouldBeAssignableTo<ServiceUnavailableError>();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns200WithAnUnknownCode_ReturnsSerializationError()
    {
        // Arrange: the captured answer with a code the contract doesn't list
        _server.GivenBody(Path, Day, WhiteListFixtures.Load("status-nip-active.json").Replace("\"C\"", "\"Q\""));

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        error.Message.ShouldBe("MF White List API response has a 'statusCode' that isn't one of C, Z, N, B, I, D, X.");
    }

    [Theory]
    [InlineData("error-400-wl-115.json", "WL-115")]
    [InlineData("error-400-wl-118.json", "WL-118")]
    [InlineData("error-400-wl-103.json", "WL-103")]
    public async Task GetVatStatusByNipAsync_Returns400_ReturnsBadRequestErrorWithTheCodeAndTheDisplayPath(string fixture, string code)
    {
        // Arrange
        _server.GivenFixture(Path, Day, fixture, HttpStatusCode.BadRequest);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.WhiteListCode.ShouldBe(code);
        error.Message.ShouldContain(DisplayPath);
        error.Message.ShouldNotContain(Nip);
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns400Wl195_ReturnsServiceUnavailableError()
    {
        // Arrange: the body is built from the error table (WL-195, "repeat the query"), not captured
        _server.GivenFixture(Path, Day, "error-400-wl-195.from-error-table.json", HttpStatusCode.BadRequest);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>();
        error.WhiteListCode.ShouldBe("WL-195");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns429Wl191WithoutRetryAfter_ReturnsRateLimitErrorUntilMidnight()
    {
        // Arrange: the body is built from the error table (WL-191), not captured
        _server.GivenFixture(Path, Day, "error-429-wl-191.from-error-table.json", HttpStatusCode.TooManyRequests);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(429);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns429WithRetryAfter_ReturnsRateLimitErrorWithTheServersDelay()
    {
        // Arrange
        _server.GivenRetryAfter(Path, "30");

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>().RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns500Wl197_ReturnsUnknownError()
    {
        // Arrange: the body is built from the error table (WL-197), not captured
        _server.GivenFixture(Path, Day, "error-500-wl-197.from-error-table.json", HttpStatusCode.InternalServerError);

        // Act
        var result = await StatusAsync(CreateSut());

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
    public async Task GetVatStatusByNipAsync_Returns5xx_ReturnsServiceUnavailableError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(Path, statusCode);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>();
        error.WhiteListCode.ShouldBeNull();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetVatStatusByNipAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(Path, statusCode);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpUnauthorizedError>().Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns404_ReturnsNotFoundErrorWithoutTheNip()
    {
        // Arrange
        _server.GivenStatus(Path, HttpStatusCode.NotFound);

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(404);
        error.Message.ShouldNotContain(Nip);
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_ConnectionFails_ReturnsConnectionFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await StatusAsync(sut);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>().Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange: hand-written, the API can't produce it
        _server.GivenBody(Path, Day, """{"result":{"subject":{"statusCode":"C""");

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().ToException().ShouldBeOfType<OpenUrzednikSerializationException>();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Returns200WithoutTheProof_ReturnsSerializationError()
    {
        // Arrange: hand-written, the API can't produce it (requestId is in every captured answer)
        _server.GivenBody(Path, Day, """{"result":{"subject":{"statusCode":"C","requestedDate":"02-11-2026","responseDateTime":"21-11-2026 19:58:34"}}}""");

        // Act
        var result = await StatusAsync(CreateSut());

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenSlowResponse(Path);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await StatusAsync(CreateSut(timeout: TimeSpan.FromSeconds(0.1)));

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(WhiteListWireMockServerExtensions.GaveUpWithin);
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task GetVatStatusByNipAsync_CancelledBeforeTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.GivenSlowResponse(Path);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();
        var sut = CreateSut();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(async () => _ = await sut.GetVatStatusByNipAsync(Nip, Date, cts.Token));

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(WhiteListWireMockServerExtensions.GaveUpWithin);
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}
