using Microsoft.Extensions.Time.Testing;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public static class TestClock
{
    /// <summary>
    /// A clock far after every date the fakers generate (they stop at 2030), so random dates are never "in the future".
    /// Tests about the "today" boundary create their own <see cref="FakeTimeProvider"/>.
    /// </summary>
    public static FakeTimeProvider FarFuture()
        => new(new DateTimeOffset(2100, 1, 1, 12, 0, 0, TimeSpan.Zero));
}
