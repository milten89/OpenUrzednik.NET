namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Log level, with the same values as <c>Microsoft.Extensions.Logging.LogLevel</c>.
/// </summary>
public enum OpenUrzednikLogLevel
{
    /// <summary>Most detailed messages.</summary>
    Trace = 0,
    /// <summary>Diagnostic details, e.g. validation failures, 404 responses and empty results.</summary>
    Debug = 1,
    /// <summary>General flow of the application.</summary>
    Information = 2,
    /// <summary>Expected failures of a request, e.g. timeouts, network errors and non-success HTTP statuses.</summary>
    Warning = 3,
    /// <summary>Unexpected failures, e.g. a response that can't be read or mapped.</summary>
    Error = 4,
    /// <summary>Failures that need immediate attention.</summary>
    Critical = 5,
    /// <summary>Disables logging.</summary>
    None = 6
}
