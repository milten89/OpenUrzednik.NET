using System.Diagnostics;
using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0023, ADR-0025 confirmation: the spans of a real White List client reach an ActivityListener, with the identifier only masked and
// the request path only as its route template. End-to-end through WhiteListClient, so not split into one file per method.
public sealed class WhiteListClientTracingTest : IDisposable
{
    private const string Nip = "7368125416";
    private const string BankAccount = "61109010140000071219812874";
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));
    private static readonly DateTime Date = new(2026, 11, 2);

    private readonly ActivityRecorder _recorder = new();

    public void Dispose() => _recorder.Dispose();

    private WhiteListClient CreateSut(HttpClient httpClient)
        => new(httpClient, null, Clock, new OpenUrzednikTelemetry { TraceSource = new ActivityTraceSource(_recorder.Source) });

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SearchByNipAsync_NotOnTheList_RecordsOperationAndHttpActivitiesWithoutTheNip()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json(HttpStatusCode.OK, """{"result":{"subject":null,"requestDateTime":"20-11-2026 22:44:25","requestId":"rd9zq-99alll1"}}""")));

        // Act
        var result = await CreateSut(httpClient).SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        _recorder.Stopped.Count.ShouldBe(2);
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("mf_white_list.search.nip");
        operation.Kind.ShouldBe(ActivityKind.Internal);
        operation.GetTagItem("mf_white_list.nip").ShouldBe("******5416");
        operation.GetTagItem("mf_white_list.date").ShouldBe("2026-11-02");
        operation.GetTagItem("error.type").ShouldBe("notFound");
        operation.Status.ShouldBe(ActivityStatusCode.Error);
        http.OperationName.ShouldBe("mf_white_list.http.get");
        http.ParentSpanId.ShouldBe(operation.SpanId);
        http.GetTagItem("server.address").ShouldBe("wykazpodatnikowvat.mf.gov.pl");
        http.GetTagItem("url.path").ShouldBe("/api/search/nip/{nip}");
        http.GetTagItem("http.response.status_code").ShouldBe(200);
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("68125"));
    }

    [Fact]
    public async Task SearchByBankAccountAsync_RateLimited_RecordsTheErrorWithoutTheAccount()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json((HttpStatusCode)429, """{"message":"Limit żądań dla tego adresu IP został na dziś wyczerpany.","code":"WL-191"}""")));

        // Act
        var result = await CreateSut(httpClient).SearchByBankAccountAsync(BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("mf_white_list.search.bank_account");
        operation.GetTagItem("mf_white_list.bank_account").ShouldBe("**********************2874");
        operation.GetTagItem("error.type").ShouldBe("rateLimitExceeded");
        http.GetTagItem("url.path").ShouldBe("/api/search/bank-account/{account}");
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("10901"));
    }

    [Fact]
    public async Task SearchByNipsAsync_FailedEntries_RecordsTheCountWithoutErrorOrNips()
    {
        // Arrange: one NIP not on the list and one the API rejected, echoing it; the call itself succeeds
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Json(HttpStatusCode.OK,
            """{"result":{"entries":[{"identifier":"{id}","subjects":[]},{"identifier":"{id}","error":{"message":"Nieprawidłowy NIP {id}.","code":"WL-115"}}],"requestDateTime":"21-11-2026 08:48:50","requestId":"r5wrK-99b0ck2"}}""".Replace("{id}", Nip))));

        // Act
        var result = await CreateSut(httpClient).SearchByNipsAsync([Nip, Nip], Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("mf_white_list.search.nips");
        operation.GetTagItem("mf_white_list.count").ShouldBe(2);
        operation.GetTagItem("mf_white_list.date").ShouldBe("2026-11-02");
        operation.GetTagItem("error.type").ShouldBeNull();
        operation.Status.ShouldBe(ActivityStatusCode.Unset);
        http.GetTagItem("url.path").ShouldBe("/api/search/nips/{nips}");
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("68125"));
    }

    // Every tag value and event tag value of the recorded activities, as text.
    private IEnumerable<string> AllTagsAndEvents()
        => _recorder.Stopped.SelectMany(activity => activity.TagObjects.Select(tag => $"{tag.Value}")
            .Concat(activity.Events.SelectMany(e => e.Tags.Select(tag => $"{tag.Value}")))
            .Append(activity.StatusDescription ?? string.Empty));

    [Fact]
    public async Task CheckNipBankAccountAsync_Answered_RecordsOperationAndHttpActivitiesWithoutNipOrAccount()
    {
        // Arrange: NIE, a success
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json(HttpStatusCode.OK, """{"result":{"accountAssigned":"NIE","requestDateTime":"21-11-2026 18:14:16","requestId":"wAaOW-99b2nhg"}}""")));

        // Act
        var result = await CreateSut(httpClient).CheckNipBankAccountAsync(Nip, BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Value.AccountAssigned.ShouldBeFalse();
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("mf_white_list.check.nip_bank_account");
        operation.GetTagItem("mf_white_list.nip").ShouldBe("******5416");
        operation.GetTagItem("mf_white_list.bank_account").ShouldBe("**********************2874");
        operation.GetTagItem("mf_white_list.date").ShouldBe("2026-11-02");
        operation.GetTagItem("error.type").ShouldBeNull();
        operation.Status.ShouldBe(ActivityStatusCode.Unset);
        http.GetTagItem("url.path").ShouldBe("/api/check/nip/{nip}/bank-account/{account}");
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("68125") && !text.Contains("10901"));
    }

    [Fact]
    public async Task CheckRegonBankAccountAsync_RateLimited_RecordsTheErrorWithoutRegonOrAccount()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json((HttpStatusCode)429, """{"message":"Limit żądań dla tego adresu IP został na dziś wyczerpany.","code":"WL-191"}""")));

        // Act
        var result = await CreateSut(httpClient).CheckRegonBankAccountAsync("593728162", BankAccount, Date, TestContext.Current.CancellationToken);

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListRateLimitExceededError>();
        var http = _recorder.Stopped[0];
        var operation = _recorder.Stopped[1];
        operation.OperationName.ShouldBe("mf_white_list.check.regon_bank_account");
        operation.GetTagItem("mf_white_list.regon").ShouldBe("*****8162");
        operation.GetTagItem("mf_white_list.bank_account").ShouldBe("**********************2874");
        operation.GetTagItem("error.type").ShouldBe("rateLimitExceeded");
        http.GetTagItem("url.path").ShouldBe("/api/check/regon/{regon}/bank-account/{account}");
        AllTagsAndEvents().ShouldAllBe(text => !text.Contains("37281") && !text.Contains("10901"));
    }
}
