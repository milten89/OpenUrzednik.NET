using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

using OpenUrzednik.Http.DependencyInjection;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Http.Tests.DependencyInjection;

public partial class DisplayPathHttpClientLoggingTest
{
    [Fact]
    public void AddOpenUrzednikDisplayPathLogging_NullBuilder_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => DisplayPathHttpClientLogging.AddOpenUrzednikDisplayPathLogging(null!)).ParamName.ShouldBe("builder");
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_DisplayPath_LogsMicrosoftEntriesWithDisplayUri()
    {
        // Arrange
        var recorder = new RecordingLoggerProvider();
        using var provider = BuildProvider(CreateOkHandler(), recorder);

        // Act
        var result = await CreateExecutor(provider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var entries = HttpClientEntries(recorder);
        entries.Count.ShouldBe(4);
        entries[0].ShouldSatisfyAllConditions(
            entry => entry.Category.ShouldBe(LogicalCategory),
            entry => entry.Level.ShouldBe(LogLevel.Information),
            entry => entry.EventId.ShouldBe(new EventId(100, "RequestPipelineStart")),
            entry => entry.Message.ShouldBe($"Start processing HTTP request GET {BaseAddress}search/nip/{{nip}}?*"));
        entries[1].ShouldSatisfyAllConditions(
            entry => entry.Category.ShouldBe(ClientCategory),
            entry => entry.EventId.ShouldBe(new EventId(100, "RequestStart")),
            entry => entry.Message.ShouldBe($"Sending HTTP request GET {BaseAddress}search/nip/{{nip}}?*"));
        entries[2].ShouldSatisfyAllConditions(
            entry => entry.Category.ShouldBe(ClientCategory),
            entry => entry.EventId.ShouldBe(new EventId(101, "RequestEnd")),
            entry => entry.Message.ShouldStartWith("Received HTTP response headers after "),
            entry => entry.Message.ShouldEndWith("ms - 200"));
        entries[3].ShouldSatisfyAllConditions(
            entry => entry.Category.ShouldBe(LogicalCategory),
            entry => entry.EventId.ShouldBe(new EventId(101, "RequestPipelineEnd")),
            entry => entry.Message.ShouldStartWith("End processing HTTP request after "),
            entry => entry.Message.ShouldEndWith("ms - 200"));
        recorder.Entries.ShouldAllBe(entry => !entry.Message.Contains(Identifier));
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_RequestWithoutQuery_WritesTheSameEntriesAsMicrosoftsDefaultLogger()
    {
        // Arrange
        // Parity with the Microsoft.Extensions.Http version this target references; only the elapsed time differs.
        var ours = new RecordingLoggerProvider();
        var microsofts = new RecordingLoggerProvider();
        using var oursProvider = BuildProvider(CreateOkHandler(), ours);
        using var microsoftsProvider = BuildProvider(CreateOkHandler(), microsofts, (_, builder) => builder.RemoveAllLoggers().AddDefaultLogger());

        // Act
        await CreateExecutor(oursProvider).GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);
        await CreateExecutor(microsoftsProvider).GetAsync("rates", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        static IEnumerable<string> Comparable(RecordingLoggerProvider recorder) => HttpClientEntries(recorder)
            .Where(entry => entry.EventId.Id is 100 or 101)
            .Select(entry => $"{entry.Category}|{entry.Level}|{entry.EventId.Id}|{entry.EventId.Name}|{ElapsedPattern.Replace(entry.Message, "after Xms")}");
        Comparable(ours).ShouldBe(Comparable(microsofts));
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_NoDisplayPath_LogsUriWithRedactedQuery()
    {
        // Arrange
        var recorder = new RecordingLoggerProvider();
        using var provider = BuildProvider(CreateOkHandler(), recorder);

        // Act
        await CreateExecutor(provider).GetAsync("rates?date=2026-11-06", TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        HttpClientEntries(recorder)[1].Message.ShouldBe($"Sending HTTP request GET {BaseAddress}rates?*");
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_HttpRequestException_LogsRequestFailedOnBothCategories()
    {
        // Arrange
        var recorder = new RecordingLoggerProvider();
        using var provider = BuildProvider(new StubHttpMessageHandler(new HttpRequestException("Connection refused")), recorder);

        // Act
        await CreateExecutor(provider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        var failed = HttpClientEntries(recorder).Where(entry => entry.EventId.Id == 104).ToList();
        failed.Select(entry => (entry.Category, entry.EventId.Name)).ShouldBe([(ClientCategory, "RequestFailed"), (LogicalCategory, "RequestPipelineFailed")]);
        failed.ShouldAllBe(entry => entry.Level == LogLevel.Information && entry.Message.StartsWith("HTTP request failed after ", StringComparison.Ordinal));
        recorder.Entries.ShouldAllBe(entry => !entry.Message.Contains(Identifier));
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_Timeout_LogsNoEndOrFailure()
    {
        // Arrange
        var recorder = new RecordingLoggerProvider();
        using var provider = BuildProvider(new StubHttpMessageHandler(new TaskCanceledException("The operation was canceled.")), recorder);

        // Act
        await CreateExecutor(provider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        HttpClientEntries(recorder).Select(entry => entry.EventId.Id).ShouldBe([100, 100]);
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_AppDefaultsAddLoggers_KeepsOnlyDisplayPathEntries()
    {
        // Arrange
        // App-wide defaults registered after the provider's client still run before its named configuration.
        var recorder = new RecordingLoggerProvider();
        var appLogger = new CountingHttpClientLogger();
        using var provider = BuildProvider(CreateOkHandler(), recorder,
            (services, _) => services.ConfigureHttpClientDefaults(defaults => defaults.AddDefaultLogger().AddLogger(_ => appLogger)));

        // Act
        await CreateExecutor(provider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        appLogger.Starts.ShouldBe(0);
        HttpClientEntries(recorder).Count.ShouldBe(4);
        recorder.Entries.ShouldAllBe(entry => !entry.Message.Contains(Identifier));
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_AppRestoresDefaultLogger_LogsMicrosoftEntriesWithRequestPath()
    {
        // Arrange
        var recorder = new RecordingLoggerProvider();
        using var provider = BuildProvider(CreateOkHandler(), recorder, (_, builder) => builder.RemoveAllLoggers().AddDefaultLogger());

        // Act
        await CreateExecutor(provider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        HttpClientEntries(recorder).ShouldContain(entry => entry.Category == ClientCategory && entry.Message.Contains("search/nip/" + Identifier));
    }

    [Fact]
    public async Task AddOpenUrzednikDisplayPathLogging_InformationDisabled_LogsNothing()
    {
        // Arrange
        var recorder = new RecordingLoggerProvider();
        using var provider = BuildProvider(CreateOkHandler(), recorder,
            (services, _) => services.AddLogging(logging => logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning)));

        // Act
        await CreateExecutor(provider).GetAsync(IdentifierPath, DisplayPath, TypeInfo, TestContext.Current.CancellationToken);

        // Assert
        HttpClientEntries(recorder).ShouldBeEmpty();
    }

    // Counts the calls an app's own logger would get.
    private sealed class CountingHttpClientLogger : IHttpClientLogger
    {
        public int Starts { get; private set; }

        public object? LogRequestStart(HttpRequestMessage request)
        {
            Starts++;
            return null;
        }

        public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed) { }

        public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed) { }
    }
}
