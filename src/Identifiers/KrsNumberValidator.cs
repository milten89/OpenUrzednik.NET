using System.Diagnostics.CodeAnalysis;

using OpenUrzednik.Core;

namespace OpenUrzednik.Identifiers;

/// <summary>
/// Validates a KRS number: 10 digits with the leading zeros (e.g. <c>0000012345</c>), written with or without spaces and hyphens.
/// It has no check digit. All zeros is never issued.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Shared source (Identifiers.props): every provider compiles all validators but may use only some.")]
internal sealed class KrsNumberValidator(string propertyName, string value) : IdentifierValidator(propertyName, value, allowCountryPrefix: false)
{
    /// <inheritdoc />
    public override string Name => "krs";

    /// <inheritdoc />
    protected override string Format => "a KRS number of 10 digits, leading zeros included";

    /// <inheritdoc />
    protected override bool HasValidLength(int length) => length == 10;

    /// <inheritdoc />
    protected override OpenUrzednikResult ValidateDigits(string digits)
        => AreZeros(digits, 10) ? NotIssued() : OpenUrzednikResult.Success();
}
