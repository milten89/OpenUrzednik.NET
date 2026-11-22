using System.Net;
using System.Text;

using OpenUrzednik.MF.WhiteList.Common;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests;

// The limit on how much of an error body the filter buffers to find the code (WhiteListErrorMapping.MaxCodeBodyBytes).
public sealed partial class WhiteListRetryTest
{
    private const string LargeBodyStart = """{"code":"WL-195","message":" """;

    // A body of a given size, produced on demand, that counts the bytes read from it. It starts with a JSON answer carrying a code, so
    // that only the limit keeps the filter from finding it.
    private sealed class CountingStream(long size) : Stream
    {
        private static readonly byte[] Start = Encoding.UTF8.GetBytes(LargeBodyStart);
        private long _position;

        public long BytesRead => Interlocked.Read(ref _position);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = (int)Math.Min(count, size - _position);
            for (var index = 0; index < read; index++)
            {
                var at = _position + index;
                buffer[offset + index] = at < Start.Length ? Start[at] : (byte)'x';
            }

            Interlocked.Add(ref _position, read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    // A JSON answer with the code WL-195 and a padding field, of exactly the given length in bytes.
    private static string BodyOfLength(int length)
    {
        const string prefixText = """{"code":"WL-195","pad":" """;
        const string suffixText = """ "}""";
        var prefix = prefixText.TrimEnd();
        var suffix = suffixText.TrimStart();
        return prefix + new string('x', length - prefix.Length - suffix.Length) + suffix;
    }

    [Theory]
    [InlineData(400, false)]
    [InlineData(500, true)]
    public async Task ShouldRetryAsync_BodyOverTheLimit_HasNoCodeAndIsNotReadInFull(int statusCode, bool expected)
    {
        // Arrange: a 10 MB body that starts with WL-195, which would make a 400 retryable and a 500 not
        using var stream = new CountingStream(10_000_000);
        using var response = new HttpResponseMessage((HttpStatusCode)statusCode) { Content = new StreamContent(stream) };

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert: nothing is found past the limit, and the body is not read in full (one copy buffer at most)
        retry.ShouldBe(expected);
        stream.BytesRead.ShouldBeLessThan(1_000_000);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task ShouldRetryAsync_BodyAroundTheLimit_IsReadUpToIt(int overLimit, bool expected)
    {
        // Arrange: a 400 answer of exactly the limit has its code found, one byte more has none
        var body = BodyOfLength(WhiteListErrorMapping.MaxCodeBodyBytes + overLimit);
        Encoding.UTF8.GetByteCount(body).ShouldBe(WhiteListErrorMapping.MaxCodeBodyBytes + overLimit);
        using var response = Response(400, body);

        // Act
        var retry = await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
        retry.ShouldBe(expected);
    }

    [Fact]
    public async Task ShouldRetryAsync_BodyWithinTheLimit_StillReadableByTheErrorMapping()
    {
        // Arrange
        var body = BodyOfLength(WhiteListErrorMapping.MaxCodeBodyBytes);
        using var response = OneShotResponse(400, body);

        // Act
        await WhiteListRetry.ShouldRetryAsync(response, TestContext.Current.CancellationToken);

        // Assert
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        (await response.Content.ReadAsStringAsync()).ShouldBe(body);
#pragma warning restore xUnit1051
    }
}
