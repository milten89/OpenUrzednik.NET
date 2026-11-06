#if !NET
using System.Text.RegularExpressions;

namespace System.Text.RegularExpressions
{
    /// <summary>netstandard2.0 has no regex source generator, so the attribute only keeps the .NET declarations compiling.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class GeneratedRegexAttribute(string pattern, RegexOptions options) : Attribute
    {
        public string Pattern { get; } = pattern;

        public RegexOptions Options { get; } = options;
    }
}

namespace OpenUrzednik.Nbp.Validation
{
    // The part the regex source generator writes on .NET.
    internal sealed partial class Iso4217Validator
    {
        private static readonly Regex Iso4217CurrencyCode = new(Iso4217CurrencyCodePattern, RegexOptions.CultureInvariant);

        private static partial Regex Iso4217CurrencyCodeRegex() => Iso4217CurrencyCode;
    }
}
#endif
