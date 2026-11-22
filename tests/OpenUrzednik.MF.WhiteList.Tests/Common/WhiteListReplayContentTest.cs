using System.Net.Http.Headers;
using System.Text;

using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed class WhiteListReplayContentTest
{
    private const string Prefix = "0123456789";
    private const string Rest = "abcdefghijklmnopqrstuvwxyz";

    // Tracks its disposal, and holds the rest of the body.
    private sealed class TrackingStream(byte[] bytes) : MemoryStream(bytes)
    {
        public bool Closed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Closed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class OriginalContent : HttpContent
    {
        public bool Disposed { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) => Task.CompletedTask;

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

    // .NET Framework's HttpContent and StreamReader have no overloads with a token.
    private static async Task<Stream> OpenAsync(HttpContent content)
    {
#pragma warning disable xUnit1051
        return await content.ReadAsStreamAsync();
#pragma warning restore xUnit1051
    }

    private static async Task<string> ReadTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
#pragma warning disable xUnit1051
        return await reader.ReadToEndAsync();
#pragma warning restore xUnit1051
    }

    private static (WhiteListReplayContent Content, TrackingStream RestStream, OriginalContent Original) Create(string prefix = Prefix, string rest = Rest, int prefixLength = -1)
    {
        var original = new OriginalContent();
        original.Headers.ContentType = new MediaTypeHeaderValue("text/html") { CharSet = "utf-8" };
        original.Headers.TryAddWithoutValidation("X-Test", "kept");
        var restStream = new TrackingStream(Encoding.UTF8.GetBytes(rest));
        // The prefix array may hold more bytes than the ones read, as the filter's buffer does.
        var prefixBytes = Encoding.UTF8.GetBytes(prefix + "###");
        return (new WhiteListReplayContent(prefixBytes, prefixLength < 0 ? prefix.Length : prefixLength, restStream, original), restStream, original);
    }

    [Fact]
    public void Ctor_Original_CopiesTheContentHeaders()
    {
        // Act
        var (content, _, _) = Create();

        // Assert
        content.Headers.ContentType.ShouldNotBeNull().ToString().ShouldBe("text/html; charset=utf-8");
        content.Headers.GetValues("X-Test").ShouldBe(["kept"]);
    }

    [Fact]
    public async Task ReadAsStreamAsync_Content_ServesThePrefixThenTheRest()
    {
        // Arrange
        var (content, _, _) = Create();

        // Act
        using var stream = await OpenAsync(content);
        var text = await ReadTextAsync(stream);

        // Assert: only the bytes counted as the prefix, not the rest of its array
        text.ShouldBe(Prefix + Rest);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(64)]
    public async Task ReadAsync_BufferOfAnySize_ReadsEverythingInOrder(int size)
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);
        var result = new List<byte>();
        var buffer = new byte[size];

        // Act
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, size, TestContext.Current.CancellationToken)) > 0)
            result.AddRange(buffer.Take(read));

        // Assert
        Encoding.UTF8.GetString([.. result]).ShouldBe(Prefix + Rest);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(64)]
    public async Task Read_BufferOfAnySize_ReadsEverythingInOrder(int size)
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);
        var result = new List<byte>();
        var buffer = new byte[size];

        // Act
        int read;
        while ((read = stream.Read(buffer, 0, size)) > 0)
            result.AddRange(buffer.Take(read));

        // Assert
        Encoding.UTF8.GetString([.. result]).ShouldBe(Prefix + Rest);
    }

    [Fact]
    public async Task ReadAsync_BufferLargerThanThePrefix_ReturnsThePrefixFirst()
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);
        var buffer = new byte[64];

        // Act
        var first = await stream.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken);
        var second = await stream.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken);

        // Assert
        first.ShouldBe(Prefix.Length);
        second.ShouldBe(Rest.Length);
    }

    [Fact]
    public async Task ReadAsync_EmptyBuffer_ReadsNothingAndKeepsThePrefix()
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);

        // Act
        var empty = await stream.ReadAsync(new byte[4], 2, 0, TestContext.Current.CancellationToken);
        var buffer = new byte[4];
        var read = await stream.ReadAsync(buffer, 0, 4, TestContext.Current.CancellationToken);

        // Assert
        empty.ShouldBe(0);
        Encoding.UTF8.GetString(buffer, 0, read).ShouldBe("0123");
    }

    [Fact]
    public async Task ReadAsync_BufferWithOffset_WritesAtTheOffset()
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);
        var buffer = new byte[8];

        // Act
        var read = await stream.ReadAsync(buffer, 3, 4, TestContext.Current.CancellationToken);

        // Assert
        read.ShouldBe(4);
        buffer.ShouldBe([0, 0, 0, (byte)'0', (byte)'1', (byte)'2', (byte)'3', 0]);
    }

    [Fact]
    public async Task ReadAsync_NoPrefix_ServesTheRest()
    {
        // Arrange
        var (content, _, _) = Create(prefixLength: 0);
        using var stream = await OpenAsync(content);

        // Act
        var text = await ReadTextAsync(stream);

        // Assert
        text.ShouldBe(Rest);
    }

    [Fact]
    public async Task CopyToAsync_Content_WritesThePrefixThenTheRest()
    {
        // Arrange
        var (content, _, _) = Create();
        using var target = new MemoryStream();

        // Act
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        await content.CopyToAsync(target);
#pragma warning restore xUnit1051

        // Assert
        Encoding.UTF8.GetString(target.ToArray()).ShouldBe(Prefix + Rest);
    }

    [Fact]
    public async Task Stream_Members_AreReadOnlyAndForwardOnly()
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);

        // Assert
        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeFalse();
        stream.CanWrite.ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => _ = stream.Length);
        Should.Throw<NotSupportedException>(() => _ = stream.Position);
        Should.Throw<NotSupportedException>(() => stream.Position = 0);
        Should.Throw<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Should.Throw<NotSupportedException>(() => stream.SetLength(0));
        Should.Throw<NotSupportedException>(() => stream.Write([1], 0, 1));
        Should.NotThrow(stream.Flush);
    }

    [Fact]
    public void Headers_ContentLength_IsNotComputed()
    {
        // Arrange
        var (content, _, _) = Create();

        // Assert: the size of the rest is not known
        content.Headers.ContentLength.ShouldBeNull();
    }

    [Fact]
    public void Dispose_Content_DisposesTheRestAndTheOriginal()
    {
        // Arrange
        var (content, rest, original) = Create();

        // Act
        content.Dispose();

        // Assert
        rest.Closed.ShouldBeTrue();
        original.Disposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Dispose_Content_ThenReadAsStreamAsync_ThrowsObjectDisposedException()
    {
        // Arrange
        var (content, _, _) = Create();
        content.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => OpenAsync(content));
    }

    [Fact]
    public async Task Dispose_Content_ThenReadAsStringAsync_ThrowsObjectDisposedException()
    {
        // Arrange
        var (content, _, _) = Create();
        content.Dispose();

        // Act & Assert
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        await Should.ThrowAsync<ObjectDisposedException>(() => content.ReadAsStringAsync());
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task CopyToAsync_ContentAlreadyRead_ThrowsInvalidOperationException()
    {
        // Arrange
        var (content, _, _) = Create();
        using var first = new MemoryStream();
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        await content.CopyToAsync(first);

        // Act & Assert: a second take would replay the prefix over the stream that is already read
        await Should.ThrowAsync<InvalidOperationException>(() => content.CopyToAsync(Stream.Null));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task ReadAsStreamAsync_ContentAlreadyCopied_ThrowsInvalidOperationException()
    {
        // Arrange
        var (content, _, _) = Create();
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        await content.CopyToAsync(Stream.Null);
#pragma warning restore xUnit1051

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => OpenAsync(content));
    }

    [Fact]
    public async Task CopyToAsync_ContentAlreadyOpenedAsStream_ThrowsInvalidOperationException()
    {
        // Arrange
        var (content, _, _) = Create();
        using var stream = await OpenAsync(content);

        // Act & Assert
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        await Should.ThrowAsync<InvalidOperationException>(() => content.CopyToAsync(Stream.Null));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task Dispose_ReadStream_LeavesTheRestToTheContent()
    {
        // Arrange
        var (content, rest, original) = Create();
        var stream = await OpenAsync(content);

        // Act
        stream.Dispose();

        // Assert
        rest.Closed.ShouldBeFalse();
        original.Disposed.ShouldBeFalse();
    }

    [Fact]
    public async Task ReadAsync_RestArrivingLater_NeverResumesOnTheCallersSynchronizationContext()
    {
        // Arrange
        var original = new OriginalContent();
        var content = new WhiteListReplayContent(Encoding.UTF8.GetBytes(Prefix), Prefix.Length, new DelayedStream(Encoding.UTF8.GetBytes(Rest)), original);
        using var stream = await OpenAsync(content);
        var buffer = new byte[64];
        (await stream.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken)).ShouldBe(Prefix.Length);

        // Act: the prefix is used up, so this read waits for the rest
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => stream.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken));

        // Assert
        posts.ShouldBe(0);
    }

    [Fact]
    public async Task CopyToAsync_RestArrivingLater_NeverResumesOnTheCallersSynchronizationContext()
    {
        // Arrange
        var original = new OriginalContent();
        var content = new WhiteListReplayContent(Encoding.UTF8.GetBytes(Prefix), Prefix.Length, new DelayedStream(Encoding.UTF8.GetBytes(Rest)), original);
        using var target = new MemoryStream();

        // Act
#pragma warning disable xUnit1051 // .NET Framework's HttpContent has no overload with a token
        var posts = await CountingSynchronizationContext.CountPostsAsync(() => content.CopyToAsync(target));
#pragma warning restore xUnit1051

        // Assert
        posts.ShouldBe(0);
        Encoding.UTF8.GetString(target.ToArray()).ShouldBe(Prefix + Rest);
    }
}
