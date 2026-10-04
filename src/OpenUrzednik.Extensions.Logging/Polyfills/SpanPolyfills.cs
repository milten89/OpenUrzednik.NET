#if !NET
using System.Text;

namespace OpenUrzednik.Extensions.Logging;

/// <summary>
/// The span overloads netstandard2.0 lacks, with the .NET names, so <see cref="LogValues"/> keeps its allocation-free .NET code.
/// </summary>
internal static class SpanPolyfills
{
    internal static StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> value)
    {
        builder.EnsureCapacity(builder.Length + value.Length);
        foreach (var c in value)
            builder.Append(c);
        return builder;
    }

    extension(string)
    {
        internal static string Concat(ReadOnlySpan<char> first, ReadOnlySpan<char> second, ReadOnlySpan<char> third)
        {
            var chars = new char[first.Length + second.Length + third.Length];
            first.CopyTo(chars);
            second.CopyTo(chars.AsSpan(first.Length));
            third.CopyTo(chars.AsSpan(first.Length + second.Length));
            return new string(chars);
        }
    }
}
#endif
