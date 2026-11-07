using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

namespace OpenUrzednik.Core.Tests.Errors;

// Shared helpers; tests are in one file per method.
public partial class OpenUrzednikErrorTest
{
    // An error with any code, to reach the base class's own argument checks.
    private sealed class CodeError(string code, string message) : OpenUrzednikError(code, message)
    {
        protected override OpenUrzednikException CreateException() => new TestException(Message);
    }
}
