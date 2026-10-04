using System.Collections;
using System.Globalization;
using System.Text;

namespace OpenUrzednik.Extensions.Logging;

/// <summary>
/// Log state in the shape Microsoft.Extensions.Logging providers expect: the named values, then <c>{OriginalFormat}</c>.
/// </summary>
internal sealed class LogValues : IReadOnlyList<KeyValuePair<string, object?>>
{
    internal const string OriginalFormatKey = "{OriginalFormat}";
    internal const string NullValue = "(null)";

    private readonly string _messageTemplate;
    private readonly KeyValuePair<string, object?>[] _values;

    internal LogValues(string messageTemplate, KeyValuePair<string, object?>[] values)
    {
        _messageTemplate = messageTemplate;
        _values = values;
    }

    public int Count => _values.Length + 1;

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (index == _values.Length)
                return new(OriginalFormatKey, _messageTemplate);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _values.Length);
            return _values[index];
        }
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Fills the template's placeholders with the values in order, as <c>LoggerMessage</c> does.
    /// <c>{{</c> and <c>}}</c> are literal braces; alignment and format (<c>{name,5:0.00}</c>) are applied with the invariant culture.
    /// </summary>
    public override string ToString()
    {
        var template = _messageTemplate;
        var builder = new StringBuilder(template.Length + 32);
        var valueIndex = 0;
        var i = 0;
        while (i < template.Length)
        {
            var c = template[i];
            if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
            {
                builder.Append(c);
                i += 2;
                continue;
            }

            if (c == '{')
            {
                var end = template.IndexOf('}', i + 1);
                if (end < 0)
                {
                    builder.Append(template, i, template.Length - i);
                    break;
                }

                if (valueIndex < _values.Length)
                    AppendValue(builder, _values[valueIndex++].Value, template.AsSpan(i + 1, end - i - 1));
                else
                    builder.Append(template, i, end - i + 1);

                i = end + 1;
                continue;
            }

            builder.Append(c);
            i++;
        }

        return builder.ToString();
    }

    private static string FormatItem(object? item) => item switch
    {
        null => NullValue,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => item.ToString() ?? string.Empty,
    };

    private static void AppendValue(StringBuilder builder, object? value, ReadOnlySpan<char> hole)
    {
        if (value is null)
        {
            builder.Append(NullValue);
            return;
        }

        if (value is IEnumerable enumerable and not string and not IFormattable)
            value = string.Join(", ", enumerable.Cast<object?>().Select(FormatItem));

        var formatStart = hole.IndexOfAny(',', ':');
        if (formatStart < 0)
        {
            builder.Append(value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString());
            return;
        }

        var compositeFormat = string.Concat("{0".AsSpan(), hole.Slice(formatStart), "}".AsSpan());
        try
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, compositeFormat, value);
        }
        catch (FormatException)
        {
            // A bad alignment or format must not make logging throw; write the placeholder as it is.
            builder.Append('{').Append(hole).Append('}');
        }
    }
}
