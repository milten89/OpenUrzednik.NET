#if !NET
using System.Runtime.InteropServices;

// Same namespace as the executor, so the call sites need no #if.
namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// netstandard2.0 has no cancellation token on these reads. The token stops the wait, so the request deadline still
/// applies, but the read itself goes on in the background until the response is disposed.
/// </summary>
internal static class HttpPolyfills
{
    internal static Task<Stream> ReadAsStreamAsync(this HttpContent content, CancellationToken cancellationToken)
        => content.ReadAsStreamAsync().WaitAsync(cancellationToken);

    internal static Task<int> ReadAsync(this StreamReader reader, Memory<char> buffer, CancellationToken cancellationToken)
    {
        if (!MemoryMarshal.TryGetArray<char>(buffer, out var segment))
            throw new ArgumentException("The buffer must be backed by an array.", nameof(buffer));

        return reader.ReadAsync(segment.Array!, segment.Offset, segment.Count).WaitAsync(cancellationToken);
    }

    internal static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            return await task.ConfigureAwait(false);

        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancelled))
        {
            if (await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false) != task)
            {
                // Observe a later failure of the abandoned read, so it isn't reported as unobserved.
                _ = task.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                throw new OperationCanceledException(cancellationToken);
            }
        }

        return await task.ConfigureAwait(false);
    }
}
#endif
