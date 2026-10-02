namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// A structured logger used by the OpenUrzednik.NET clients, so the core packages don't depend on a logging library (ADR-0003).
/// Use <see cref="NullOpenUrzednikLogger"/> to log nothing, or the adapter in <c>OpenUrzednik.Extensions.Logging</c> to write to <c>ILogger</c>.
/// </summary>
/// <remarks>
/// Message templates use named placeholders (<c>"{provider} request to {path} failed"</c>), filled with the values in order.
/// The generic overloads let implementations skip boxing when the level is disabled; callers should still check
/// <see cref="IsEnabled"/> before building values for <see cref="OpenUrzednikLogLevel.Debug"/> and <see cref="OpenUrzednikLogLevel.Trace"/> entries.
/// </remarks>
public interface IOpenUrzednikLogger
{
    /// <summary>Returns whether entries at <paramref name="level"/> are written.</summary>
    /// <param name="level">The level to check.</param>
    bool IsEnabled(OpenUrzednikLogLevel level);

    /// <summary>Writes an entry without values.</summary>
    /// <param name="level">The entry level.</param>
    /// <param name="exception">The exception the entry is about, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    void Log(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate);

    /// <summary>Writes an entry with one named value.</summary>
    /// <param name="level">The entry level.</param>
    /// <param name="exception">The exception the entry is about, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="name0">The name of the first placeholder.</param>
    /// <param name="value0">The value of the first placeholder.</param>
    void Log<T0>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                 string name0, T0 value0);

    /// <summary>Writes an entry with two named values.</summary>
    /// <param name="level">The entry level.</param>
    /// <param name="exception">The exception the entry is about, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="name0">The name of the first placeholder.</param>
    /// <param name="value0">The value of the first placeholder.</param>
    /// <param name="name1">The name of the second placeholder.</param>
    /// <param name="value1">The value of the second placeholder.</param>
    void Log<T0, T1>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                     string name0, T0 value0,
                     string name1, T1 value1);

    /// <summary>Writes an entry with three named values.</summary>
    /// <param name="level">The entry level.</param>
    /// <param name="exception">The exception the entry is about, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="name0">The name of the first placeholder.</param>
    /// <param name="value0">The value of the first placeholder.</param>
    /// <param name="name1">The name of the second placeholder.</param>
    /// <param name="value1">The value of the second placeholder.</param>
    /// <param name="name2">The name of the third placeholder.</param>
    /// <param name="value2">The value of the third placeholder.</param>
    void Log<T0, T1, T2>(OpenUrzednikLogLevel level, Exception? exception, string messageTemplate,
                         string name0, T0 value0,
                         string name1, T1 value1,
                         string name2, T2 value2);

    /// <summary>Writes an entry with any number of named values.</summary>
    /// <param name="level">The entry level.</param>
    /// <param name="exception">The exception the entry is about, if any.</param>
    /// <param name="messageTemplate">The message template.</param>
    /// <param name="properties">The placeholder names and values, in template order.</param>
    void Log(OpenUrzednikLogLevel level, Exception? exception,
        string messageTemplate,
        IReadOnlyList<KeyValuePair<string, object?>> properties);
}
