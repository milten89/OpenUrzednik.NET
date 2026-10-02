namespace OpenUrzednik.Extensions.Logging.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class LogValuesTest
{
    private static LogValues Create(string messageTemplate, params (string Name, object? Value)[] values)
        => new(messageTemplate, [.. values.Select(v => new KeyValuePair<string, object?>(v.Name, v.Value))]);
}
