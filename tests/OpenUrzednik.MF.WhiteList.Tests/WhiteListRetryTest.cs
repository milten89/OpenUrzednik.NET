using System.Net;
using System.Text;

namespace OpenUrzednik.MF.WhiteList.Tests;

// Shared helpers; the tests are in one file per method.
public sealed partial class WhiteListRetryTest
{
    private static HttpResponseMessage Response(int statusCode, string? body)
    {
        var response = new HttpResponseMessage((HttpStatusCode)statusCode);
        if (body is not null)
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return response;
    }

    private static string Body(string code) => $$"""{"message":"Komunikat","code":"{{code}}"}""";

    // A body that fails when it is read, as a connection dropped half way does.
    private sealed class FailingContent(Exception exception) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.FromException(exception);

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    // A body that can be read only once, as a network stream: it survives a second read only if the filter buffered it.
    private sealed class OneShotStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static HttpResponseMessage OneShotResponse(int statusCode, string body)
        => new((HttpStatusCode)statusCode) { Content = new StreamContent(new OneShotStream(Encoding.UTF8.GetBytes(body))) };

    // A body that arrives asynchronously, away from the caller's SynchronizationContext, so every await of the filter suspends.
    private sealed class AsyncContent(string body) : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await Task.Delay(1).ConfigureAwait(false);
            var bytes = Encoding.UTF8.GetBytes(body);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
