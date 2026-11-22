using System.Net;

#if !NET
using OpenUrzednik.Http.Infrastructure;

#endif

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// The body of a response of which the first bytes were already read from the network stream: serves those bytes, then the rest of the
/// stream. It replaces the original content of <see cref="WhiteListErrorMapping.ReadCodeAsync"/> for a body over the limit, so the error
/// mapping reads it from the start. The stream and the original content are disposed with this content.
/// </summary>
internal sealed class WhiteListReplayContent : HttpContent
{
    private readonly byte[] _prefix;
    private readonly int _prefixLength;
    private readonly Stream _rest;
    private readonly HttpContent _original;
    private bool _consumed;

    internal WhiteListReplayContent(byte[] prefix, int prefixLength, Stream rest, HttpContent original)
    {
        _prefix = prefix;
        _prefixLength = prefixLength;
        _rest = rest;
        _original = original;
        CopyHeaders(original, this);
    }

    /// <summary>Copies the content headers (<c>Content-Type</c> with its charset, <c>Content-Length</c> and the others) without validating them again.</summary>
    internal static void CopyHeaders(HttpContent from, HttpContent to)
    {
        foreach (var header in from.Headers)
            to.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        MarkConsumed();
        return Task.FromResult<Stream>(new ReplayStream(_prefix, _prefixLength, _rest));
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        MarkConsumed();
        using var source = new ReplayStream(_prefix, _prefixLength, _rest);
        await source.CopyToAsync(stream).ConfigureAwait(false);
    }

    // Like network content, the body can be taken once: a second take would replay the prefix over a half-read stream.
    private void MarkConsumed()
    {
        if (_consumed)
            throw new InvalidOperationException("The body was already consumed and cannot be read again.");

        _consumed = true;
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rest.Dispose();
            _original.Dispose();
        }

        base.Dispose(disposing);
    }

    // Forward-only and read-only; disposing it leaves the rest of the stream to the content.
    private sealed class ReplayStream(byte[] prefix, int prefixLength, Stream rest) : Stream
    {
        private int _offset;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var taken = TakePrefix(buffer.AsSpan(offset, count));
            if (taken > 0)
                return taken;

            return rest.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadCoreAsync(buffer.AsMemory(offset, count), cancellationToken);

#if NET
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => new(ReadCoreAsync(buffer, cancellationToken));
#endif

        private async Task<int> ReadCoreAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var taken = TakePrefix(buffer.Span);
            if (taken > 0)
                return taken;

            return await rest.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        // Copies from the unread part of the prefix; 0 when it is used up (or the buffer is empty).
        private int TakePrefix(Span<byte> buffer)
        {
            var taken = Math.Min(buffer.Length, prefixLength - _offset);
            prefix.AsSpan(_offset, taken).CopyTo(buffer);
            _offset += taken;
            return taken;
        }
    }
}
