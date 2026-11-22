#if NETFRAMEWORK
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenUrzednik.TestCommon;

using Polly;
using Polly.Retry;
using Polly.Timeout;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// The .NET Framework resilience handler of the DI README ("Resilience on .NET Framework"): a retry sends a new request message, which must
// carry the request's properties, because the display path the HttpClient logs write instead of the identifier travels in them (ADR-0017).
public sealed partial class WhiteListServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddHttpMessageHandler_NetFrameworkResilienceHandlerOfTheReadme_LogsNoIdentifier()
    {
        // Arrange
        var loggerProvider = new LeakRecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(loggerProvider).SetMinimumLevel(LogLevel.Trace));
        services.AddOpenUrzednikMFWhiteList()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("""{"message":"NIP 3245174504","code":"WL-197"}"""),
            }))
            .AddHttpMessageHandler(() => new WhiteListResilienceHandler());
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        AssertNoIdentifier(loggerProvider, Nip);
    }

    // The code of the README's snippet, identical to it; change both together.
    private sealed class WhiteListResilienceHandler : DelegatingHandler
    {
        private static readonly ResiliencePipeline<HttpResponseMessage> Pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = args => args.Outcome.Result is { } response
                    ? WhiteListRetry.ShouldRetryAsync(response, args.Context.CancellationToken)
                    : new ValueTask<bool>(args.Outcome.Exception is HttpRequestException or TimeoutRejectedException),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    args.Outcome.Result?.Dispose();
                    return default;
                },
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Pipeline.ExecuteAsync(
                async token =>
                {
                    using var attempt = new HttpRequestMessage(request.Method, request.RequestUri);
                    foreach (var header in request.Headers)
                        attempt.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    foreach (var property in request.Properties)
                        attempt.Properties[property.Key] = property.Value;
                    return await base.SendAsync(attempt, token).ConfigureAwait(false);
                },
                cancellationToken).AsTask();
    }
}
#endif
