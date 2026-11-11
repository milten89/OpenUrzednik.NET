using System.Net;
using System.Reflection;

using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests;

// Library code awaits with ConfigureAwait(false) (.claude/rules/code-conventions.md), so no client method resumes on the caller's SynchronizationContext:
// a caller that blocks on the task in a UI or ASP.NET (.NET Framework) context would deadlock otherwise. The rows come from the
// clients' public methods, so a new method fails here until it has its call below.
public partial class ClientConventionsTest
{
    private static readonly Type[] Clients = [typeof(NbpCurrencyExchangeRateClient), typeof(NbpExchangeRateTableClient), typeof(NbpGoldPriceClient)];

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
        // The response arrives asynchronously, so every await in the client and its pipeline suspends.
        using var httpClient = new HttpClient(new AsyncStubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound)));
        var rates = new NbpCurrencyExchangeRateClient(httpClient, timeProvider: TestClock.FarFuture());
        var tables = new NbpExchangeRateTableClient(httpClient, timeProvider: TestClock.FarFuture());
        var gold = new NbpGoldPriceClient(httpClient, timeProvider: TestClock.FarFuture());
        var date = new DateTime(2026, 10, 2);
        var from = date.AddDays(-7);
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => method switch
        {
            "NbpCurrencyExchangeRateClient.GetLatestAsync(currency, table)" => rates.GetLatestAsync("USD", TableType.A, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetTopCountAsync(currency, topCount, table)" => rates.GetTopCountAsync("USD", 5, TableType.A, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetTodayAsync(currency, table)" => rates.GetTodayAsync("USD", TableType.A, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetAsync(currency, date, table)" => rates.GetAsync("USD", date, TableType.A, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetAsync(currency, from, to, table)" => rates.GetAsync("USD", from, date, TableType.A, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetLastPublishedBeforeAsync(currency, date, table)" => rates.GetLastPublishedBeforeAsync("USD", date, TableType.A, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetBuySellLatestAsync(currency)" => rates.GetBuySellLatestAsync("USD", cancellationToken),
            "NbpCurrencyExchangeRateClient.GetBuySellTopCountAsync(currency, topCount)" => rates.GetBuySellTopCountAsync("USD", 5, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetBuySellTodayAsync(currency)" => rates.GetBuySellTodayAsync("USD", cancellationToken),
            "NbpCurrencyExchangeRateClient.GetBuySellAsync(currency, date)" => rates.GetBuySellAsync("USD", date, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetBuySellAsync(currency, from, to)" => rates.GetBuySellAsync("USD", from, date, cancellationToken),
            "NbpCurrencyExchangeRateClient.GetBuySellLastPublishedBeforeAsync(currency, date)" => rates.GetBuySellLastPublishedBeforeAsync("USD", date, cancellationToken),
            "NbpExchangeRateTableClient.GetLatestAsync(table)" => tables.GetLatestAsync(TableType.A, cancellationToken),
            "NbpExchangeRateTableClient.GetTopCountAsync(table, topCount)" => tables.GetTopCountAsync(TableType.A, 5, cancellationToken),
            "NbpExchangeRateTableClient.GetTodayAsync(table)" => tables.GetTodayAsync(TableType.A, cancellationToken),
            "NbpExchangeRateTableClient.GetAsync(table, date)" => tables.GetAsync(TableType.A, date, cancellationToken),
            "NbpExchangeRateTableClient.GetAsync(table, from, to)" => tables.GetAsync(TableType.A, from, date, cancellationToken),
            "NbpExchangeRateTableClient.GetLastPublishedBeforeAsync(table, date)" => tables.GetLastPublishedBeforeAsync(TableType.A, date, cancellationToken),
            "NbpExchangeRateTableClient.GetBuySellLatestAsync()" => tables.GetBuySellLatestAsync(cancellationToken),
            "NbpExchangeRateTableClient.GetBuySellTopCountAsync(topCount)" => tables.GetBuySellTopCountAsync(5, cancellationToken),
            "NbpExchangeRateTableClient.GetBuySellTodayAsync()" => tables.GetBuySellTodayAsync(cancellationToken),
            "NbpExchangeRateTableClient.GetBuySellAsync(date)" => tables.GetBuySellAsync(date, cancellationToken),
            "NbpExchangeRateTableClient.GetBuySellAsync(from, to)" => tables.GetBuySellAsync(from, date, cancellationToken),
            "NbpExchangeRateTableClient.GetBuySellLastPublishedBeforeAsync(date)" => tables.GetBuySellLastPublishedBeforeAsync(date, cancellationToken),
            "NbpGoldPriceClient.GetLatestAsync()" => gold.GetLatestAsync(cancellationToken),
            "NbpGoldPriceClient.GetTopCountAsync(topCount)" => gold.GetTopCountAsync(5, cancellationToken),
            "NbpGoldPriceClient.GetTodayAsync()" => gold.GetTodayAsync(cancellationToken),
            "NbpGoldPriceClient.GetAsync(date)" => gold.GetAsync(date, cancellationToken),
            "NbpGoldPriceClient.GetAsync(from, to)" => gold.GetAsync(from, date, cancellationToken),
            "NbpGoldPriceClient.GetLastPublishedBeforeAsync(date)" => gold.GetLastPublishedBeforeAsync(date, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "A new public client method: add its call here."),
        });

        // Assert
        posts.ShouldBe(0);
    }
}
