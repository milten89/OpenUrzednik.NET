using System.Diagnostics;
using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList.WireMock;

// The batch searches against captured answers, and the ADR-0002 error paths of a batch (ADR-0023). The identifiers in requests are fake,
// with valid check digits, as many as the captured answer has entries.
public partial class WhiteListClientWireMockTest
{
    private const string Nip2 = "3169973097";
    private const string Regon2 = "489998560";
    private const string BankAccount2 = "26059629673518324991153273";
    private const string NipsPath = "/api/search/nips/" + Nip + "," + Nip2 + "," + Nip + "," + Nip2;
    private const string RegonsPath = "/api/search/regons/" + Regon + "," + Regon2 + "," + Regon;
    private const string BankAccountsPath = "/api/search/bank-accounts/" + BankAccount + "," + BankAccount2;

    private static readonly string[] Nips = [Nip, Nip2, Nip, Nip2];
    private static readonly DateTimeOffset BatchAnswered = new(2026, 11, 21, 8, 48, 50, TimeSpan.FromHours(1));

    [Fact]
    public async Task SearchByNipsAsync_Returns200_MapsEveryEntryInOrderWithTheProof()
    {
        // Arrange
        _server.GivenFixture(NipsPath, Day, "search-nips.json");

        // Act
        var result = await CreateSut().SearchByNipsAsync(["PL 736-812-54-16", Nip2, Nip, Nip2], Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entries = result.Value.Entries;
        entries.Select(e => e.Identifier).ShouldBe(["3245174504", "5260250274", "1234567890", "3245174504"]);
        entries[0].Subjects.ShouldHaveSingleItem().Name.ShouldBe("Nazwa Firmy 1");
        entries[3].ShouldBe(entries[0]);
        var notOnList = entries[1].Error.ShouldBeOfType<WhiteListNotFoundError>();
        notOnList.ShouldBeAssignableTo<NotFoundError>();
        notOnList.RequestId.ShouldBe("r5wrK-99b0ck2");
        notOnList.Metadata[WhiteListNotFoundError.RequestDateTimeMetadataKey].ShouldBe("2026-11-21T08:48:50+01:00");
        var rejected = entries[2].Error.ShouldBeOfType<WhiteListQueryRejectedError>();
        rejected.WhiteListCode.ShouldBe("WL-115");
        rejected.Message.ShouldBe("MF White List API rejected the identifier: WL-115 Nieprawidłowy NIP.");
        result.Value.RequestId.ShouldBe("r5wrK-99b0ck2");
        result.Value.RequestDateTime.ShouldBe(BatchAnswered);
        _server.ShouldHaveReceivedGet(NipsPath, Day);
    }

    [Fact]
    public async Task SearchByRegonsAsync_Returns200_MapsEveryEntryInOrderWithTheProof()
    {
        // Arrange
        _server.GivenFixture(RegonsPath, Day, "search-regons.json");

        // Act
        var result = await CreateSut().SearchByRegonsAsync(["593-728-162", Regon2, Regon], Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entries = result.Value.Entries;
        entries.Select(e => e.Identifier).ShouldBe(["216973362", "755016841", "123456789"]);
        entries[0].Subjects.ShouldHaveSingleItem().Regon.ShouldBe("216973362");
        entries[1].Error.ShouldBeOfType<WhiteListNotFoundError>().RequestId.ShouldBe("K4oSY-99b0ck2");
        entries[2].Error.ShouldBeOfType<WhiteListQueryRejectedError>().WhiteListCode.ShouldBe("WL-107");
        _server.ShouldHaveReceivedGet(RegonsPath, Day);
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_Returns200_MapsEveryEntryInOrderWithTheProof()
    {
        // Arrange
        _server.GivenFixture(BankAccountsPath, Day, "search-bank-accounts.json");

        // Act
        var result = await CreateSut().SearchByBankAccountsAsync(["PL61 1090 1014 0000 0712 1981 2874", BankAccount2], Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entries = result.Value.Entries;
        entries.Select(e => e.Identifier).ShouldBe(["70506405335016096312945164", "61101000000000000000000000"]);
        entries[0].Error.ShouldBeOfType<WhiteListNotFoundError>().RequestId.ShouldBe("3h7uz-99b0ck2");
        entries[1].Error.ShouldBeOfType<WhiteListQueryRejectedError>().WhiteListCode.ShouldBe("WL-111");
        result.Value.RequestDateTime.ShouldBe(BatchAnswered);
        _server.ShouldHaveReceivedGet(BankAccountsPath, Day);
    }

    [Fact]
    public async Task SearchByNipsAsync_Returns400Wl130_ReturnsBadRequestErrorWithTheCodeAndMessage()
    {
        // Arrange: the API's answer to 31 identifiers, which the client never sends
        _server.GivenFixture(NipsPath, Day, "error-400-wl-130.json", HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>();
        error.WhiteListCode.ShouldBe("WL-130");
        error.Message.ShouldBe($"MF White List API rejected the request to {_server.Urls[0]}/api/search/nips/{{nips}}: WL-130 Przekroczono maksymalną liczbę argumentów zapytania.");
        error.Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe(400);
    }

    [Fact]
    public async Task SearchByRegonsAsync_Returns400_ReturnsBadRequestErrorWithTheRouteTemplate()
    {
        // Arrange
        _server.GivenFixture(RegonsPath, Day, "error-400-wl-130.json", HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByRegonsAsync([Regon, Regon2, Regon], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>().Message.ShouldContain("/api/search/regons/{regons}");
    }

    [Fact]
    public async Task SearchByBankAccountsAsync_Returns400_ReturnsBadRequestErrorWithTheRouteTemplate()
    {
        // Arrange
        _server.GivenFixture(BankAccountsPath, Day, "error-400-wl-130.json", HttpStatusCode.BadRequest);

        // Act
        var result = await CreateSut().SearchByBankAccountsAsync([BankAccount, BankAccount2], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListBadRequestError>().Message.ShouldContain("/api/search/bank-accounts/{accounts}");
    }

    [Fact]
    public async Task SearchByNipsAsync_Returns429WithRetryAfter_ReturnsRateLimitErrorWithTheServersDelay()
    {
        // Arrange
        _server.GivenRetryAfter(NipsPath, "30");

        // Act
        var result = await CreateSut().SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        error.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        error.WhiteListCode.ShouldBe("WL-191");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SearchByNipsAsync_Returns5xx_ReturnsServiceUnavailableError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(NipsPath, statusCode);

        // Act
        var result = await CreateSut().SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceUnavailableError>()
            .Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task SearchByNipsAsync_Returns401Or403_ReturnsUnauthorizedError(HttpStatusCode statusCode)
    {
        // Arrange
        _server.GivenStatus(NipsPath, statusCode);

        // Act
        var result = await CreateSut().SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpUnauthorizedError>()
            .Metadata[HttpStatusExtensions.StatusCodeMetadataKey].ShouldBe((int)statusCode);
    }

    [Fact]
    public async Task SearchByNipsAsync_Returns404_ReturnsNotFoundErrorWithoutTheNips()
    {
        // Arrange
        _server.GivenStatus(NipsPath, HttpStatusCode.NotFound);

        // Act
        var result = await CreateSut().SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<HttpNotFoundError>();
        error.Message.ShouldNotContain(Nip);
        error.Message.ShouldNotContain(Nip2);
    }

    [Fact]
    public async Task SearchByNipsAsync_Returns200WithMalformedJson_ReturnsSerializationError()
    {
        // Arrange: hand-written, the API can't produce it
        _server.GivenBody(NipsPath, Day, """{"result":{"entries":[{"identifier":"3245174504","subjects":[""");

        // Act
        var result = await CreateSut().SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>().ToException().ShouldBeOfType<OpenUrzednikSerializationException>();
    }

    [Fact]
    public async Task SearchByNipsAsync_Returns200WithAnotherNumberOfEntries_ReturnsSerializationError()
    {
        // Arrange: the captured answer to four NIPs, for a request of two
        _server.GivenFixture("/api/search/nips/" + Nip + "," + Nip2, Day, "search-nips.json");

        // Act
        var result = await CreateSut().SearchByNipsAsync([Nip, Nip2], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<SerializationError>()
            .Message.ShouldBe("MF White List API response has another number of 'entries' than the identifiers sent.");
    }

    [Fact]
    public async Task SearchByNipsAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenSlowResponse(NipsPath);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CreateSut(timeout: TimeSpan.FromSeconds(0.1)).SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(WhiteListWireMockServerExtensions.GaveUpWithin);
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task SearchByNipsAsync_CancelledBeforeTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.GivenSlowResponse(NipsPath);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await CreateSut().SearchByNipsAsync(Nips, Date, cts.Token));
    }

    [Fact]
    public async Task SearchByNipsAsync_ConnectionFails_ReturnsConnectionFailedError()
    {
        // Arrange
        var sut = CreateSut();
        _server.Stop();

        // Act
        var result = await sut.SearchByNipsAsync(Nips, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ConnectionFailedError>().Exception.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SearchByNipsAsync_EmptyBatch_SendsNothing()
    {
        // Arrange: the ministry's firewall answers an empty batch with 200 and an HTML page, so the client never sends one
        var sut = CreateSut();

        // Act
        var result = await sut.SearchByNipsAsync([], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<int>>().Name.ShouldBe("nips");
        _server.LogEntries.ShouldBeEmpty();
    }
}
