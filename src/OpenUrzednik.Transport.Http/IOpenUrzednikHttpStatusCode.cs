using System.Net;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// The status code of the HTTP response that caused an error: implemented by the HTTP subclasses of the condition errors, such as
/// <see cref="HttpNotFoundError"/> (ADR-0022). Check it with <c>is</c>, or read the status of any error with
/// <see cref="HttpStatusExtensions.GetHttpStatus"/>.
/// </summary>
/// <remarks>
/// It is frozen: a further HTTP detail (e.g. response headers) comes as a separate interface, never as a new member (ADR-0011).
/// </remarks>
public interface IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Gets the status code of the HTTP response that caused the error. The value can be one <see cref="HttpStatusCode"/> doesn't name:
    /// 429 Too Many Requests (RFC 6585) has no member on netstandard2.0 / .NET Framework, so compare with <c>(HttpStatusCode)429</c>.
    /// </summary>
    HttpStatusCode StatusCode { get; }
}
