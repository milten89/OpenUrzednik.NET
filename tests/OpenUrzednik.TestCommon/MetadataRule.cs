using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.TestCommon;

/// <summary>
/// Checks ADR-0021's rule for metadata values: strings, booleans, <see cref="int"/>, <see cref="long"/>, <see cref="double"/>,
/// <see langword="null"/>, or arrays of one of these. Dates, times and durations are ISO 8601 strings, so they pass as strings.
/// </summary>
public static class MetadataRule
{
    /// <summary>The 1.0 key kept with a <see cref="TimeSpan"/> value until 2.0 (ADR-0021).</summary>
    private static readonly string[] OneZeroExceptions = [RateLimitExceededError.RetryAfterMetadataKey];

    public static void ShouldFollowTheMetadataRule(this OpenUrzednikError error)
    {
        foreach (var entry in error.Metadata.Where(entry => Array.IndexOf(OneZeroExceptions, entry.Key) < 0))
        {
            IsAllowed(entry.Value).ShouldBeTrue($"{error.GetType().Name}.Metadata[\"{entry.Key}\"] is a {entry.Value?.GetType().Name}");
        }
    }

    private static bool IsAllowed(object? value)
        => value is null or string or bool or int or long or double or string[] or bool[] or int[] or long[] or double[];
}
