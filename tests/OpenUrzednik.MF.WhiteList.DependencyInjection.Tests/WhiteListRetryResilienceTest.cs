#if NET
using System.Net;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.DependencyInjection.Tests;

// The snippet of WhiteListRetry's documentation in AddStandardResilienceHandler's ShouldHandle, with the real handler, so a change in
// Microsoft's API that breaks the snippet fails here. The retries are immediate.
public sealed class WhiteListRetryResilienceTest
{
    private const string Nip = "3245174504";
    private static readonly DateTime Date = new(2026, 11, 2);
    private static readonly string SearchNipJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "search-nip.json"));

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string body)
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static string Error(string code) => $$"""{"message":"Komunikat","code":"{{code}}"}""";

    // Registers the clients with the standard handler and the snippet; the primary handler is the given one.
    private static ServiceProvider Build(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var services = new ServiceCollection();
        services.AddOpenUrzednikMFWhiteList()
            .ConfigurePrimaryHttpMessageHandler(() => new DelegatingStubHttpMessageHandler((request, _) => send(request)))
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.Delay = TimeSpan.Zero;
                options.Retry.UseJitter = false;
                options.Retry.ShouldHandle = args => args.Outcome.Result is { } response
                    ? WhiteListRetry.ShouldRetryAsync(response, args.Context.CancellationToken)
                    : ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome));
            });
        return services.BuildServiceProvider();
    }

    // Answers with the given responses in order (the last one repeats) and counts the requests.
    private static ServiceProvider Build(out Func<int> requests, params Func<HttpResponseMessage>[] answers)
    {
        var count = 0;
        requests = () => Volatile.Read(ref count);
        return Build(_ =>
        {
            var index = Math.Min(Interlocked.Increment(ref count) - 1, answers.Length - 1);
            return Task.FromResult(answers[index]());
        });
    }

    [Theory]
    [InlineData(400, "WL-195")]
    [InlineData(400, "WL-196")]
    [InlineData(503, "WL-100")]
    [InlineData(408, "WL-100")]
    public async Task Retry_RetryableAnswer_SucceedsOnTheSecondRequest(int statusCode, string code)
    {
        // Arrange
        using var provider = Build(out var requests, () => Response((HttpStatusCode)statusCode, Error(code)), () => Response(HttpStatusCode.OK, SearchNipJson));

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(string.Join("; ", result.Errors.Select(error => error.Message)));
        requests().ShouldBe(2);
    }

    [Fact]
    public async Task Retry_ExceptionFromTheHandler_FallsBackToTheTransientPredicate()
    {
        // Arrange
        var count = 0;
        using var provider = Build(_ => Interlocked.Increment(ref count) == 1
            ? throw new HttpRequestException("Connection reset.")
            : Task.FromResult(Response(HttpStatusCode.OK, SearchNipJson)));

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        count.ShouldBe(2);
    }

    [Theory]
    [InlineData(429, "WL-191")]
    [InlineData(500, "WL-197")]
    [InlineData(400, "WL-115")]
    public async Task Retry_AnswerNotWorthRetrying_IsReturnedAfterOneRequestWithItsCode(int statusCode, string code)
    {
        // Arrange
        using var provider = Build(out var requests, () => Response((HttpStatusCode)statusCode, Error(code)));

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert: the filter left the body readable, so the error mapping still finds the code
        requests().ShouldBe(1);
        result.Errors.ShouldHaveSingleItem().ShouldBeAssignableTo<IWhiteListErrorCode>().WhiteListCode.ShouldBe(code);
    }

    [Fact]
    public async Task Retry_NotFound_IsReturnedAfterOneRequest()
    {
        // Arrange
        using var provider = Build(out var requests, () => Response(HttpStatusCode.NotFound, string.Empty));

        // Act
        var result = await provider.GetRequiredService<IWhiteListClient>().SearchByNipAsync(Nip, Date, TestContext.Current.CancellationToken);

        // Assert
        requests().ShouldBe(1);
        result.IsFailure.ShouldBeTrue();
    }
}
#endif
