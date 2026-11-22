using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// ADR-0017: IHttpClientFactory logs the request URI, which carries the NIP. The named client replaces its loggers with ones that write
// the display path, so no entry (message, scope or state) holds the identifier. TestCommon's RecordingTelemetry does not see these entries.
public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddOpenUrzednikMFWhiteList_RequestWithNipInThePath_LogsNoIdentifier()
    {
        // Arrange
        var loggerProvider = new LeakRecordingLoggerProvider();
        using var provider = BuildProvider(out _, services => services.AddLogging(logging => logging
            .AddProvider(loggerProvider)
            .SetMinimumLevel(LogLevel.Trace)));

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        AssertNoIdentifier(loggerProvider, Nip);
    }

    [Fact]
    public async Task AddKeyedOpenUrzednikMFWhiteList_RequestWithNipInThePath_LogsNoIdentifier()
    {
        // Arrange
        var loggerProvider = new LeakRecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Trace));
        services.AddKeyedOpenUrzednikMFWhiteList("gateway")
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(Json(SearchNipJson)));
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredKeyedService<IWhiteListClient>("gateway").SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        AssertNoIdentifier(loggerProvider, Nip);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, """{"message":"Nieprawidlowy NIP 3245174504","code":"WL-115"}""")]
    [InlineData((HttpStatusCode)429, """{"message":"Limit dla 3245174504","code":"WL-191"}""")]
    [InlineData(HttpStatusCode.InternalServerError, """{"message":"NIP 3245174504","code":"WL-197"}""")]
    public async Task AddOpenUrzednikMFWhiteList_ErrorEchoingTheNip_LogsNoIdentifier(HttpStatusCode statusCode, string body)
    {
        // Arrange
        var loggerProvider = new LeakRecordingLoggerProvider();
        using var provider = BuildProvider(out _, services => services.AddLogging(logging => logging
            .AddProvider(loggerProvider)
            .SetMinimumLevel(LogLevel.Trace)),
            new HttpResponseMessage(statusCode) { Content = new StringContent(body) });

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        AssertNoIdentifier(loggerProvider, Nip);
    }

    private static void AssertNoIdentifier(LeakRecordingLoggerProvider loggerProvider, string identifier)
    {
        var entries = loggerProvider.Entries;
        // The test would pass for nothing if IHttpClientFactory logged nothing.
        entries.ShouldContain(entry => entry.Category.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal));

        // The identifier as given, and any 5 of its consecutive digits, in every text a log sink received.
        var needles = new List<string> { identifier };
        for (var start = 0; start + 5 <= identifier.Length; start++)
            needles.Add(identifier.Substring(start, 5));

        foreach (var entry in entries)
        {
            foreach (var text in entry.Texts)
            {
                foreach (var needle in needles)
                    text.ShouldNotContain(needle, Case.Sensitive, $"{entry.Category}: {text}");
            }
        }
    }
}
