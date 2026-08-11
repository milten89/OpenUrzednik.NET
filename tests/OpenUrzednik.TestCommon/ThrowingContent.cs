using System.Net;

namespace OpenUrzednik.TestCommon;

public sealed class ThrowingContent : HttpContent
{
    private readonly Func<Stream, TransportContext?, Task> _serializeToStreamDelegate;

    public ThrowingContent(Func<Stream, TransportContext?, Task> serializeToStreamDelegate)
    {
        ArgumentNullException.ThrowIfNull(serializeToStreamDelegate);

        _serializeToStreamDelegate = serializeToStreamDelegate;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        await _serializeToStreamDelegate(stream, context);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = -1;
        return false;
    }
}
