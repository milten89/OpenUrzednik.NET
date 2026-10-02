using System.Runtime.CompilerServices;

namespace OpenUrzednik.TestCommon.Attributes;

/// <summary>
/// A test that calls the real API. It is explicit, so it runs only with <c>--explicit on</c> (or <c>only</c>), or when
/// <c>OPEN_URZEDNIK_INTEGRATION_TEST_ENABLED</c> is set. It never runs in the default CI build.
/// </summary>
public sealed class ManualTheoryAttribute : TheoryAttribute
{
    public ManualTheoryAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
    {
        // Calls the real API, so it runs only when asked for: with `--explicit on`, or when the environment variable is set.
        Explicit = Environment.GetEnvironmentVariable(TestConst.IntegrationTestsEnabledEnvVar) is null;
    }
}
