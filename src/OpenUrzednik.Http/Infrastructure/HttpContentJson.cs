using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace OpenUrzednik.Http.Infrastructure;

internal static class HttpContentJson
{
    /// <summary>
    /// <c>ReadFromJsonAsync</c>, bounded by the token on every target. .NET's response stream honours the token. .NET Framework's checks it
    /// only before a read starts, so there the wait is bounded with <c>WaitAsync</c>; the abandoned read ends when the response is disposed.
    /// </summary>
    /// <remarks>A polyfill can't hide this difference: the method has the same signature as the library's own.</remarks>
    internal static Task<T?> ReadJsonAsync<T>(this HttpContent content, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
#if NET
        => content.ReadFromJsonAsync(typeInfo, cancellationToken);
#else
        => content.ReadFromJsonAsync(typeInfo, cancellationToken).WaitAsync(cancellationToken);
#endif
}
