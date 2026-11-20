using System.Diagnostics;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.IntegrationTests.Nbp.WireMock;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

using WireMock.Server;

namespace OpenUrzednik.IntegrationTests.MF.WhiteList.WireMock;

// The three searches against captured answers (ADR-0023). The identifiers in requests are fake, with valid check digits; WireMock
// answers by path, so the captured answer needn't be about them.
public partial class WhiteListClientWireMockTest : IDisposable
{
    private const string Nip = "7368125416";
    private const string Regon = "593728162";
    private const string BankAccount = "61109010140000071219812874";
    private const string NipPath = "/api/search/nip/" + Nip;
    private const string RegonPath = "/api/search/regon/" + Regon;
    private const string BankAccountPath = "/api/search/bank-account/" + BankAccount;
    private const string Day = "2026-11-02";

    // 12:00 on 2026-11-20 in Warsaw: the date is in the list's range, and midnight is 12 hours away.
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Date = new(2026, 11, 2);

    private static readonly Subject MinistryOfFinance = new("MINISTERSTWO FINANSÓW")
    {
        Nip = "5260250274",
        VatStatus = VatStatus.Active,
        Regon = "000002217",
        ResidenceAddress = "ŚWIĘTOKRZYSKA 12, 00-916 WARSZAWA",
        RegistrationLegalDate = new DateTime(1996, 1, 1),
        AccountNumbers =
        [
            "59101010100075912222000000", "10101010100038252231000000", "49101010100038122225000000", "60101010100038252230000000",
            "09101010100075912223000000", "12101010100075912221000000", "18101010100046042231000000", "18101014690032611391200000",
            "64101010100075911339200000", "56113010170020117278200033", "07113010170200000000138215", "64101010100037242231000000",
            "06101010100075912225000000", "41101010100071712231000000", "03101010100075912227000000", "59113010880001312263200002",
            "78113010170200000000137228",
        ],
        HasVirtualAccounts = true,
    };

    private readonly WireMockServer _server = LoopbackWireMockServer.StartHttps();
    private readonly List<HttpClient> _httpClients = [];

    private WhiteListClient CreateSut(TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        // A plain HttpClient: the host root and timeout go to the client, the HttpClient isn't configured.
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = _server.Urls[0] + "/", Timeout = timeout }, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task SearchByNipAsync_Returns200WithASubject_MapsItWithTheProof()
    {
        // Arrange
        _server.GivenFixture(NipPath, Day, "search-nip.json");

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectSearchResult(
            new Subject("Nazwa Firmy 1")
            {
                Nip = "3245174504",
                VatStatus = VatStatus.Active,
                Regon = "79156739856513",
                Krs = "1234567890",
                ResidenceAddress = "TAKA a Taka 191/369, 15-360 Miejscowość 1",
                RegistrationLegalDate = new DateTime(2017, 11, 26),
                RemovalDate = new DateTime(2017, 10, 18),
                RemovalBasis = "Ustawa o podatku od towarów i usług Art. 196",
                HasVirtualAccounts = false,
            },
            "EftiD-99alll1",
            new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1))));
        _server.ShouldHaveReceivedGet(NipPath, Day);
    }

    [Fact]
    public async Task SearchByRegonAsync_Returns200WithASubject_MapsItWithTheProof()
    {
        // Arrange
        _server.GivenFixture(RegonPath, Day, "search-regon.json");

        // Act
        var result = await CreateSut().SearchByRegonAsync("593-728-162", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectSearchResult(MinistryOfFinance, "I854A-99allna", new DateTimeOffset(2026, 11, 20, 22, 45, 22, TimeSpan.FromHours(1))));
        _server.ShouldHaveReceivedGet(RegonPath, Day);
    }

    [Fact]
    public async Task SearchByBankAccountAsync_Returns200WithSubjects_MapsThemWithTheProof()
    {
        // Arrange
        _server.GivenFixture(BankAccountPath, Day, "search-bank-account.json");

        // Act
        var result = await CreateSut().SearchByBankAccountAsync("PL61 1090 1014 0000 0712 1981 2874", Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectsSearchResult([MinistryOfFinance], "R43vM-99alln9", new DateTimeOffset(2026, 11, 20, 22, 45, 21, TimeSpan.FromHours(1))));
        _server.ShouldHaveReceivedGet(BankAccountPath, Day);
    }

    [Fact]
    public async Task SearchByNipAsync_Returns200WithoutASubject_ReturnsNotFoundWithTheProof()
    {
        // Arrange
        _server.GivenFixture(NipPath, Day, "search-nip-not-found.json");

        // Act
        var result = await CreateSut().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.ShouldBeAssignableTo<NotFoundError>();
        error.RequestId.ShouldBe("rd9zq-99alll1");
        error.RequestDateTime.ShouldBe(new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1)));
        error.Metadata[WhiteListNotFoundError.RequestDateTimeMetadataKey].ShouldBe("2026-11-20T22:44:25+01:00");
    }

    [Fact]
    public async Task SearchByRegonAsync_Returns200WithoutASubject_ReturnsNotFoundWithTheProof()
    {
        // Arrange: the answer without a subject has the same shape for a REGON as for a NIP
        _server.GivenFixture(RegonPath, Day, "search-nip-not-found.json");

        // Act
        var result = await CreateSut().SearchByRegonAsync(Regon, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>().RequestId.ShouldBe("rd9zq-99alll1");
    }

    [Fact]
    public async Task SearchByBankAccountAsync_Returns200WithNoSubjects_ReturnsNotFoundWithTheProof()
    {
        // Arrange
        _server.GivenFixture(BankAccountPath, Day, "search-bank-account-not-found.json");

        // Act
        var result = await CreateSut().SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.RequestId.ShouldBe("Meupq-99alll1");
        error.RequestDateTime.ShouldBe(new DateTimeOffset(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1)));
    }

    [Fact]
    public async Task SearchByNipAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenSlowResponse(NipPath);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CreateSut(timeout: TimeSpan.FromSeconds(0.1)).SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(WhiteListWireMockServerExtensions.GaveUpWithin);
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task SearchByNipAsync_CancelledBeforeTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.GivenSlowResponse(NipPath);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(async () => await CreateSut().SearchByNipAsync(Nip, Date, cts.Token));

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
