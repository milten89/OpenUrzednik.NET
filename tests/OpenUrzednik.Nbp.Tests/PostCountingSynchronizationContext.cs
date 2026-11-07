namespace OpenUrzednik.Nbp.Tests;

/// <summary>
/// A <see cref="SynchronizationContext"/> that counts the continuations posted to it and runs them on the thread pool.
/// Library code awaits with <c>ConfigureAwait(false)</c>, so it never posts to the caller's context: a caller that blocks on
/// the task in a UI or ASP.NET (.NET Framework) context would deadlock otherwise.
/// </summary>
internal sealed class PostCountingSynchronizationContext : SynchronizationContext
{
    private int _posts;

    private int Posts => Volatile.Read(ref _posts);

    public override void Post(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        ThreadPool.QueueUserWorkItem(_ => d(state));
    }

    public override SynchronizationContext CreateCopy() => this;

    /// <summary>
    /// Starts <paramref name="call"/> with a counting context as the current one, awaits it outside that context, and returns
    /// how many continuations were posted to the context.
    /// </summary>
    public static async Task<int> CountPostsAsync(Func<Task> call)
    {
        var context = new PostCountingSynchronizationContext();
        var previous = Current;
        Task task;
        SetSynchronizationContext(context);
        try
        {
            task = call();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }

        await task;
        return context.Posts;
    }
}
