using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.TestCommon;

public class TestException(string message) : OpenUrzednikException(TestError.ErrorCode, message);
