using System.Net;
using System.Reflection;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

// Library code awaits with ConfigureAwait(false) (.claude/rules/code-conventions.md), so no client method resumes on the caller's
// SynchronizationContext: a caller that blocks on the task in a UI or ASP.NET (.NET Framework) context would deadlock otherwise. The rows
// come from the clients' public methods, so a new method fails here until it has its call below.
public sealed partial class ClientConventionsTest
{
    private static readonly Type[] Clients = [typeof(WhiteListClient)];

    public static TheoryData<string> PublicClientMethods()
    {
        var data = new TheoryData<string>();
        foreach (var client in Clients)
        {
            foreach (var method in client.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => typeof(Task).IsAssignableFrom(m.ReturnType)))
                data.Add($"{client.Name}.{method.Name}({string.Join(", ", method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).Select(p => p.Name))})");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PublicClientMethods))]
    public async Task PublicClientMethod_CallerSynchronizationContext_NeverResumesOnIt(string method)
    {
        // Arrange
        // The response arrives asynchronously, so every await in the client and its pipeline suspends; the answer is "not on the list",
        // which runs the whole path: request, mapping and, for a single search, the not-on-list log; a batch of one gets it as its entry.
        using var httpClient = new HttpClient(new AsyncResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"result":{"subject":null,"subjects":[],"entries":[{"identifier":"0","subjects":[]}],"requestDateTime":"20-11-2026 22:44:25","requestId":"rd9zq-99alll1"}}""",
                Encoding.UTF8, "application/json"),
        }));
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));
        var client = new WhiteListClient(httpClient, null, clock);
        var date = new DateTime(2026, 11, 2);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => method switch
        {
            "WhiteListClient.SearchByNipAsync(nip, date)" => client.SearchByNipAsync("7368125416", date, cancellationToken),
            "WhiteListClient.SearchByRegonAsync(regon, date)" => client.SearchByRegonAsync("593728162", date, cancellationToken),
            "WhiteListClient.SearchByBankAccountAsync(bankAccount, date)" => client.SearchByBankAccountAsync("61109010140000071219812874", date, cancellationToken),
            "WhiteListClient.SearchByNipsAsync(nips, date)" => client.SearchByNipsAsync(["7368125416"], date, cancellationToken),
            "WhiteListClient.SearchByRegonsAsync(regons, date)" => client.SearchByRegonsAsync(["593728162"], date, cancellationToken),
            "WhiteListClient.SearchByBankAccountsAsync(bankAccounts, date)" => client.SearchByBankAccountsAsync(["61109010140000071219812874"], date, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "A new public client method: add its call here."),
        });

        // Assert
        posts.ShouldBe(0);
    }

    // Completes asynchronously and away from the caller's SynchronizationContext, so the client's awaits really suspend.
    private sealed class AsyncResponseHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            return respond();
        }
    }
}
