using OpenUrzednik.Identifiers;

namespace OpenUrzednik.Core.Tests.Identifiers;

// Shared helpers; tests are in one file per method. Validate is tested through the concrete validators.
public partial class IdentifierValidatorTest
{
    // Exposes the protected helpers of the base class; its abstract members are never called.
    private sealed class TestValidator() : IdentifierValidator("value", string.Empty, countryPrefix: null)
    {
        public override string Name => "test";

        protected override string Format => "a test value";

        internal static int CallWeightedSum(string digits, int start, int[] weights) => WeightedSum(digits, start, weights);

        protected override bool HasValidLength(int length) => true;

        protected override OpenUrzednikResult ValidateDigits(string digits) => OpenUrzednikResult.Success();
    }
}
