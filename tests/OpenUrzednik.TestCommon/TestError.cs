using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.TestCommon;

public sealed class TestError(string message) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "testError";

    public override Exception ToException() => new TestException(Message);
}
