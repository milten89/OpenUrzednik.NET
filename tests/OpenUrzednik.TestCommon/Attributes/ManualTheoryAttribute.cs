using System.Runtime.CompilerServices;

namespace OpenUrzednik.TestCommon.Attributes;

public sealed class ManualTheoryAttribute : TheoryAttribute
{
    public ManualTheoryAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
    {
        if (Environment.GetEnvironmentVariable(TestConst.IntegrationTestsEnabledEnvVar) is null)
            Skip = $"Integration test calling real API. Set {TestConst.IntegrationTestsEnabledEnvVar} to start.";
    }
}
