using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.TestCommon;

/// <summary>
/// Proves a personal identifier (NIP, REGON, PESEL, bank account number) doesn't leak into what an app can see (ADR-0003,
/// CLAUDE.md Telemetry): log templates and properties, span names, tags, statuses and events, error messages, codes and metadata,
/// and exception messages, <see cref="Exception.Data"/> and the errors of an <see cref="OpenUrzednikException"/>, down the inner
/// exceptions. A masked value (only the last <c>4</c> characters visible) passes. It doesn't see what Microsoft's
/// <c>ILogger</c> receives (the <c>IHttpClientFactory</c> logging of ADR-0017): a DI package tests that itself.
/// </summary>
public static class IdentifierLeak
{
    /// <summary>The shortest run of an identifier's digits that counts as a leak: one more than a mask leaves visible.</summary>
    public const int LeakingDigits = 5;

    // A separator a formatted identifier may have between two digits: 736-812-54-16, 61 1090 1014 ….
    private const string Separator = @"[\s \-./]?";

    /// <summary>
    /// Fails when any of <paramref name="identifiers"/> appears in <paramref name="telemetry"/> or in <paramref name="result"/>'s
    /// errors: as given (ignoring case, for a <c>PL</c> prefix), or as any run of <see cref="LeakingDigits"/> or more of its
    /// consecutive digits, also with a separator (space, <c>-</c>, <c>.</c>, <c>/</c>) between them, which finds a prefix, a
    /// partly masked or a formatted value. Use identifiers whose digit runs can't appear by chance in the output.
    /// </summary>
    /// <param name="result">The result of the call under test.</param>
    /// <param name="telemetry">The telemetry the client wrote to.</param>
    /// <param name="identifiers">The raw identifiers the call was given; at least one.</param>
    public static void ShouldNotLeak(this OpenUrzednikResult result, RecordingTelemetry telemetry, params string[] identifiers)
        => ShouldNotLeak(result.Errors, telemetry, identifiers);

    /// <inheritdoc cref="ShouldNotLeak(OpenUrzednikResult, RecordingTelemetry, string[])"/>
    /// <remarks>The value of a successful result isn't searched: it's the data the API returned.</remarks>
    public static void ShouldNotLeak<T>(this OpenUrzednikResult<T> result, RecordingTelemetry telemetry, params string[] identifiers)
        => ShouldNotLeak(result.Errors, telemetry, identifiers);

    private static void ShouldNotLeak(IReadOnlyList<OpenUrzednikError> errors, RecordingTelemetry telemetry, string[] identifiers)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(identifiers);
        if (identifiers.Length == 0 || identifiers.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Give at least one identifier, none of them empty: with none, nothing is searched.", nameof(identifiers));

        var texts = telemetry.Texts.ToList();
        foreach (var (place, exception) in telemetry.Exceptions)
            texts.AddRange(Texts(place, exception));
        foreach (var (error, index) in errors.Select((error, index) => (error, index)))
            texts.AddRange(Texts($"error {index} ({error.GetType().Name})", error));

        var leaks = identifiers
            .SelectMany(identifier => Patterns(identifier).Select(pattern => (identifier, pattern)))
            .SelectMany(search => texts
                .Where(text => search.pattern.IsMatch(text.Text))
                .Select(text => $"{search.identifier} in {text.Place}: {text.Text}"))
            .Distinct()
            .ToList();

        leaks.ShouldBeEmpty("an identifier reached what an app can see: mask it with SensitiveValue.Mask or use a display value");
    }

    /// <summary>The text of a value: a string or <c>char[]</c> as it is, a collection, dictionary entry or pair item by item.</summary>
    internal static IEnumerable<string> Flatten(object? value)
        => value switch
        {
            null => [],
            string text => [text],
            char[] characters => [new string(characters)],
            DictionaryEntry entry => Flatten(entry.Key).Concat(Flatten(entry.Value)),
            IDictionary dictionary => Entries(dictionary).SelectMany(entry => Flatten(entry)),
            IEnumerable items => items.Cast<object?>().SelectMany(Flatten),
            IFormattable formattable => [formattable.ToString(null, CultureInfo.InvariantCulture)],
            _ when IsKeyValuePair(value.GetType()) => Flatten(Property(value, "Key")).Concat(Flatten(Property(value, "Value"))),
            _ => [value.ToString() ?? ""],
        };

    // The identifier as given, and every run of LeakingDigits digits (the shortest runs find every longer one) with an optional
    // separator between any two of them.
    private static IEnumerable<Regex> Patterns(string identifier)
    {
        yield return new Regex(Regex.Escape(identifier), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var digits = new string(identifier.Where(c => c is >= '0' and <= '9').ToArray());
        for (var start = 0; start + LeakingDigits <= digits.Length; start++)
            yield return new Regex(string.Join(Separator, digits.Substring(start, LeakingDigits).ToCharArray()), RegexOptions.CultureInvariant);
    }

    private static IEnumerable<(string Place, string Text)> Texts(string place, OpenUrzednikError error)
    {
        yield return ($"{place}: message", error.Message);
        yield return ($"{place}: code", error.Code);
        foreach (var entry in error.Metadata)
        {
            yield return ($"{place}: metadata key", entry.Key);
            foreach (var value in Flatten(entry.Value))
                yield return ($"{place}: metadata {entry.Key}", value);
        }
        foreach (var text in Texts($"{place}: ToException()", error.ToException()))
            yield return text;
    }

    private static IEnumerable<(string Place, string Text)> Texts(string place, Exception exception)
    {
        for (var (current, depth) = (exception, 0); current is not null; (current, depth) = (current.InnerException, depth + 1))
        {
            var where = depth == 0 ? place : $"{place} inner {depth} ({current.GetType().Name})";
            yield return ($"{where}: message", current.Message);
            foreach (DictionaryEntry entry in current.Data)
            {
                foreach (var key in Flatten(entry.Key))
                    yield return ($"{where}: Data key", key);
                foreach (var value in Flatten(entry.Value))
                    yield return ($"{where}: Data value", value);
            }
            // A multi-error OpenUrzednikValidationException lists every error; the message names only the first.
            if (current is OpenUrzednikException openUrzednik)
            {
                foreach (var (error, index) in openUrzednik.Errors.Select((error, index) => (error, index)))
                {
                    yield return ($"{where} error {index}: message", error.Message);
                    foreach (var value in error.Metadata.Values.SelectMany(Flatten))
                        yield return ($"{where} error {index}: metadata", value);
                }
            }
            if (current is AggregateException aggregate)
            {
                foreach (var (inner, index) in aggregate.InnerExceptions.Skip(1).Select((inner, index) => (inner, index + 1)))
                {
                    foreach (var text in Texts($"{where} aggregate {index}", inner))
                        yield return text;
                }
            }
        }
    }

    // Through the dictionary's own enumerator: a generic Dictionary enumerated as IEnumerable yields KeyValuePairs, not entries.
    private static IEnumerable<DictionaryEntry> Entries(IDictionary dictionary)
    {
        var entries = dictionary.GetEnumerator();
        while (entries.MoveNext())
            yield return entries.Entry;
    }

    private static bool IsKeyValuePair(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

    private static object? Property(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);
}
