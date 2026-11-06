// netstandard2.0 only (src/Directory.Build.props): members .NET has, with the .NET names, so call sites need no #if.

namespace System.Collections.ObjectModel
{
    internal static class ReadOnlyDictionaryPolyfill
    {
        extension<TKey, TValue>(ReadOnlyDictionary<TKey, TValue>)
            where TKey : notnull
        {
            public static ReadOnlyDictionary<TKey, TValue> Empty => EmptyDictionary<TKey, TValue>.Instance;
        }

        private static class EmptyDictionary<TKey, TValue>
            where TKey : notnull
        {
            internal static readonly ReadOnlyDictionary<TKey, TValue> Instance = new(new Dictionary<TKey, TValue>());
        }
    }
}

namespace System.Diagnostics
{
    internal static class StopwatchPolyfill
    {
        extension(Stopwatch)
        {
            public static TimeSpan GetElapsedTime(long startingTimestamp)
                => TimeSpan.FromTicks((long)((Stopwatch.GetTimestamp() - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
        }
    }
}
