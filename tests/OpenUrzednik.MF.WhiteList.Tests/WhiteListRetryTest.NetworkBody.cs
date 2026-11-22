using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

// The filter on content that behaves like the network: chunked (no Content-Length) and readable once, so a failed attempt to buffer it
// leaves it consumed. StreamContent over a custom stream does not behave so: it serves the body again.
public sealed partial class WhiteListRetryTest
{
    private static readonly DateTime SearchDate = new(2026, 11, 2);

    // Content of a network response: the stream can be taken once (to read it or to copy it, which also disposes it); a second attempt,
    // for example after a buffering that went over its limit, fails as .NET's does.
    private sealed class NetworkContent(Stream stream) : HttpContent
    {
        private bool _consumed;
        public bool Disposed { get; private set; }

        private Stream Take()
        {
            if (_consumed)
                throw new InvalidOperationException("The stream was already consumed. It cannot be read again.");

            _consumed = true;
            return stream;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult(Take());

        protected override async Task SerializeToStreamAsync(Stream target, TransportContext? context)
        {
            using var source = Take();
            await source.CopyToAsync(target).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private static HttpResponseMessage NetworkResponse(int statusCode, string body, out NetworkContent content)
    {
        content = new NetworkContent(new OneShotStream(Encoding.UTF8.GetBytes(body)));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        content.Headers.TryAddWithoutValidation("X-Test", "kept");
        return new HttpResponseMessage((HttpStatusCode)statusCode) { Content = content };
    }

    private static string LargeHtml() => "<html>" + new string('x', 10_000) + "</html>";

    // The handler of an app that applies the filter, as an AddStandardResilienceHandler on its last attempt would, and returns the response.
    private sealed class FilterHandler(Func<Task<HttpResponseMessage>> respond) : DelegatingHandler(new DelegatingStubHttpMessageHandler((_, _) => respond()))
    {
        public bool? Retry { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Retry = await WhiteListRetry.ShouldRetryAsync(response, cancellationToken).ConfigureAwait(false);
            return response;
        }
    }

    private static async Task<(OpenUrzednikError Error, bool Retry)> SearchAsync(int statusCode, string body, bool consumedBefore = false)
    {
        using var handler = new FilterHandler(async () =>
        {
            var response = NetworkResponse(statusCode, body, out var content);
            // An app handler in front of the filter read the body, which network content allows once.
            if (consumedBefore)
                await content.CopyToAsync(Stream.Null);
            return response;
        });
        using var httpClient = new HttpClient(handler);
        var client = new WhiteListClient(httpClient, null, new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero)));

        var result = await client.SearchByNipAsync("7368125416", SearchDate, TestContext.Current.CancellationToken);

        return (result.Errors.ShouldHaveSingleItem(), handler.Retry.ShouldNotBeNull());
    }

    [Fact]
    public async Task ShouldRetryAsync_ChunkedBadRequestOverTheLimit_ClientReturnsBadRequestError()
    {
        // Act
        var (error, retry) = await SearchAsync(400, LargeHtml());

        // Assert: the error mapping read the body from the start, instead of failing on a consumed stream
        retry.ShouldBeFalse();
        error.ShouldBeOfType<WhiteListBadRequestError>();
    }

    [Fact]
    public async Task ShouldRetryAsync_ChunkedBadRequestWithTheRepeatCodeWithinTheLimit_ClientReturnsItsCode()
    {
        // Act
        var (error, retry) = await SearchAsync(400, Body("WL-195"));

        // Assert
        retry.ShouldBeTrue();
        error.ShouldBeOfType<WhiteListServiceUnavailableError>().WhiteListCode.ShouldBe("WL-195");
    }

    [Fact]
    public async Task ShouldRetryAsync_ChunkedServerErrorOverTheLimit_IsRetriedAndClientReturnsServiceUnavailableError()
    {
        // Act
        var (error, retry) = await SearchAsync(500, LargeHtml());

        // Assert
        retry.ShouldBeTrue();
        error.ShouldBeOfType<WhiteListServiceUnavailableError>();
    }

    [Fact]
    public async Task ShouldRetryAsync_ChunkedServerErrorWithTheUnknownStatusCode_ClientReturnsUnknownError()
    {
        // Act
        var (error, retry) = await SearchAsync(500, Body("WL-197"));

        // Assert
        retry.ShouldBeFalse();
        error.ShouldBeOfType<WhiteListUnknownError>().WhiteListCode.ShouldBe("WL-197");
    }

    [Theory]
    [InlineData(400, false)]
    [InlineData(500, true)]
    public async Task ShouldRetryAsync_BodyConsumedBefore_AnswersFromTheStatusAndDoesNotThrow(int statusCode, bool expected)
    {
        // Arrange
        using var response = NetworkResponse(statusCode, Body("WL-195"), out var original);
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        await original.CopyToAsync(Stream.Null);
#pragma warning restore xUnit1051

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert: no code could be read; the response is left as it is
        retry.ShouldBe(expected);
        response.Content.ShouldBeSameAs(original);
    }

    [Theory]
    [InlineData(400, typeof(WhiteListBadRequestError))]
    [InlineData(500, typeof(WhiteListServiceUnavailableError))]
    public async Task ShouldRetryAsync_ClientAfterBodyConsumedBefore_ReturnsAnErrorInsteadOfThrowing(int statusCode, Type errorType)
    {
        // Act
        var (error, _) = await SearchAsync(statusCode, Body("WL-195"), consumedBefore: true);

        // Assert
        error.ShouldBeOfType(errorType);
    }

    [Fact]
    public async Task ShouldRetryAsync_BodyWithinTheLimit_ReplacesTheContentKeepingTheHeaders()
    {
        // Arrange
        var body = Body("WL-196");
        using var response = NetworkResponse(400, body, out var original);

        // Act
        await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        response.Content.ShouldNotBeSameAs(original);
        original.Disposed.ShouldBeTrue();
        response.Content.Headers.ContentType.ShouldNotBeNull().ToString().ShouldBe("application/json; charset=utf-8");
        response.Content.Headers.GetValues("X-Test").ShouldBe(["kept"]);
        (await ReadAllAsync(response.Content)).ShouldBe(body);
    }

    [Fact]
    public async Task ShouldRetryAsync_BodyOverTheLimit_ReplacesTheContentKeepingTheHeadersAndTheWholeBody()
    {
        // Arrange
        var body = LargeHtml();
        using var response = NetworkResponse(400, body, out var original);

        // Act
        await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert: the original is disposed with the replacement, not before
        response.Content.ShouldNotBeSameAs(original);
        original.Disposed.ShouldBeFalse();
        response.Content.Headers.ContentType.ShouldNotBeNull().ToString().ShouldBe("application/json; charset=utf-8");
        response.Content.Headers.GetValues("X-Test").ShouldBe(["kept"]);
        (await ReadAllAsync(response.Content)).ShouldBe(body);
        response.Dispose();
        original.Disposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task ShouldRetryAsync_BodyArrivingLater_NeverResumesOnTheCallersSynchronizationContext(int statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new NetworkContent(new DelayedStream(Encoding.UTF8.GetBytes(Body("WL-195")))) };
        Task<bool>? answer = null;

        // Act
        var posts = await CountingSynchronizationContext.CountPostsAsync(() =>
            answer = WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken).AsTask());

        // Assert
        posts.ShouldBe(0);
        (await answer!).ShouldBeTrue();
    }

    [Theory]
    [InlineData(400, typeof(IOException))]
    [InlineData(500, typeof(IOException))]
    [InlineData(400, typeof(HttpRequestException))]
    [InlineData(500, typeof(HttpRequestException))]
    public async Task ShouldRetryAsync_BodyThatFailsWhileRead_IsLeftForTheErrorMapping(int statusCode, Type exceptionType)
    {
        // Arrange
        using var original = new NetworkContent(new FailingStream((Exception)Activator.CreateInstance(exceptionType, "Connection reset.")!));
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = original };

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBe(statusCode == 500);
        response.Content.ShouldBeSameAs(original);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task ShouldRetryAsync_ContentThatNeverGivesItsStream_StopsWhenTheTokenIsCancelled(int statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new PendingContent() };
        using var cts = new CancellationTokenSource();
        var retry = WhiteListRetry.ShouldRetryAsync(response, cts.Token).AsTask();

        // Act
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => retry);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task ShouldRetryAsync_StreamThatNeverReturns_StopsWhenTheTokenIsCancelled(int statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new NetworkContent(new PendingStream()) };
        using var cts = new CancellationTokenSource();
        var retry = WhiteListRetry.ShouldRetryAsync(response, cts.Token).AsTask();

        // Act
        cts.Cancel();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => retry);
    }

    [Theory]
    [InlineData(400, false)]
    [InlineData(500, true)]
    public async Task ShouldRetryAsync_ContentThatCannotGiveItsStream_AnswersFromTheStatus(int statusCode, bool expected)
    {
        // Arrange: .NET Framework throws NotSupportedException for a body that was consumed before
        using var original = new UnsupportedContent();
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = original };

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBe(expected);
        response.Content.ShouldBeSameAs(original);
    }

    // Content that refuses to hand out its stream.
    private sealed class UnsupportedContent : HttpContent
    {
        protected override Task<Stream> CreateContentReadStreamAsync() => throw new NotSupportedException("The stream was consumed.");

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new NotSupportedException("The stream was consumed.");

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    private static async Task<string> ReadAllAsync(HttpContent content)
    {
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        return await content.ReadAsStringAsync();
#pragma warning restore xUnit1051
    }

    // A stream that fails on the first read, as a connection that broke.
    private sealed class FailingStream(Exception exception) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw exception;
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw exception;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // A stream whose reads wait until the token is cancelled.
    private sealed class PendingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var never = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static state => ((TaskCompletionSource<int>)state!).TrySetCanceled(), never))
                return await never.Task.ConfigureAwait(false);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // Content whose stream never arrives, and that does not look at the token.
    private sealed class PendingContent : HttpContent
    {
        protected override Task<Stream> CreateContentReadStreamAsync() => new TaskCompletionSource<Stream>().Task;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => new TaskCompletionSource<bool>().Task;

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

#if !NET
    // .NET Framework's network streams ignore the token once a read has started; .NET's honour it, so only the polyfill needs the test.
    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    public async Task ShouldRetryAsync_StreamThatIgnoresTheToken_StopsWhenTheTokenIsCancelled(int statusCode)
    {
        // Arrange
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new NetworkContent(new TokenIgnoringStream(release.Task)) };
        using var cts = new CancellationTokenSource();
        var retry = WhiteListRetry.ShouldRetryAsync(response, cts.Token).AsTask();

        // Act
        cts.Cancel();
        var finished = await Task.WhenAny(retry, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        // Assert: it stops soon after the cancellation, long before the read completes
        finished.ShouldBeSameAs(retry);
        await Should.ThrowAsync<OperationCanceledException>(() => retry);
        release.SetResult(0);
    }

    // A stream whose reads complete only when released, whatever the token says.
    private sealed class TokenIgnoringStream(Task<int> released) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => released;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
#endif
}
