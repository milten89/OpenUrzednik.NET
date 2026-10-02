using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

using Shouldly;

namespace OpenUrzednik.Nbp.DependencyInjection.Tests;

public sealed partial class ResilienceRejectionHandlerTest
{
    [Fact]
    public async Task SendAsync_InnerSucceeds_ReturnsResponse()
    {
        // Arrange
        var response = new HttpResponseMessage();
        using var sut = CreateSut((_, _) => Task.FromResult(response));
        using var request = CreateRequest();

        // Act
        var result = await sut.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeSameAs(response);
    }

    [Fact]
    public async Task SendAsync_TimeoutRejected_ThrowsTaskCanceledExceptionWithoutInnerTimeoutException()
    {
        // Arrange
        var rejected = new TimeoutRejectedException("The operation didn't complete within the allowed timeout of '00:00:30'.");
        using var sut = CreateSut((_, _) => throw rejected);
        using var request = CreateRequest();

        // Act
        // Record.ExceptionAsync awaits the task; Shouldly would replace the exception of a cancelled task with a new one.
        var exception = await Record.ExceptionAsync(() => sut.SendAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeOfType<TaskCanceledException>().InnerException.ShouldBeSameAs(rejected);
        exception.Message.ShouldBe(rejected.Message);
    }

    public static TheoryData<ExecutionRejectedException> OtherRejections => new()
    {
        new BrokenCircuitException("The circuit is now open."),
        new RateLimiterRejectedException("The operation could not be executed because it was rejected by the rate limiter."),
    };

    [Theory]
    [MemberData(nameof(OtherRejections))]
    public async Task SendAsync_OtherRejection_ThrowsHttpRequestException(ExecutionRejectedException rejected)
    {
        // Arrange
        using var sut = CreateSut((_, _) => throw rejected);
        using var request = CreateRequest();

        // Act
        var exception = await Should.ThrowAsync<HttpRequestException>(() => sut.SendAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.InnerException.ShouldBeSameAs(rejected);
    }

    [Fact]
    public async Task SendAsync_CallerCancels_LetsOperationCanceledExceptionThrough()
    {
        // Arrange
        var cancelled = new OperationCanceledException();
        using var sut = CreateSut((_, _) => throw cancelled);
        using var request = CreateRequest();

        // Act
        var exception = await Record.ExceptionAsync(() => sut.SendAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ShouldBeSameAs(cancelled);
    }
}
