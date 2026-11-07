using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Core.Tests;

// Shared helpers; tests are in one file per method.
public partial class OpenUrzednikResultExtensionsTest
{
    // Starts an operation on a thread whose SynchronizationContext counts posts, as a UI or classic ASP.NET context would, then
    // completes what the operation waits for and returns how many continuations were posted back to that context. Library code
    // awaits with ConfigureAwait(false), so there should be none: a caller blocking on the task on such a context would deadlock.
    private static async Task<int> CountCallerContextPostsAsync(Func<Task> start, Action complete)
    {
        var context = new CountingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        Task task;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            task = start();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        complete();
        await task;
        return context.Posts;
    }
}
