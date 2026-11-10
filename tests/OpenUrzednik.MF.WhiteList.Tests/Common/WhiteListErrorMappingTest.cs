using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

// Shared helpers; tests are in one file per method. The mapping runs inside a real RestRequestExecutor, as the clients use it, so the
// profile, the display path and the body read are exercised too. Identifiers are fake.
public sealed partial class WhiteListErrorMappingTest : IDisposable
{
    private const string RelativePath = "search/nip/1234567890";
    private const string DisplayPath = "search/nip/{nip}";
    private const string RequestPath = "https://wykazpodatnikowvat.mf.gov.pl/api/search/nip/{nip}";

    // 23:00 in Warsaw (CET), one hour before midnight.
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 22, 0, 0, TimeSpan.Zero);

    private readonly List<HttpClient> _httpClients = [];

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
    }

    private async Task<OpenUrzednikError> SendAsync(HttpResponseMessage response)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(response));
        _httpClients.Add(httpClient);
        var connection = WhiteListConnection.CreateSearch(httpClient, null, new OpenUrzednikTelemetry(), new FakeTimeProvider(Now));

        var result = await connection.GetAsync(RelativePath, DisplayPath, WhiteListJsonContext.Default.ErrorDto, TestContext.Current.CancellationToken);

        return result.Errors.ShouldHaveSingleItem();
    }

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string? body)
    {
        var response = new HttpResponseMessage(statusCode);
        if (body is not null)
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return response;
    }

    private static string Body(string code, string message) => $$"""{"message":"{{message}}","code":"{{code}}"}""";
}
