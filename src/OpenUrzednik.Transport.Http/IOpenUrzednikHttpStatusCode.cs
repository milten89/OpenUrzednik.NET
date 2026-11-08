namespace OpenUrzednik.Transport.Http;

/// <summary>
/// The status code of the HTTP response that caused an error: implemented by the HTTP subclasses of the condition errors, such as
/// <see cref="HttpNotFoundError"/> (ADR-0021). Check it with <c>is</c>, or read the status of any error with
/// <see cref="OpenUrzednik.Core.Errors.HttpErrorMetadata.GetHttpStatusCode"/>.
/// </summary>
/// <remarks>
/// It is frozen: a further HTTP detail (e.g. response headers) comes as a separate interface, never as a new member (ADR-0011).
/// </remarks>
public interface IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Gets the status code of the HTTP response that caused the error.
    /// </summary>
    int StatusCode { get; }
}
