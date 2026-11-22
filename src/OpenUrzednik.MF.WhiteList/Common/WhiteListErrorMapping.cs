using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// Turns the MF White List API's error responses into the provider's error types (ADR-0023's error table): it reads the API's
/// <c>{"message", "code"}</c> body and keeps the code in <see cref="IWhiteListErrorCode.WhiteListCode"/>. Statuses the table doesn't name
/// fall back to the shared layer's default mapping.
/// </summary>
internal static class WhiteListErrorMapping
{
    // The codes of a 400 that asks to repeat the query while the list is updated.
    internal const string DatabaseUpdatedCode = "WL-195";
    internal const string UpdateInProgressCode = "WL-196";

    // The code of a 500 for an entity whose historical status the system can't determine.
    internal const string HistoricalStatusUnknownCode = "WL-197";

    // The shortest identifier the API echoes is a 9-digit REGON; NIP has 10 digits, a long REGON 14, an account number 26.
    internal const int MinIdentifierDigits = 9;

    // The prefix of the API's own codes (WL-100 to WL-197).
    private const string CodePrefix = "WL-";

    /// <summary>
    /// The most of an error body <see cref="ReadCodeAsync"/> buffers: the API's <c>{"message", "code"}</c> answers are far smaller, so a
    /// larger body (a proxy's HTML page) has no code worth waiting for, and the request is sent with <c>ResponseHeadersRead</c>, so it
    /// isn't read in full.
    /// </summary>
    internal const int MaxCodeBodyBytes = 4096;

    // No separator: not one that joins an identifier's digits, or none seen yet in the run.
    private const char NoSeparator = '\0';

    private const HttpStatusCode TooManyRequests = (HttpStatusCode)429;

    /// <summary>
    /// Maps a non-success response; <see langword="null"/> for a status the table doesn't name (the shared default mapping applies).
    /// </summary>
    /// <param name="context">The response.</param>
    /// <param name="timeProvider">The client's clock, for the midnight estimate of a 429 without <c>Retry-After</c>.</param>
    internal static async Task<OpenUrzednikError?> MapAsync(ErrorResponseContext context, TimeProvider timeProvider)
    {
        var statusCode = context.StatusCode;
        if (statusCode != HttpStatusCode.BadRequest && statusCode != TooManyRequests && statusCode < HttpStatusCode.InternalServerError)
            return null;

        var body = Parse(await context.ReadMessageAsync().ConfigureAwait(false));
        var code = SanitizeCode(body?.Code);
        var detail = Detail(code, body?.Message);
        var path = context.RequestPath;

        if (statusCode == TooManyRequests)
        {
            // The ministry documents no Retry-After; an IP over the limit "may" be blocked until midnight (ADR-0023).
            var retryAfter = context.RetryAfter ?? WarsawCalendar.UntilNextMidnight(timeProvider);
            return new WhiteListRateLimitExceededError(
                $"MF White List API rate limit hit on {path}{detail}", retryAfter, statusCode, code);
        }

        if (statusCode == HttpStatusCode.BadRequest)
        {
            if (code == DatabaseUpdatedCode || code == UpdateInProgressCode)
                return new WhiteListServiceUnavailableError(
                    $"MF White List API asked to repeat the request to {path} later{detail}", statusCode, code);

            return new WhiteListBadRequestError($"MF White List API rejected the request to {path}{detail}", statusCode, code);
        }

        if (statusCode == HttpStatusCode.InternalServerError && code == HistoricalStatusUnknownCode)
            return new WhiteListUnknownError(
                $"MF White List API can't determine the historical status for {path}{detail}", statusCode, code);

        return new WhiteListServiceUnavailableError(
            $"MF White List API is unavailable for {path}, status {((int)statusCode).ToString(CultureInfo.InvariantCulture)}{detail}",
            statusCode, code);
    }

    /// <summary>
    /// Masks every identifier the server's message echoes, so it never reaches messages or telemetry (ADR-0025): a run of at least
    /// <see cref="MinIdentifierDigits"/> digits, also when single separators of one style join them (<c>736-812-54-16</c>,
    /// <c>61 1090 1014 …</c>; a space or NBSP, <c>-</c>, <c>.</c> or <c>/</c>). Every digit but the last
    /// <see cref="SensitiveValue.VisibleCharacters"/> becomes <see cref="SensitiveValue.MaskCharacter"/>, and the separators stay
    /// (<c>***-***-54-16</c>). A separator of another style ends the run, and the next run starts with the ended run's last group of
    /// digits, so a date and a time (<c>2026-11-19 10:15:30</c>) stay as they are, and a number before an identifier
    /// (<c>10:15:30 736-812-54-16</c>) doesn't keep it from being masked. Not masked: an identifier whose own separators mix styles, or are anything else
    /// (a tab, a narrow no-break space); the API sends identifiers as plain digits.
    /// </summary>
    internal static string MaskIdentifiers(string text)
    {
        char[]? masked = null;
        var start = 0;
        var digits = 0;
        var groupStart = 0;
        var groupDigits = 0;
        var style = NoSeparator;
        // A digit extends the run and its current group; a separator of the run's style (or the first one) followed by a digit starts
        // a new group; one of another style followed by a digit closes the run and starts the next one at the last group; anything
        // else closes the run [start, index), and the end of the text closes the last one. Runs may overlap: masking only ever hides
        // digits. One loop with no inner skip, so that mutation testing has no equivalent mutant here (ADR-0020).
        for (var index = 0; index < text.Length; index++)
        {
            if (IsDigit(text[index]))
            {
                if (digits == 0)
                    start = index;
                if (groupDigits == 0)
                    groupStart = index;
                digits++;
                groupDigits++;
                continue;
            }

            // Read for every non-digit, not only inside a run: there the character before is always a digit, so a lookbehind
            // mutant would be equivalent; here it fails on the first character. A separator before a run's first digit only sets
            // the style the run's own first separator would set or restart from, so it needs no check of its own.
            var nextIsDigit = index + 1 < text.Length && IsDigit(text[index + 1]);
            var separator = nextIsDigit ? SeparatorStyle(text[index]) : NoSeparator;
            if (separator != NoSeparator && (style == NoSeparator || style == separator))
            {
                style = separator;
                groupDigits = 0;
                continue;
            }

            Mask(ref masked, text, start, index, digits);
            if (separator != NoSeparator)
            {
                // Another style: the last group begins the next run.
                start = groupStart;
                digits = groupDigits;
                groupDigits = 0;
                style = separator;
                continue;
            }

            digits = 0;
            groupDigits = 0;
            style = NoSeparator;
        }

        Mask(ref masked, text, start, text.Length, digits);
        return masked is null ? text : new string(masked);
    }

    // Hides the digits of the run [start, end) but its last VisibleCharacters, when it holds an identifier; separators stay.
    private static void Mask(ref char[]? masked, string text, int start, int end, int digits)
    {
        if (digits < MinIdentifierDigits)
            return;

        masked ??= text.ToCharArray();
        var hidden = digits - SensitiveValue.VisibleCharacters;
        for (var index = start; index < end; index++)
        {
            if (IsDigit(text[index]) && hidden-- > 0)
                masked[index] = SensitiveValue.MaskCharacter;
        }
    }

    // The style of a separator that may join an identifier's digits: a space (NBSP included), '-', '.' or '/'; NoSeparator for
    // anything else.
    private static char SeparatorStyle(char value) => value switch
    {
        ' ' or '\u00A0' => ' ',
        '-' or '.' or '/' => value,
        _ => NoSeparator,
    };

    /// <summary>
    /// The server's code when it has a documented shape: <c>WL-</c> and three digits (<c>WL-115</c>), or WL2's single upper-case letter
    /// (<c>I</c>, <c>D</c>, <c>X</c>); <see langword="null"/> for anything else, which is never echoed into messages, <c>WhiteListCode</c> or
    /// metadata, so a proxy's or a changed API's text can't carry an identifier or other data there.
    /// </summary>
    internal static string? SanitizeCode(string? code)
    {
        if (code is null)
            return null;

        var length = code.Length;
        if (length == 1)
            return IsUpperCaseLetter(code[0]) ? code : null;

        if (length == CodePrefix.Length + 3 && code.StartsWith(CodePrefix, StringComparison.Ordinal)
            && IsDigit(code[3]) && IsDigit(code[4]) && IsDigit(code[5]))
            return code;

        return null;
    }

    // ASCII digits only: the API sends identifiers as ASCII.
    private static bool IsDigit(char value) => (uint)(value - '0') <= 9;

    private static bool IsUpperCaseLetter(char value) => (uint)(value - 'A') <= 'Z' - 'A';

    /// <summary>
    /// The API's code in an error body, or <see langword="null"/> for no body, one that can not be read, one over
    /// <see cref="MaxCodeBodyBytes"/>, or one without a documented code. Reads at most <see cref="MaxCodeBodyBytes"/> + 1 bytes of the
    /// body and replaces <see cref="HttpResponseMessage.Content"/>, so <see cref="MapAsync"/> reads the body from the start afterwards:
    /// by the bytes read, for a body within the limit, or by them followed by the rest of the stream, for a larger one (which has no
    /// code). The body has to be replaced, because network content can be read once: a failed attempt to buffer it would leave it
    /// consumed. A body that fails while it is read is left as it is.
    /// </summary>
    /// <remarks>
    /// A body that an app handler already consumed (or disposed) has no code either: it is left as it is, for the caller to fail on.
    /// </remarks>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    internal static async Task<string?> ReadCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // .NET Framework leaves Content null when there is no body.
        var content = response.Content;
        if (content is null)
            return null;

        var buffer = new byte[MaxCodeBodyBytes + 1];
        var length = 0;
        var stream = Stream.Null;
        try
        {
            // .NET honours the token in the stream only when the content does; WaitAsync bounds the wait either way.
            stream = await content.ReadAsStreamAsync(cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
            // A loop without an assignment in its condition, as in RestRequestExecutor, so that mutation testing can check it.
            while (length != buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                length += read;
            }
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            // The body was consumed before (.NET), or its content disposed (.NET Framework: ObjectDisposedException).
            return null;
        }
        catch (NotSupportedException)
        {
            // .NET Framework: the body was consumed before.
            return null;
        }
        catch (InvalidDataException)
        {
            // A compressed body that can't be decompressed (AutomaticDecompression).
            return null;
        }

        if (length > MaxCodeBodyBytes)
        {
            response.Content = new WhiteListReplayContent(buffer, length, stream, content);
            return null;
        }

        var replacement = new ByteArrayContent(buffer, 0, length);
        WhiteListReplayContent.CopyHeaders(content, replacement);
        response.Content = replacement;
        content.Dispose();
        return SanitizeCode(Parse(Encoding.UTF8.GetString(buffer, 0, length))?.Code);
    }

    // The body is the API's JSON; anything else (no body, a proxy's HTML page, a body cut at the 500 characters the executor reads) has no code.
    internal static ErrorDto? Parse(string? body)
    {
        if (body is null)
            return null;

        // One return after the try, so that mutation testing has no equivalent mutant in the catch block (ADR-0020).
        ErrorDto? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize(body, WhiteListJsonContext.Default.ErrorDto);
        }
        catch (JsonException)
        {
            // Not the API's JSON: the answer has no code.
        }

        return dto;
    }

    /// <summary>
    /// <c>": WL-115 Nieprawidłowy NIP."</c> with the identifiers in the message masked (<see cref="MaskIdentifiers"/>), or <c>"."</c>
    /// without code and message; <paramref name="code"/> must be sanitized (<see cref="SanitizeCode"/>). Also the detail of a batch entry's
    /// <c>error</c>.
    /// </summary>
    internal static string Detail(string? code, string? message)
    {
        if (message is not null)
            message = MaskIdentifiers(message);

        if (code is null)
            return message is null ? "." : $": {message}";

        return message is null ? $": {code}" : $": {code} {message}";
    }
}
