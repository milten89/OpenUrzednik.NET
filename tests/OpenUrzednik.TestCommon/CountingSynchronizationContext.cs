namespace OpenUrzednik.TestCommon;

/// <summary>
/// A <see cref="SynchronizationContext"/> that counts the continuations posted or sent to it and runs them on the thread pool.
/// Library code awaits with <c>ConfigureAwait(false)</c>, so it never resumes on the caller's context: a caller that blocks on the task
/// in a UI or ASP.NET (.NET Framework) context would deadlock otherwise. The tests check that the count stays 0.
/// </summary>
public sealed class CountingSynchronizationContext : SynchronizationContext
{
    private int _posts;

    /// <summary>Gets the number of continuations posted or sent to this context.</summary>
    public int Posts => Volatile.Read(ref _posts);

    /// <inheritdoc />
    public override void Post(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        ThreadPool.QueueUserWorkItem(_ => d(state));
    }

    /// <inheritdoc />
    public override void Send(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        d(state);
    }

    /// <inheritdoc />
    public override SynchronizationContext CreateCopy() => this;

    /// <summary>
    /// Starts <paramref name="call"/> with a counting context as the current one, awaits it outside that context, and returns how many
    /// continuations were posted to the context.
    /// </summary>
    public static async Task<int> CountPostsAsync(Func<Task> call)
    {
        var context = new CountingSynchronizationContext();
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
