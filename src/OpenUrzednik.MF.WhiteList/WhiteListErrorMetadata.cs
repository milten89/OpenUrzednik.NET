namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The <see cref="OpenUrzednik.Core.Errors.OpenUrzednikError.Metadata"/> key several MF White List error types share (ADR-0021). A key only
/// one type adds is a constant on that type (<see cref="WhiteListNotFoundError.RequestIdMetadataKey"/>,
/// <see cref="WhiteListNotFoundError.RequestDateTimeMetadataKey"/>). None reuses a key of Core or the transport (<c>statusCode</c>,
/// <c>retryAfter</c>, …), whose entries the errors keep.
/// </summary>
public static class WhiteListErrorMetadata
{
    /// <summary>
    /// The key of <see cref="IWhiteListErrorCode.WhiteListCode"/>, a <see cref="string"/> (<see langword="null"/> when the answer had no code).
    /// Every type that implements <see cref="IWhiteListErrorCode"/> adds it.
    /// </summary>
    public const string WhiteListCodeMetadataKey = "whiteListCode";

}
