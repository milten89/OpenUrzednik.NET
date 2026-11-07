using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Core.Tests.Validation;

// Shared helpers; tests are in one file per method.
public partial class ValueValidatorTest
{
    private sealed class TestValidator(string propertyName, int value) : ValueValidator<int>(propertyName, value)
    {
        public override string Name => "test";

        public override OpenUrzednikResult Validate() => OpenUrzednikResult.Success();
    }
}
