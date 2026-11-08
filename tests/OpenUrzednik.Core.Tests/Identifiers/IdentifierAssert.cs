using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

/// <summary>Assertions shared by the identifier validator tests.</summary>
internal static class IdentifierAssert
{
    /// <summary>
    /// Asserts a single <see cref="ValidationError"/> for <paramref name="ruleName"/> and <paramref name="propertyName"/> whose message ends
    /// with <paramref name="messageEnd"/>, and that the value appears only masked: never in the message, at most its last 4 characters
    /// in the metadata (ADR-0003).
    /// </summary>
    internal static void ShouldBeMaskedValidationError(this OpenUrzednikResult result, string ruleName, string propertyName, string? value, string messageEnd)
    {
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<string>>();
        error.Message.ShouldEndWith(messageEnd);
        error.Metadata[ValidationError.RuleNameMetadataKey].ShouldBe(ruleName);
        error.Metadata[ValidationError.NameMetadataKey].ShouldBe(propertyName);

        if (string.IsNullOrEmpty(value) || value!.Length <= 4)
            return;

        error.Message.ShouldNotContain(value);
        var masked = error.Metadata[ValidationError.ValueMetadataKey].ShouldBeOfType<string>();
        masked.ShouldBe(new string('*', value.Length - 4) + value.Substring(value.Length - 4));
    }
}
