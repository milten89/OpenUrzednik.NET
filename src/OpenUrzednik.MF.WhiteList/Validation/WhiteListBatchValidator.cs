using System.Globalization;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Identifiers;

namespace OpenUrzednik.MF.WhiteList.Validation;

/// <summary>
/// Validates the identifiers of a batch search (ADR-0023): 1 to <see cref="MaxCount"/> of them, else a <see cref="ValidationError{T}"/> of
/// <see cref="int"/> (the count) named after the parameter; and each one with its shared validator, named after its position
/// (<c>nips[3]</c>, counted from 0), its value masked. Every error is returned together. Duplicates are valid: the API answers each one.
/// After a successful <see cref="Validate"/>, <see cref="Normalized"/> holds the digits to send, in order.
/// </summary>
/// <remarks>The client throws <see cref="ArgumentNullException"/> for a <see langword="null"/> list or element first (<see cref="ThrowIfAnyNull"/>).</remarks>
internal sealed class WhiteListBatchValidator(string propertyName, IReadOnlyList<string> value, IReadOnlyList<string> elementNames,
    Func<string, string, IdentifierValidator> createValidator) : ValueValidator<IReadOnlyList<string>>(propertyName, value)
{
    /// <summary>The most identifiers the API takes in one batch; it rejects more with <c>WL-130</c>.</summary>
    internal const int MaxCount = 30;

    private readonly string[] _normalized = new string[value.Count];

    public override string Name => "whiteListBatchCount";

    /// <summary>Gets the identifiers without separators and prefix, in order: what to send once <see cref="Validate"/> succeeds.</summary>
    internal IReadOnlyList<string> Normalized => _normalized;

    /// <summary>The <see cref="Normalized"/> identifiers joined with commas, as the request path takes them.</summary>
    internal string Joined() => string.Join(",", _normalized);

    /// <summary>
    /// The names of the first <see cref="MaxCount"/> elements of the parameter <paramref name="parameterName"/> (<c>nips[0]</c>, …), built
    /// once per parameter so that a call allocates none.
    /// </summary>
    internal static string[] ElementNames(string parameterName)
    {
        var names = new string[MaxCount];
        for (var i = 0; i < names.Length; i++)
            names[i] = ElementName(parameterName, i);
        return names;
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="identifiers"/> or one of its elements is <see langword="null"/>
    /// (ADR-0002); the message names the element's position, never a value.
    /// </summary>
    internal static void ThrowIfAnyNull(IReadOnlyList<string> identifiers, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(identifiers, parameterName);

        // The count in a local, so that mutation testing can compile the mutants of the comparison (ADR-0020).
        var count = identifiers.Count;
        for (var i = 0; i < count; i++)
        {
            if (identifiers[i] is null)
                throw new ArgumentNullException(parameterName, $"'{ElementName(parameterName, i)}' is null.");
        }
    }

    public override OpenUrzednikResult Validate()
    {
        var count = Value.Count;
        var result = count is >= 1 and <= MaxCount
            ? OpenUrzednikResult.Success()
            : OpenUrzednikResult.Failure(new ValidationError<int>(
                $"'{PropertyName}' should hold from 1 to {MaxCount} identifiers, not {count.ToString(CultureInfo.InvariantCulture)}.", Name, PropertyName, count));

        var named = elementNames.Count;
        for (var i = 0; i < count; i++)
        {
            // The cached name for the first MaxCount positions (no allocation), a new one past them.
            var name = i < named ? elementNames[i] : ElementName(PropertyName, i);
            var validator = createValidator(name, Value[i]);
            result = result.And(validator.Validate());
            _normalized[i] = validator.Normalized;
        }

        return result;
    }

    private static string ElementName(string parameterName, int index) => $"{parameterName}[{index.ToString(CultureInfo.InvariantCulture)}]";
}
