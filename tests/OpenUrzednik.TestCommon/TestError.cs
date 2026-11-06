using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.TestCommon;

public sealed class TestError(string message) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "testError";

    protected override OpenUrzednikException CreateException() => new TestException(Message);
}
