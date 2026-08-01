using System.Runtime.CompilerServices;

namespace OpenUrzednik.TestCommon.Attributes;

public sealed class ManualFactAttribute : FactAttribute
{
    public ManualFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1) 
        : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable(TestConst.IntegrationTestsEnabledEnvVar) is null)
            Skip = $"Integration test calling real API. Set {TestConst.IntegrationTestsEnabledEnvVar} to start.";
    }
}
