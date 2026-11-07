using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Tests.Exceptions;

// Shared helpers; tests are in one file per method.
public partial class OpenUrzednikExceptionTest
{
    // An exception with any code, to reach the base class's own argument checks.
    private sealed class CodeException : OpenUrzednikException
    {
        public CodeException(string code)
            : base(code, "message") { }

        public CodeException(string code, Exception innerException)
            : base(code, "message", innerException) { }
    }
}
