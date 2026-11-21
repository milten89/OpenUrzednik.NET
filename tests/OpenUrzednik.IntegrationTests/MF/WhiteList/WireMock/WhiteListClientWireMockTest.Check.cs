using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.MF.WhiteList.Check;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList.WireMock;

// The two checks of an account number against captured answers and every ADR-0002 error path (ADR-0023). Each test runs for the NIP and
// the REGON check: the answers have the same shape, so one fixture serves both, as WireMock answers by path.
public partial class WhiteListClientWireMockTest
{
    private const string CheckNipPath = "/api/check/nip/" + Nip + "/bank-account/" + BankAccount;
    private const string CheckRegonPath = "/api/check/regon/" + Regon + "/bank-account/" + BankAccount;

    public static TheoryData<string> CheckKinds => ["nip", "regon"];

    private static string CheckPath(string kind) => kind == "nip" ? CheckNipPath : CheckRegonPath;

    private static string CheckDisplayPath(string kind) => $"/api/check/{kind}/{{{kind}}}/bank-account/{{account}}";

    private Task<OpenUrzednikResult<AccountCheckResult>> CheckAsync(WhiteListClient sut, string kind, DateTime? date = null)
        => kind == "nip"
            ? sut.CheckNipBankAccountAsync("PL 736-812-54-16", "PL61 1090 1014 0000 0712 1981 2874", date ?? Date, TestContext.Current.CancellationToken)
            : sut.CheckRegonBankAccountAsync("593-728-162", "PL61 1090 1014 0000 0712 1981 2874", date ?? Date, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData("nip", "check-nip-assigned.json", true, "PxqUV-99b2nhh", 17)]
    [InlineData("nip", "check-nip-not-assigned.json", false, "wAaOW-99b2nhg", 16)]
    [InlineData("nip", "check-nip-not-on-list.json", false, "hifVE-99b2nhg", 16)]
    [InlineData("regon", "check-regon-assigned.json", true, "inirF-99b2nhh", 17)]
    [InlineData("regon", "check-regon-not-assigned.json", false, "N4ZPn-99b2nhg", 16)]
    public async Task CheckBankAccountAsync_Returns200_MapsTheVerdictWithTheProof(string kind, string fixture, bool assigned, string requestId, int second)
    {
        // Arrange
        _server.GivenFixture(CheckPath(kind), Day, fixture);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new AccountCheckResult(assigned, requestId, new DateTimeOffset(2026, 11, 21, 18, 14, second, TimeSpan.FromHours(1))));
        _server.ShouldHaveReceivedGet(CheckPath(kind), Day);
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns200WithAnUnknownVerdict_ReturnsSerializationError(string kind)
    {
        // Arrange: the captured answer with a verdict the contract doesn't list
        _server.GivenBody(CheckPath(kind), Day, WhiteListFixtures.Load("check-nip-assigned.json").Replace("\"TAK\"", "\"MOZE\""));

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
        error.Message.ShouldBe("MF White List API response has an 'accountAssigned' that is neither 'TAK' nor 'NIE'.");
        error.Message.ShouldNotContain("MOZE");
    }

    [Theory]
    [InlineData("nip", "error-400-wl-115.json", "WL-115")]
    [InlineData("nip", "error-400-wl-118.json", "WL-118")]
    [InlineData("nip", "error-400-wl-103.json", "WL-103")]
    [InlineData("regon", "error-400-wl-107.json", "WL-107")]
    [InlineData("regon", "error-400-wl-111.json", "WL-111")]
    public async Task CheckBankAccountAsync_Returns400_ReturnsBadRequestErrorWithTheCodeAndTheDisplayPath(string kind, string fixture, string code)
    {
        // Arrange
        _server.GivenFixture(CheckPath(kind), Day, fixture, HttpStatusCode.BadRequest);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.WhiteListCode.ShouldBe(code);
        error.Message.ShouldContain(CheckDisplayPath(kind));
        error.Message.ShouldNotContain(BankAccount);
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns400Wl195_ReturnsServiceUnavailableError(string kind)
    {
        // Arrange: the body is built from the error table (WL-195, "repeat the query"), not captured
        _server.GivenFixture(CheckPath(kind), Day, "error-400-wl-195.from-error-table.json", HttpStatusCode.BadRequest);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>();
        error.WhiteListCode.ShouldBe("WL-195");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns429Wl191WithoutRetryAfter_ReturnsRateLimitErrorUntilMidnight(string kind)
    {
        // Arrange: the body is built from the error table (WL-191), not captured
        _server.GivenFixture(CheckPath(kind), Day, "error-429-wl-191.from-error-table.json", HttpStatusCode.TooManyRequests);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.WhiteListCode.ShouldBe("WL-191");
        error.RetryAfter.ShouldBe(TimeSpan.FromHours(12));
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(429);
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns429WithRetryAfter_ReturnsRateLimitErrorWithTheServersDelay(string kind)
    {
        // Arrange
        _server.GivenRetryAfter(CheckPath(kind), "30");

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>().RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns500Wl197_ReturnsUnknownError(string kind)
    {
        // Arrange: the body is built from the error table (WL-197), not captured
        _server.GivenFixture(CheckPath(kind), Day, "error-500-wl-197.from-error-table.json", HttpStatusCode.InternalServerError);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListUnknownError>();
        error.WhiteListCode.ShouldBe("WL-197");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(500);
    }

    [Theory]
    [InlineData("nip", HttpStatusCode.InternalServerError)]
    [InlineData("nip", HttpStatusCode.BadGateway)]
    [InlineData("regon", HttpStatusCode.ServiceUnavailable)]
    [InlineData("regon", HttpStatusCode.GatewayTimeout)]
    public async Task CheckBankAccountAsync_Returns5xx_ReturnsServiceUnavailableError(string kind, HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(CheckPath(kind), statusCode);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>();
        error.WhiteListCode.ShouldBeNull();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [InlineData("nip", HttpStatusCode.Unauthorized)]
    [InlineData("regon", HttpStatusCode.Forbidden)]
    public async Task CheckBankAccountAsync_Returns401Or403_ReturnsUnauthorizedError(string kind, HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(CheckPath(kind), statusCode);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpUnauthorizedError>().Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns404_ReturnsNotFoundErrorWithoutTheIdentifiers(string kind)
    {
        // Arrange
        _server.GivenStatus(CheckPath(kind), HttpStatusCode.NotFound);

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>();
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(404);
        error.Message.ShouldNotContain(BankAccount);
        error.Message.ShouldNotContain(Nip);
        error.Message.ShouldNotContain(Regon);
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_ConnectionFails_ReturnsConnectionFailedError(string kind)
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await CheckAsync(sut, kind);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>().Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns200WithMalformedJson_ReturnsSerializationError(string kind)
    {
        // Arrange: hand-written, the API can't produce it
        _server.GivenBody(CheckPath(kind), Day, """{"result":{"accountAssigned":"TA""");

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().ToException().ShouldBeOfType<OpenUrzednikSerializationException>();
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Returns200WithoutTheProof_ReturnsSerializationError(string kind)
    {
        // Arrange: hand-written, the API can't produce it (requestId is in every captured answer)
        _server.GivenBody(CheckPath(kind), Day, """{"result":{"accountAssigned":"NIE","requestDateTime":"21-11-2026 18:14:16"}}""");

        // Act
        var result = await CheckAsync(CreateSut(), kind);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>();
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_Timeout_ReturnsRequestTimeoutError(string kind)
    {
        // Arrange
        _server.GivenSlowResponse(CheckPath(kind));
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CheckAsync(CreateSut(timeout: TimeSpan.FromSeconds(0.1)), kind);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(WhiteListWireMockServerExtensions.GaveUpWithin);
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Theory]
    [MemberData(nameof(CheckKinds))]
    public async Task CheckBankAccountAsync_CancelledBeforeTimeout_ThrowsOperationCanceledException(string kind)
    {
        // Arrange
        _server.GivenSlowResponse(CheckPath(kind));
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();
        var sut = CreateSut();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(async () => _ = kind == "nip"
            ? await sut.CheckNipBankAccountAsync(Nip, BankAccount, Date, cts.Token)
            : await sut.CheckRegonBankAccountAsync(Regon, BankAccount, Date, cts.Token));

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(WhiteListWireMockServerExtensions.GaveUpWithin);
    }
}
