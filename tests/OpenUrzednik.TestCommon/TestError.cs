using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.TestCommon;

public record TestError(string Message) : OpenUrzednikError(Message)
{
    public override Exception ToException() => new TestException(Message);
}
