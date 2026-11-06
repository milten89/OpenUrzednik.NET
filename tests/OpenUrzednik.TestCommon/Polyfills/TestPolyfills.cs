#if !NET
using System.Net.Mime;

namespace System.Net;

/// <summary>Members .NET has and .NET Framework doesn't; the tests use the .NET names on every target.</summary>
public static class TestPolyfills
{
    extension(HttpStatusCode)
    {
        public static HttpStatusCode TooManyRequests => (HttpStatusCode)429;

        public static HttpStatusCode UnprocessableEntity => (HttpStatusCode)422;
    }

    extension(MediaTypeNames.Application)
    {
        public static string Json => "application/json";
    }

    public static Task CancelAsync(this CancellationTokenSource source)
    {
        source.Cancel();
        return Task.CompletedTask;
    }
}
#endif
